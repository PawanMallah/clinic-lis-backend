namespace LIS.SpecimenService.Models;

public class Specimen
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid OrderId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = string.Empty;
    public string? TubeType { get; set; }
    public string? TubeColor { get; set; }
    public decimal? VolumeMl { get; set; }
    public Guid? CollectedBy { get; set; }
    public string? CollectedByName { get; set; }
    public DateTime? CollectedAt { get; set; }
    public Guid? ReceivedBy { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string Status { get; set; } = "pending";
    public string? RejectReason { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SpecimenTracking
{
    public Guid Id { get; set; }
    public Guid SpecimenId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? PerformedBy { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
}
