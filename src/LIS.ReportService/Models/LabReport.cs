namespace LIS.ReportService.Models;

public class LabReport
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid OrderId { get; set; }
    public string? PatientName { get; set; }
    public string? ReportNumber { get; set; }
    public string Status { get; set; } = "draft";
    public string? ReportPdfUrl { get; set; }
    public string? ReportJson { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveredVia { get; set; }
    public int Version { get; set; } = 1;
    public string? AmendmentReason { get; set; }
    public Guid? SignedBy { get; set; }
    public string? SignedByName { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
