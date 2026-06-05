namespace LIS.OrderService.Models;

public class LabOrder
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public int? PatientAge { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientMobile { get; set; }
    public string? ExternalOrderId { get; set; }
    public string SourceSystem { get; set; } = "manual";
    public string Priority { get; set; } = "routine";
    public string Status { get; set; } = "ordered";
    public Guid? OrderedBy { get; set; }
    public string? OrderedByName { get; set; }
    public string? ClinicalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LabOrderTest
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Status { get; set; } = "ordered";
    public Guid? SpecimenId { get; set; }
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public string? ResultFlag { get; set; }
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ReportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
