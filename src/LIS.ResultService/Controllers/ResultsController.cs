using LIS.ResultService.DTOs;
using LIS.ResultService.Helpers;
using LIS.ResultService.Models;
using LIS.ResultService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace LIS.ResultService.Controllers;

[ApiController]
[Route("[controller]")]
public class ResultsController : ControllerBase
{
    private readonly IResultRepository _resultRepository;

    public ResultsController(IResultRepository resultRepository)
    {
        _resultRepository = resultRepository;
    }

    private Guid GetLabId()
    {
        var labIdStr = HttpContext.Items["labId"]?.ToString();
        if (string.IsNullOrEmpty(labIdStr) || !Guid.TryParse(labIdStr, out var labId))
            throw new UnauthorizedAccessException("Lab context not found");
        return labId;
    }

    private (Guid? userId, string? name) GetUserInfo()
    {
        var userIdStr = HttpContext.Items["userId"]?.ToString();
        var name = HttpContext.Items["name"]?.ToString();
        Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : null;
        return (userId, name);
    }

    /// <summary>
    /// Enter a single test result with auto-flagging and critical alert generation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> EnterResult([FromBody] EnterResultRequest request)
    {
        if (request.OrderTestId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("OrderTestId is required"));

        var labId = GetLabId();
        var (userId, userName) = GetUserInfo();

        // Try to parse numeric value for flag evaluation
        decimal? numericValue = null;
        if (decimal.TryParse(request.ResultValue, out var parsed))
            numericValue = parsed;

        // Look up the order test to get test info and reference ranges
        var existingResult = await _resultRepository.GetByOrderTestIdAsync(request.OrderTestId);

        // For now, get reference ranges from existing result or default nulls
        decimal? refLow = existingResult?.ReferenceLow;
        decimal? refHigh = existingResult?.ReferenceHigh;
        decimal? critLow = existingResult?.CriticalLow;
        decimal? critHigh = existingResult?.CriticalHigh;

        var flag = ResultFlagHelper.EvaluateFlag(numericValue, refLow, refHigh, critLow, critHigh);
        var isCritical = ResultFlagHelper.IsCriticalFlag(flag);

        var result = new TestResult
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            OrderId = existingResult?.OrderId ?? Guid.Empty,
            OrderTestId = request.OrderTestId,
            TestId = existingResult?.TestId ?? Guid.Empty,
            TestCode = existingResult?.TestCode ?? string.Empty,
            TestName = existingResult?.TestName ?? string.Empty,
            ParameterName = existingResult?.ParameterName,
            ResultValue = request.ResultValue,
            ResultNumeric = numericValue,
            ResultUnit = request.ResultUnit,
            ReferenceLow = refLow,
            ReferenceHigh = refHigh,
            CriticalLow = critLow,
            CriticalHigh = critHigh,
            Flag = flag,
            IsCritical = isCritical,
            RawValue = request.ResultValue,
            Method = request.Method,
            Remarks = request.Remarks,
            EnteredBy = userId,
            EnteredByName = userName,
            EnteredAt = DateTime.UtcNow,
            Status = "entered",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _resultRepository.CreateResultAsync(result);

        // Auto-create critical alert if result is critical
        if (isCritical)
        {
            var alert = new CriticalAlert
            {
                Id = Guid.NewGuid(),
                LabId = labId,
                ResultId = created.Id,
                OrderId = created.OrderId,
                PatientName = null, // Would be populated from order lookup
                TestName = created.TestName,
                ResultValue = created.ResultValue,
                CriticalType = flag,
                CreatedAt = DateTime.UtcNow
            };
            await _resultRepository.CreateCriticalAlertAsync(alert);
        }

        var response = MapToResponse(created);
        return Created($"/results/{created.Id}", ApiResponse<ResultResponse>.Ok(response, "Result entered successfully"));
    }

