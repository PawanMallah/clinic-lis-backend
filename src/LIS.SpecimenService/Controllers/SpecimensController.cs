using LIS.SpecimenService.DTOs;
using LIS.SpecimenService.Models;
using LIS.SpecimenService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using Shared.Helpers;

namespace LIS.SpecimenService.Controllers;

[ApiController]
[Route("[controller]")]
public class SpecimensController : ControllerBase
{
    private readonly ISpecimenRepository _specimenRepository;

    public SpecimensController(ISpecimenRepository specimenRepository)
    {
        _specimenRepository = specimenRepository;
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

    [HttpPost]
    public async Task<IActionResult> CreateSpecimen([FromBody] CreateSpecimenRequest request)
    {
        if (request.OrderId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Order ID is required"));

        if (string.IsNullOrWhiteSpace(request.SpecimenType))
            return BadRequest(ApiResponse<object>.Fail("Specimen type is required"));

        var labId = GetLabId();
        var (userId, userName) = GetUserInfo();
        var sequence = await _specimenRepository.GetTodaySequenceAsync();
        var barcode = BarcodeHelper.Generate(sequence);

        var specimen = new Specimen
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            OrderId = request.OrderId,
            Barcode = barcode,
            SpecimenType = request.SpecimenType,
            TubeType = request.TubeType,
            TubeColor = request.TubeColor,
            VolumeMl = request.VolumeMl,
            Status = "collected",
            CollectedBy = userId,
            CollectedByName = userName,
            CollectedAt = DateTime.UtcNow,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _specimenRepository.CreateAsync(specimen);

        // Add tracking entry
        var tracking = new SpecimenTracking
        {
            Id = Guid.NewGuid(),
            SpecimenId = created.Id,
            Action = "collected",
            PerformedBy = userId,
            PerformedByName = userName,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Notes
        };
        await _specimenRepository.AddTrackingAsync(tracking);

        var response = MapToResponse(created);
        return Created($"/specimens/{created.Id}", ApiResponse<SpecimenResponse>.Ok(response, "Specimen registered successfully"));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? barcode,
        [FromQuery] string? status,
        [FromQuery] Guid? orderId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var specimens = await _specimenRepository.GetAllAsync(labId, barcode, status, orderId, page, pageSize);
        var total = await _specimenRepository.GetCountAsync(labId, barcode, status, orderId);

        var responses = specimens.Select(s => MapToResponse(s)).ToList();

        var result = new SpecimenListResponse
        {
            Specimens = responses,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<SpecimenListResponse>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var specimen = await _specimenRepository.GetByIdAsync(id);
        if (specimen == null)
            return NotFound(ApiResponse<object>.Fail("Specimen not found"));

        var tracking = await _specimenRepository.GetTrackingAsync(id);
        var response = MapToResponse(specimen, tracking);

        return Ok(ApiResponse<SpecimenResponse>.Ok(response));
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var specimen = await _specimenRepository.GetByBarcodeAsync(barcode);
        if (specimen == null)
            return NotFound(ApiResponse<object>.Fail("Specimen not found"));

        var tracking = await _specimenRepository.GetTrackingAsync(specimen.Id);
        var response = MapToResponse(specimen, tracking);

        return Ok(ApiResponse<SpecimenResponse>.Ok(response));
    }

    [HttpPatch("{id}/collect")]
    public async Task<IActionResult> CollectSpecimen(Guid id, [FromBody] CollectSpecimenRequest? request)
    {
        var specimen = await _specimenRepository.GetByIdAsync(id);
        if (specimen == null)
            return NotFound(ApiResponse<object>.Fail("Specimen not found"));

        if (specimen.Status != "pending")
            return BadRequest(ApiResponse<object>.Fail("Specimen is not in pending status"));

        var (userId, userName) = GetUserInfo();

        specimen.Status = "collected";
        specimen.CollectedBy = userId;
        specimen.CollectedByName = userName;
        specimen.CollectedAt = DateTime.UtcNow;

        await _specimenRepository.UpdateAsync(specimen);

        var tracking = new SpecimenTracking
        {
            Id = Guid.NewGuid(),
            SpecimenId = id,
            Action = "collected",
            PerformedBy = userId,
            PerformedByName = userName,
            PerformedAt = DateTime.UtcNow,
            Notes = request?.Notes
        };
        await _specimenRepository.AddTrackingAsync(tracking);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = "collected" }, "Specimen collected"));
    }

