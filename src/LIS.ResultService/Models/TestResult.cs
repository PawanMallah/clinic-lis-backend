namespace LIS.ResultService.Models;

public class TestResult
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderTestId { get; set; }
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? ParameterName { get; set; }
    public string? ResultValue { get; set; }
    public decimal? ResultNumeric { get; set; }
    public string? ResultUnit { get; set; }
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public decimal? CriticalLow { get; set; }
    public decimal? CriticalHigh { get; set; }
    public string Flag { get; set; } = "normal";
    public bool IsCritical { get; set; }
    public Guid? InstrumentId { get; set; }
    public string? InstrumentName { get; set; }
    public string? RawValue { get; set; }
    public string? Method { get; set; }
    public string? Remarks { get; set; }
    public Guid? EnteredBy { get; set; }
    public string? EnteredByName { get; set; }
    public DateTime? EnteredAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ResultVerification
{
    public Guid Id { get; set; }
    public Guid ResultId { get; set; }
    public string VerificationLevel { get; set; } = string.Empty;
    public Guid VerifiedBy { get; set; }
    public string VerifiedByName { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public string Status { get; set; } = "approved";
    public string? Comments { get; set; }
    public string? PreviousValue { get; set; }
    public string? CorrectedValue { get; set; }
}

public class CriticalAlert
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid ResultId { get; set; }
    public Guid OrderId { get; set; }
    public string? PatientName { get; set; }
    public string? TestName { get; set; }
    public string? ResultValue { get; set; }
    public string CriticalType { get; set; } = string.Empty;
    public Guid? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool NotifiedDoctor { get; set; }
    public DateTime CreatedAt { get; set; }
}