    /// <summary>
    /// Enter multiple test results at once.
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> BatchEnterResults([FromBody] BatchEnterResultsRequest request)
    {
        if (request.Results.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("At least one result is required"));

        var labId = GetLabId();
        var (userId, userName) = GetUserInfo();
        var responses = new List<ResultResponse>();

        foreach (var item in request.Results)
        {
            decimal? numericValue = null;
            if (decimal.TryParse(item.ResultValue, out var parsed))
                numericValue = parsed;

            var existingResult = await _resultRepository.GetByOrderTestIdAsync(item.OrderTestId);

            decimal? refLow = existingResult?.ReferenceLow;
            decimal? refHigh = existingResult?.ReferenceHigh;
            decimal? critLow = existingResult?.CriticalLow;
            decimal? critHigh = existingResult?.CriticalHigh;

            var flag = ResultFlagHelper.EvaluateFlag(numericValue, refLow, refHigh, critLow, critHigh);
            var isCritical = ResultFlagHelper.IsCriticalFlag(flag);

            var result = new TestResult
            {
                Id = Guid.NewGuid(),
                LabId = labId,
                OrderId = existingResult?.OrderId ?? Guid.Empty,
                OrderTestId = item.OrderTestId,
                TestId = existingResult?.TestId ?? Guid.Empty,
                TestCode = existingResult?.TestCode ?? string.Empty,
                TestName = existingResult?.TestName ?? string.Empty,
                ParameterName = existingResult?.ParameterName,
                ResultValue = item.ResultValue,
                ResultNumeric = numericValue,
                ResultUnit = item.ResultUnit,
                ReferenceLow = refLow,
                ReferenceHigh = refHigh,
                CriticalLow = critLow,
                CriticalHigh = critHigh,
                Flag = flag,
                IsCritical = isCritical,
                RawValue = item.ResultValue,
                Method = item.Method,
                Remarks = item.Remarks,
                EnteredBy = userId,
                EnteredByName = userName,
                EnteredAt = DateTime.UtcNow,
                Status = "entered",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _resultRepository.CreateResultAsync(result);

            if (isCritical)
            {
                var alert = new CriticalAlert
                {
                    Id = Guid.NewGuid(),
                    LabId = labId,
                    ResultId = created.Id,
                    OrderId = created.OrderId,
                    TestName = created.TestName,
                    ResultValue = created.ResultValue,
                    CriticalType = flag,
                    CreatedAt = DateTime.UtcNow
                };
                await _resultRepository.CreateCriticalAlertAsync(alert);
            }

            responses.Add(MapToResponse(created));
        }

        return Created("/results/batch", ApiResponse<List<ResultResponse>>.Ok(responses, $"{responses.Count} results entered successfully"));
    }

    /// <summary>
    /// Get pending results worklist grouped by priority.
    /// </summary>
    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var (items, total) = await _resultRepository.GetWorklistAsync(labId, department, status, page, pageSize);

        var response = new WorklistResponse
        {
            Items = items.Select(i => new WorklistItem
            {
                OrderId = i.OrderId.ToString(),
                PatientName = i.PatientName,
                PatientUhid = i.PatientUhid,
                TestCode = i.TestCode,
                TestName = i.TestName,
                Priority = i.Priority,
                Status = i.Status,
                OrderedAt = i.OrderedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<WorklistResponse>.Ok(response));
    }

    /// <summary>
    /// Get all results for a specific order.
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId)
    {
        var results = await _resultRepository.GetByOrderIdAsync(orderId);
        var responses = new List<ResultResponse>();

        foreach (var result in results)
        {
            var verifications = await _resultRepository.GetVerificationsAsync(result.Id);
            responses.Add(MapToResponse(result, verifications));
        }

        return Ok(ApiResponse<List<ResultResponse>>.Ok(responses));
    }

    /// <summary>
    /// Get a single result with full verification history.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _resultRepository.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Result not found"));

        var verifications = await _resultRepository.GetVerificationsAsync(result.Id);
        var response = MapToResponse(result, verifications);

        return Ok(ApiResponse<ResultResponse>.Ok(response));
    }

