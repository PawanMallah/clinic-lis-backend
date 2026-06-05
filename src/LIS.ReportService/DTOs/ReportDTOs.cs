namespace LIS.ReportService.DTOs;

public class GenerateReportRequest
{
    public Guid OrderId { get; set; }
    public string? PatientName { get; set; }
}

public class ReportResponse
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string? ReportNumber { get; set; }
    public string? PatientName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? ReportPdfUrl { get; set; }
    public string? GeneratedAt { get; set; }
    public string? SignedByName { get; set; }
    public string? SignedAt { get; set; }
    public string? DeliveredAt { get; set; }
    public string? DeliveredVia { get; set; }
    public string? AmendmentReason { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class SignReportRequest
{
    public Guid SignedBy { get; set; }
    public string SignedByName { get; set; } = string.Empty;
}

public class DeliverReportRequest
{
    public string DeliveryMethod { get; set; } = "email";
    public string? RecipientAddress { get; set; }
}

public class AmendReportRequest
{
    public string AmendmentReason { get; set; } = string.Empty;
}

public class ReportListResponse
{
    public List<ReportResponse> Reports { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