    [HttpPatch("{id}/receive")]
    public async Task<IActionResult> ReceiveSpecimen(Guid id, [FromBody] ReceiveSpecimenRequest? request)
    {
        var specimen = await _specimenRepository.GetByIdAsync(id);
        if (specimen == null)
            return NotFound(ApiResponse<object>.Fail("Specimen not found"));

        if (specimen.Status == "received")
            return BadRequest(ApiResponse<object>.Fail("Specimen already received"));

        if (specimen.Status == "rejected")
            return BadRequest(ApiResponse<object>.Fail("Cannot receive a rejected specimen"));

        var (userId, userName) = GetUserInfo();

        specimen.Status = "received";
        specimen.ReceivedBy = userId;
        specimen.ReceivedByName = userName;
        specimen.ReceivedAt = DateTime.UtcNow;

        await _specimenRepository.UpdateAsync(specimen);

        var tracking = new SpecimenTracking
        {
            Id = Guid.NewGuid(),
            SpecimenId = id,
            Action = "received",
            PerformedBy = userId,
            PerformedByName = userName,
            PerformedAt = DateTime.UtcNow,
            Notes = request?.Notes
        };
        await _specimenRepository.AddTrackingAsync(tracking);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = "received" }, "Specimen received"));
    }

    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> RejectSpecimen(Guid id, [FromBody] RejectSpecimenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(ApiResponse<object>.Fail("Reject reason is required"));

        var specimen = await _specimenRepository.GetByIdAsync(id);
        if (specimen == null)
            return NotFound(ApiResponse<object>.Fail("Specimen not found"));

        if (specimen.Status == "rejected")
            return BadRequest(ApiResponse<object>.Fail("Specimen already rejected"));

        var (userId, userName) = GetUserInfo();

        specimen.Status = "rejected";
        specimen.RejectReason = request.Reason;
        specimen.RejectedBy = userId;
        specimen.RejectedAt = DateTime.UtcNow;

        await _specimenRepository.UpdateAsync(specimen);

        var tracking = new SpecimenTracking
        {
            Id = Guid.NewGuid(),
            SpecimenId = id,
            Action = "rejected",
            PerformedBy = userId,
            PerformedByName = userName,
            PerformedAt = DateTime.UtcNow,
            Notes = $"Rejected: {request.Reason}"
        };
        await _specimenRepository.AddTrackingAsync(tracking);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = "rejected", reason = request.Reason }, "Specimen rejected"));
    }

    [HttpGet("{id}/tracking")]
    public async Task<IActionResult> GetTracking(Guid id)
    {
        var specimen = await _specimenRepository.GetByIdAsync(id);
        if (specimen == null)
            return NotFound(ApiResponse<object>.Fail("Specimen not found"));

        var tracking = await _specimenRepository.GetTrackingAsync(id);
        var entries = tracking.Select(t => new SpecimenTrackingResponse
        {
            Id = t.Id.ToString(),
            Action = t.Action,
            PerformedByName = t.PerformedByName,
            PerformedAt = t.PerformedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Notes = t.Notes
        }).ToList();

        return Ok(ApiResponse<List<SpecimenTrackingResponse>>.Ok(entries));
    }

    private static SpecimenResponse MapToResponse(Specimen specimen, List<SpecimenTracking>? tracking = null)
    {
        return new SpecimenResponse
        {
            Id = specimen.Id.ToString(),
            LabId = specimen.LabId.ToString(),
            OrderId = specimen.OrderId.ToString(),
            Barcode = specimen.Barcode,
            SpecimenType = specimen.SpecimenType,
            TubeType = specimen.TubeType,
            TubeColor = specimen.TubeColor,
            VolumeMl = specimen.VolumeMl,
            Status = specimen.Status,
            CollectedByName = specimen.CollectedByName,
            CollectedAt = specimen.CollectedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ReceivedByName = specimen.ReceivedByName,
            ReceivedAt = specimen.ReceivedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            RejectReason = specimen.RejectReason,
            Notes = specimen.Notes,
            Tracking = tracking?.Select(t => new SpecimenTrackingResponse
            {
                Id = t.Id.ToString(),
                Action = t.Action,
                PerformedByName = t.PerformedByName,
                PerformedAt = t.PerformedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Notes = t.Notes
            }).ToList() ?? new(),
            CreatedAt = specimen.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }
}