    /// <summary>
    /// Verify a result (technician or pathologist level).
    /// </summary>
    [HttpPatch("{id}/verify")]
    public async Task<IActionResult> VerifyResult(Guid id, [FromBody] VerifyResultRequest request)
    {
        var result = await _resultRepository.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Result not found"));

        if (string.IsNullOrWhiteSpace(request.VerificationLevel))
            return BadRequest(ApiResponse<object>.Fail("Verification level is required"));

        var (userId, userName) = GetUserInfo();

        var verification = new ResultVerification
        {
            Id = Guid.NewGuid(),
            ResultId = id,
            VerificationLevel = request.VerificationLevel,
            VerifiedBy = userId ?? Guid.Empty,
            VerifiedByName = userName ?? "Unknown",
            VerifiedAt = DateTime.UtcNow,
            Status = request.Status,
            Comments = request.Comments,
            PreviousValue = result.ResultValue,
            CorrectedValue = request.CorrectedValue
        };

        await _resultRepository.AddVerificationAsync(verification);

        // Update result status based on verification
        var newStatus = request.Status == "rejected" ? "rejected" : request.VerificationLevel switch
        {
            "tech_verified" => "tech_verified",
            "pathologist_verified" => "pathologist_verified",
            "released" => "released",
            _ => result.Status
        };

        await _resultRepository.UpdateResultStatusAsync(id, newStatus);

        // If corrected value provided, update the result value
        if (!string.IsNullOrEmpty(request.CorrectedValue))
        {
            await _resultRepository.UpdateResultAsync(id, request.CorrectedValue, result.ResultUnit, result.Remarks);
        }

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = newStatus }, "Result verified successfully"));
    }

    /// <summary>
    /// Get unacknowledged critical value alerts.
    /// </summary>
    [HttpGet("critical-alerts")]
    public async Task<IActionResult> GetCriticalAlerts([FromQuery] bool unacknowledgedOnly = true)
    {
        var labId = GetLabId();

        var alerts = await _resultRepository.GetCriticalAlertsAsync(labId, unacknowledgedOnly);

        var responses = alerts.Select(a => new CriticalAlertResponse
        {
            Id = a.Id.ToString(),
            PatientName = a.PatientName,
            TestName = a.TestName,
            ResultValue = a.ResultValue,
            CriticalType = a.CriticalType,
            CreatedAt = a.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Acknowledged = a.AcknowledgedAt.HasValue
        }).ToList();

        return Ok(ApiResponse<List<CriticalAlertResponse>>.Ok(responses));
    }

    /// <summary>
    /// Acknowledge a critical alert.
    /// </summary>
    [HttpPatch("critical-alerts/{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeCriticalAlert(Guid id)
    {
        var (userId, _) = GetUserInfo();

        await _resultRepository.AcknowledgeCriticalAlertAsync(id, userId ?? Guid.Empty);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), acknowledged = true }, "Critical alert acknowledged"));
    }

    /// <summary>
    /// Delta check - compare current result with patient's previous results.
    /// </summary>
    [HttpGet("delta-check/{orderTestId}")]
    public async Task<IActionResult> DeltaCheck(Guid orderTestId)
    {
        var labId = GetLabId();

        var currentResult = await _resultRepository.GetByOrderTestIdAsync(orderTestId);
        if (currentResult == null)
            return NotFound(ApiResponse<object>.Fail("Result not found for this order test"));

        // Get patient's previous results for the same test
        // We need the patient ID from the order - for now query by test code
        var previousResults = await _resultRepository.GetPatientPreviousResultsAsync(
            labId, null, currentResult.TestCode, 5);

        // Filter out the current result and get the most recent previous one
        var previous = previousResults
            .Where(r => r.Id != currentResult.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        decimal? percentChange = null;
        if (previous?.ResultNumeric.HasValue == true && currentResult.ResultNumeric.HasValue && previous.ResultNumeric != 0)
        {
            percentChange = Math.Round(
                ((currentResult.ResultNumeric.Value - previous.ResultNumeric.Value) / previous.ResultNumeric.Value) * 100, 2);
        }

        var response = new DeltaCheckResponse
        {
            TestName = currentResult.TestName,
            CurrentValue = currentResult.ResultValue,
            PreviousValue = previous?.ResultValue,
            PreviousDate = previous?.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            PercentChange = percentChange
        };

        return Ok(ApiResponse<DeltaCheckResponse>.Ok(response));
    }

    private static ResultResponse MapToResponse(TestResult result, List<ResultVerification>? verifications = null)
    {
        return new ResultResponse
        {
            Id = result.Id.ToString(),
            TestCode = result.TestCode,
            TestName = result.TestName,
            ParameterName = result.ParameterName,
            ResultValue = result.ResultValue,
            ResultUnit = result.ResultUnit,
            ReferenceLow = result.ReferenceLow,
            ReferenceHigh = result.ReferenceHigh,
            Flag = result.Flag,
            IsCritical = result.IsCritical,
            Status = result.Status,
            EnteredByName = result.EnteredByName,
            EnteredAt = result.EnteredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Verifications = verifications?.Select(v => new VerificationResponse
            {
                Id = v.Id.ToString(),
                VerificationLevel = v.VerificationLevel,
                VerifiedByName = v.VerifiedByName,
                VerifiedAt = v.VerifiedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Status = v.Status,
                Comments = v.Comments,
                CorrectedValue = v.CorrectedValue
            }).ToList() ?? new()
        };
    }
}
