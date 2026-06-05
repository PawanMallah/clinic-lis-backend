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

public class AutoVerificationRule
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid? TestId { get; set; }
    public string? TestCode { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool RequireQcPass { get; set; } = true;
    public decimal DeltaCheckPercent { get; set; } = 20;
    public int DeltaCheckHours { get; set; } = 48;
    public bool ExcludeCritical { get; set; } = true;
    public bool ExcludeFirstResult { get; set; } = true;
    public bool ExcludeNeonatal { get; set; }
    public bool ExcludeCriticalCare { get; set; }
    public bool RequireInReportableRange { get; set; } = true;
    public bool RequireNoInstrumentFlags { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AutoVerificationLog
{
    public Guid Id { get; set; }
    public Guid ResultId { get; set; }
    public Guid OrderId { get; set; }
    public string? TestCode { get; set; }
    public bool Passed { get; set; }
    public string FailureReasons { get; set; } = "[]";
    public DateTime CheckedAt { get; set; }
}

public class DigitalSignature
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public string? LicenseNumber { get; set; }
    public string? SignatureImage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
