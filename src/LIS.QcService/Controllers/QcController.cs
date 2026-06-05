using LIS.QcService.DTOs;
using LIS.QcService.Helpers;
using LIS.QcService.Models;
using LIS.QcService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace LIS.QcService.Controllers;

[ApiController]
[Route("[controller]")]
public class QcController : ControllerBase
{
    private readonly IQcRepository _qcRepository;

    public QcController(IQcRepository qcRepository)
    {
        _qcRepository = qcRepository;
    }

    private Guid GetLabId()
    {
        var labIdStr = HttpContext.Items["labId"]?.ToString();
        if (!string.IsNullOrEmpty(labIdStr) && Guid.TryParse(labIdStr, out var labId))
            return labId;
        // Default lab ID for development (no auth)
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private (Guid? userId, string? name) GetUserInfo()
    {
        var userIdStr = HttpContext.Items["userId"]?.ToString();
        var name = HttpContext.Items["name"]?.ToString() ?? "Dev User";
        Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
        return (userId, name);
    }

    // ─── Materials ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Add a new QC material (control material).
    /// </summary>
    [HttpPost("materials")]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateQcMaterialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialName))
            return BadRequest(ApiResponse<object>.Fail("Material name is required"));
        if (string.IsNullOrWhiteSpace(request.LotNumber))
            return BadRequest(ApiResponse<object>.Fail("Lot number is required"));

        var labId = GetLabId();

        var material = new QcMaterial
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            MaterialName = request.MaterialName,
            Manufacturer = request.Manufacturer,
            LotNumber = request.LotNumber,
            ExpiryDate = request.ExpiryDate,
            Level = request.Level,
            IsActive = true
        };

        var created = await _qcRepository.CreateMaterialAsync(material);
        return Created($"/qc/materials/{created.Id}", ApiResponse<QcMaterial>.Ok(created, "QC material created"));
    }

    /// <summary>
    /// List all active QC materials for the lab.
    /// </summary>
    [HttpGet("materials")]
    public async Task<IActionResult> GetMaterials()
    {
        var labId = GetLabId();
        var materials = await _qcRepository.GetMaterialsAsync(labId);
        return Ok(ApiResponse<List<QcMaterial>>.Ok(materials));
    }

    // ─── Target Values ───────────────────────────────────────────────────────────

    /// <summary>
    /// Set expected target values for a material + test combination.
    /// </summary>
    [HttpPost("materials/{id}/targets")]
    public async Task<IActionResult> CreateTargetValue(Guid id, [FromBody] CreateTargetValueRequest request)
    {
        if (request.TestId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Test ID is required"));
        if (request.ExpectedSd <= 0)
            return BadRequest(ApiResponse<object>.Fail("Expected SD must be greater than zero"));

        var targetValue = new QcTargetValue
        {
            Id = Guid.NewGuid(),
            MaterialId = id,
            TestId = request.TestId,
            TestCode = request.TestCode,
            TestName = request.TestName,
            ExpectedMean = request.ExpectedMean,
            ExpectedSd = request.ExpectedSd,
            Unit = request.Unit
        };

        var created = await _qcRepository.CreateTargetValueAsync(targetValue);
        return Created($"/qc/materials/{id}/targets/{created.Id}", ApiResponse<QcTargetValue>.Ok(created, "Target value created"));
    }

    /// <summary>
    /// Get target values for a specific material.
    /// </summary>
    [HttpGet("materials/{id}/targets")]
    public async Task<IActionResult> GetTargetValues(Guid id)
    {
        var targets = await _qcRepository.GetTargetValuesAsync(id);
        return Ok(ApiResponse<List<QcTargetValue>>.Ok(targets));
    }

    // ─── QC Records ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Enter a QC measurement. Auto-evaluates Westgard rules and creates block if out of control.
    /// </summary>
    [HttpPost("records")]
    public async Task<IActionResult> EnterQcRecord([FromBody] EnterQcRecordRequest request)
    {
        if (request.MaterialId == Guid.Empty || request.TargetValueId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Material ID and Target Value ID are required"));

        var labId = GetLabId();
        var (userId, userName) = GetUserInfo();

        // Get target value info
        var targets = await _qcRepository.GetTargetValuesAsync(request.MaterialId);
        var target = targets.FirstOrDefault(t => t.Id == request.TargetValueId);
        if (target == null)
            return NotFound(ApiResponse<object>.Fail("Target value not found"));

        // Get recent values for Westgard rule evaluation
        var recentValues = await _qcRepository.GetRecentValuesAsync(labId, request.TargetValueId, 10);

        // Evaluate Westgard rules
        var (status, violation) = WestgardRules.Evaluate(
            request.MeasuredValue, target.ExpectedMean, target.ExpectedSd, recentValues);

        var sdIndex = target.ExpectedSd != 0
            ? Math.Round((request.MeasuredValue - target.ExpectedMean) / target.ExpectedSd, 2)
            : 0;

        // Get material info for lot number and level
        var materials = await _qcRepository.GetMaterialsAsync(labId);
        var material = materials.FirstOrDefault(m => m.Id == request.MaterialId);

        var record = new QcRecord
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            MaterialId = request.MaterialId,
            TargetValueId = request.TargetValueId,
            InstrumentId = request.InstrumentId,
            InstrumentName = request.InstrumentName,
            TestId = target.TestId,
            TestCode = target.TestCode,
            TestName = target.TestName,
            Level = material?.Level ?? "normal",
            LotNumber = material?.LotNumber,
            MeasuredValue = request.MeasuredValue,
            ExpectedMean = target.ExpectedMean,
            ExpectedSd = target.ExpectedSd,
            SdIndex = sdIndex,
            Status = status,
            WestgardViolation = violation,
            RunDate = DateTime.UtcNow.Date,
            RecordedBy = userId,
            RecordedByName = userName,
            Comments = request.Comments
        };

        var created = await _qcRepository.CreateQcRecordAsync(record);

        // Auto-block test if out of control
        if (status == "out_of_control")
        {
            var block = new QcBlock
            {
                Id = Guid.NewGuid(),
                LabId = labId,
                TestId = target.TestId,
                InstrumentId = request.InstrumentId,
                Reason = $"Westgard {violation} violation detected. Measured: {request.MeasuredValue}, Mean: {target.ExpectedMean}, SD: {target.ExpectedSd}",
                BlockedBy = userId
            };
            await _qcRepository.CreateBlockAsync(block);
        }

        var response = MapToRecordResponse(created);
        return Created($"/qc/records/{created.Id}", ApiResponse<QcRecordResponse>.Ok(response, $"QC record entered. Status: {status}"));
    }

    /// <summary>
    /// List QC records with filtering and pagination.
    /// </summary>
    [HttpGet("records")]
    public async Task<IActionResult> GetQcRecords(
        [FromQuery] Guid? testId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var (items, total) = await _qcRepository.GetQcRecordsAsync(labId, testId, fromDate, toDate, page, pageSize);

        var responses = items.Select(MapToRecordResponse).ToList();

        var result = new
        {
            items = responses,
            total,
            page,
            pageSize
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    // ─── Levey-Jennings Chart ────────────────────────────────────────────────────

    /// <summary>
    /// Get Levey-Jennings chart data for a target value (last 30 days by default).
    /// </summary>
    [HttpGet("charts/{targetValueId}")]
    public async Task<IActionResult> GetLeveyJenningsChart(Guid targetValueId, [FromQuery] int days = 30)
    {
        var labId = GetLabId();

        var records = await _qcRepository.GetLeveyJenningsDataAsync(labId, targetValueId, days);

        var dataPoints = records.Select(r => new LeveyJenningsDataPoint
        {
            Date = r.RunDate.ToString("yyyy-MM-dd"),
            MeasuredValue = r.MeasuredValue,
            Mean = r.ExpectedMean,
            PlusOneSd = r.ExpectedMean + r.ExpectedSd,
            MinusOneSd = r.ExpectedMean - r.ExpectedSd,
            PlusTwoSd = r.ExpectedMean + (2 * r.ExpectedSd),
            MinusTwoSd = r.ExpectedMean - (2 * r.ExpectedSd),
            PlusThreeSd = r.ExpectedMean + (3 * r.ExpectedSd),
            MinusThreeSd = r.ExpectedMean - (3 * r.ExpectedSd),
            Status = r.Status
        }).ToList();

        return Ok(ApiResponse<List<LeveyJenningsDataPoint>>.Ok(dataPoints));
    }

    // ─── QC Status ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Get overall QC compliance status per test/instrument.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetQcStatus()
    {
        var labId = GetLabId();

        var statusList = await _qcRepository.GetQcStatusAsync(labId);

        var responses = statusList.Select(s => new QcStatusResponse
        {
            TestCode = s.TestCode,
            TestName = s.TestName,
            InstrumentName = s.InstrumentName,
            LastQcDate = s.LastQcDate?.ToString("yyyy-MM-dd"),
            LastQcStatus = s.LastQcStatus,
            IsBlocked = s.IsBlocked,
            BlockReason = s.BlockReason
        }).ToList();

        return Ok(ApiResponse<List<QcStatusResponse>>.Ok(responses));
    }

    // ─── Blocks ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Get all active QC blocks (tests that cannot report results).
    /// </summary>
    [HttpGet("blocks")]
    public async Task<IActionResult> GetActiveBlocks()
    {
        var labId = GetLabId();
        var blocks = await _qcRepository.GetActiveBlocksAsync(labId);
        return Ok(ApiResponse<List<QcBlock>>.Ok(blocks));
    }

    /// <summary>
    /// Resolve a QC block (after corrective action taken).
    /// </summary>
    [HttpPatch("blocks/{id}/resolve")]
    public async Task<IActionResult> ResolveBlock(Guid id)
    {
        var (userId, _) = GetUserInfo();
        await _qcRepository.ResolveBlockAsync(id, userId ?? Guid.Empty);
        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), resolved = true }, "Block resolved successfully"));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static QcRecordResponse MapToRecordResponse(QcRecord record)
    {
        return new QcRecordResponse
        {
            Id = record.Id.ToString(),
            TestCode = record.TestCode,
            TestName = record.TestName,
            Level = record.Level,
            LotNumber = record.LotNumber,
            MeasuredValue = record.MeasuredValue,
            ExpectedMean = record.ExpectedMean,
            ExpectedSd = record.ExpectedSd,
            SdIndex = record.SdIndex,
            Status = record.Status,
            WestgardViolation = record.WestgardViolation,
            RunDate = record.RunDate.ToString("yyyy-MM-dd"),
            RecordedByName = record.RecordedByName
        };
    }
}
