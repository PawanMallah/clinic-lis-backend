namespace LIS.QcService.Models;

public class QcMaterial
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string Level { get; set; } = "normal";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class QcTargetValue
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal ExpectedMean { get; set; }
    public decimal ExpectedSd { get; set; }
    public string? Unit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QcRecord
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid TargetValueId { get; set; }
    public Guid? InstrumentId { get; set; }
    public string? InstrumentName { get; set; }
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public decimal MeasuredValue { get; set; }
    public decimal ExpectedMean { get; set; }
    public decimal ExpectedSd { get; set; }
    public decimal? SdIndex { get; set; }
    public string Status { get; set; } = "in_control";
    public string? WestgardViolation { get; set; }
    public DateTime RunDate { get; set; }
    public Guid? RecordedBy { get; set; }
    public string? RecordedByName { get; set; }
    public DateTime? RecordedAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QcBlock
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid TestId { get; set; }
    public Guid? InstrumentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? BlockedBy { get; set; }
    public DateTime? BlockedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
