using LIS.ReportService.DTOs;
using LIS.ReportService.Models;
using LIS.ReportService.Repositories;
using LIS.ReportService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace LIS.ReportService.Controllers;

[ApiController]
[Route("[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _reportRepository;
    private readonly LabReportPdfGenerator _pdfGenerator;

    public ReportsController(IReportRepository reportRepository, LabReportPdfGenerator pdfGenerator)
    {
        _reportRepository = reportRepository;
        _pdfGenerator = pdfGenerator;
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

    [HttpPost("generate/{orderId}")]
    public async Task<IActionResult> GenerateReport(Guid orderId, [FromBody] GenerateReportRequest? request = null)
    {
        var labId = GetLabId();

        // Check if report already exists for this order
        var existing = await _reportRepository.GetByOrderIdAsync(orderId);
        if (existing != null && existing.Status == "draft")
            return Conflict(ApiResponse<object>.Fail("A draft report already exists for this order"));

        var reportNumber = await _reportRepository.GetNextReportNumberAsync(labId);

        var report = new LabReport
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            OrderId = orderId,
            PatientName = request?.PatientName,
            ReportNumber = reportNumber,
            Status = "draft",
            GeneratedAt = DateTime.UtcNow,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _reportRepository.CreateReportAsync(report);
        var response = MapToResponse(created);

        return Created($"/reports/{created.Id}", ApiResponse<ReportResponse>.Ok(response, "Report generated successfully"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null)
            return NotFound(ApiResponse<object>.Fail("Report not found"));

        var response = MapToResponse(report);
        return Ok(ApiResponse<ReportResponse>.Ok(response));
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId)
    {
        var report = await _reportRepository.GetByOrderIdAsync(orderId);
        if (report == null)
            return NotFound(ApiResponse<object>.Fail("Report not found for this order"));

        var response = MapToResponse(report);
        return Ok(ApiResponse<ReportResponse>.Ok(response));
    }

    /// <summary>
    /// Generate a PDF for a report by ID. Builds LabReportData from the supplied body
    /// containing all mandatory Section 10.1/10.2 fields and returns the PDF bytes.
    /// </summary>
    [HttpPost("{id}/pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id, [FromBody] GeneratePdfRequest request)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null)
            return NotFound(ApiResponse<object>.Fail("Report not found"));

        // Use reported time from request or fallback to report timestamps
        if (string.IsNullOrEmpty(request.ReportedAt) && report.GeneratedAt.HasValue)
            request.ReportedAt = report.GeneratedAt.Value.ToString("dd/MM/yyyy HH:mm");

        if (string.IsNullOrEmpty(request.AccessionNumber) && !string.IsNullOrEmpty(report.ReportNumber))
            request.AccessionNumber = report.ReportNumber;

        if (string.IsNullOrEmpty(request.ReportStatus))
            request.ReportStatus = report.Status switch
            {
                "finalized" => "Final",
                "draft" => "Preliminary",
                "amended" => "Corrected",
                _ => report.Status
            };

        var data = request.ToLabReportData();
        var pdfBytes = _pdfGenerator.GenerateReport(data);

        return File(pdfBytes, "application/pdf", $"report-{report.ReportNumber ?? id.ToString()}.pdf");
    }

    /// <summary>
    /// Generate a PDF from raw data without an existing report record.
    /// Accepts full LabReportData payload and returns PDF bytes directly.
    /// </summary>
    [HttpPost("pdf/generate")]
    public IActionResult GeneratePdfDirect([FromBody] GeneratePdfRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PatientName))
            return BadRequest(ApiResponse<object>.Fail("PatientName is required"));

        if (string.IsNullOrWhiteSpace(request.AccessionNumber))
            return BadRequest(ApiResponse<object>.Fail("AccessionNumber is required"));

        var data = request.ToLabReportData();
        var pdfBytes = _pdfGenerator.GenerateReport(data);

        return File(pdfBytes, "application/pdf", $"report-{request.AccessionNumber}.pdf");
    }

    [HttpPatch("{id}/sign")]
    public async Task<IActionResult> SignReport(Guid id, [FromBody] SignReportRequest request)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null)
            return NotFound(ApiResponse<object>.Fail("Report not found"));

        if (report.Status != "draft" && report.Status != "amended")
            return BadRequest(ApiResponse<object>.Fail("Only draft or amended reports can be signed"));

        if (string.IsNullOrWhiteSpace(request.SignedByName))
        {
            var (_, userName) = GetUserInfo();
            request.SignedByName = userName ?? "Unknown";
        }

        await _reportRepository.SignReportAsync(id, request.SignedBy, request.SignedByName);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = "finalized" }, "Report signed and finalized"));
    }

    [HttpPatch("{id}/deliver")]
    public async Task<IActionResult> DeliverReport(Guid id, [FromBody] DeliverReportRequest request)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null)
            return NotFound(ApiResponse<object>.Fail("Report not found"));

        if (report.Status != "finalized")
            return BadRequest(ApiResponse<object>.Fail("Only finalized reports can be delivered"));

        await _reportRepository.DeliverReportAsync(id, request.DeliveryMethod);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = "delivered", deliveredVia = request.DeliveryMethod }, "Report marked as delivered"));
    }

    [HttpGet]
    public async Task<IActionResult> GetReports(
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var reports = await _reportRepository.GetReportsAsync(labId, status, fromDate, toDate, page, pageSize);
        var total = await _reportRepository.GetReportCountAsync(labId, status, fromDate, toDate);

        var result = new ReportListResponse
        {
            Reports = reports.Select(MapToResponse).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<ReportListResponse>.Ok(result));
    }

    [HttpPost("{id}/amend")]
    public async Task<IActionResult> AmendReport(Guid id, [FromBody] AmendReportRequest request)
    {
        var original = await _reportRepository.GetByIdAsync(id);
        if (original == null)
            return NotFound(ApiResponse<object>.Fail("Report not found"));

        if (original.Status != "finalized" && original.Status != "delivered")
            return BadRequest(ApiResponse<object>.Fail("Only finalized or delivered reports can be amended"));

        if (string.IsNullOrWhiteSpace(request.AmendmentReason))
            return BadRequest(ApiResponse<object>.Fail("Amendment reason is required"));

        // Mark original as amended
        await _reportRepository.UpdateStatusAsync(id, "amended");

        var labId = GetLabId();
        var reportNumber = await _reportRepository.GetNextReportNumberAsync(labId);

        var amendment = new LabReport
        {
            Id = Guid.NewGuid(),
            LabId = original.LabId,
            OrderId = original.OrderId,
            PatientName = original.PatientName,
            ReportNumber = reportNumber,
            Status = "draft",
            ReportPdfUrl = original.ReportPdfUrl,
            ReportJson = original.ReportJson,
            GeneratedAt = DateTime.UtcNow,
            Version = original.Version + 1,
            AmendmentReason = request.AmendmentReason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _reportRepository.AmendReportAsync(amendment);
        var response = MapToResponse(created);

        return Created($"/reports/{created.Id}", ApiResponse<ReportResponse>.Ok(response, "Amendment created"));
    }

    // ─── Mapping ─────────────────────────────────────────────────────────

    private static ReportResponse MapToResponse(LabReport report)
    {
        return new ReportResponse
        {
            Id = report.Id.ToString(),
            OrderId = report.OrderId.ToString(),
            ReportNumber = report.ReportNumber,
            PatientName = report.PatientName,
            Status = report.Status,
            Version = report.Version,
            ReportPdfUrl = report.ReportPdfUrl,
            GeneratedAt = report.GeneratedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            SignedByName = report.SignedByName,
            SignedAt = report.SignedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            DeliveredAt = report.DeliveredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            DeliveredVia = report.DeliveredVia,
            AmendmentReason = report.AmendmentReason,
            CreatedAt = report.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }
}
