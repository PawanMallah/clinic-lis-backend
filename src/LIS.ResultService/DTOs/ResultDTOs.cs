namespace LIS.ResultService.DTOs;

public class EnterResultRequest
{
    public Guid OrderTestId { get; set; }
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public string? Method { get; set; }
    public string? Remarks { get; set; }
}

public class BatchEnterResultsRequest
{
    public List<EnterResultRequest> Results { get; set; } = new();
}

public class VerifyResultRequest
{
    public string VerificationLevel { get; set; } = string.Empty;
    public string Status { get; set; } = "approved";
    public string? Comments { get; set; }
    public string? CorrectedValue { get; set; }
}

public class ResultResponse
{
    public string Id { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? ParameterName { get; set; }
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public string Flag { get; set; } = "normal";
    public bool IsCritical { get; set; }
    public string Status { get; set; } = "pending";
    public string? EnteredByName { get; set; }
    public string? EnteredAt { get; set; }
    public List<VerificationResponse> Verifications { get; set; } = new();
}

public class VerificationResponse
{
    public string Id { get; set; } = string.Empty;
    public string VerificationLevel { get; set; } = string.Empty;
    public string VerifiedByName { get; set; } = string.Empty;
    public string VerifiedAt { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string? CorrectedValue { get; set; }
}

public class WorklistItem
{
    public string OrderId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Priority { get; set; } = "routine";
    public string Status { get; set; } = "pending";
    public string? OrderedAt { get; set; }
}

public class WorklistResponse
{
    public List<WorklistItem> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CriticalAlertResponse
{
    public string Id { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? TestName { get; set; }
    public string? ResultValue { get; set; }
    public string CriticalType { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public bool Acknowledged { get; set; }
}

public class DeltaCheckResponse
{
    public string TestName { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
    public string? PreviousValue { get; set; }
    public string? PreviousDate { get; set; }
    public decimal? PercentChange { get; set; }
}

// --- Auto-verification DTOs ---

public class AutoVerificationRuleDto
{
    public string? Id { get; set; }
    public string? TestId { get; set; }
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
}

public class AutoVerificationRuleResponse
{
    public string Id { get; set; } = string.Empty;
    public string? TestId { get; set; }
    public string? TestCode { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool RequireQcPass { get; set; }
    public decimal DeltaCheckPercent { get; set; }
    public int DeltaCheckHours { get; set; }
    public bool ExcludeCritical { get; set; }
    public bool ExcludeFirstResult { get; set; }
    public bool ExcludeNeonatal { get; set; }
    public bool ExcludeCriticalCare { get; set; }
    public bool RequireInReportableRange { get; set; }
    public bool RequireNoInstrumentFlags { get; set; }
    public string? CreatedAt { get; set; }
}

public class AutoVerificationResultInfo
{
    public bool Passed { get; set; }
    public List<string> FailureReasons { get; set; } = new();
    public string? AutoVerifiedAt { get; set; }
}

public class ReflexTriggerResponse
{
    public string RuleId { get; set; } = string.Empty;
    public string ReflexTestId { get; set; } = string.Empty;
    public string ReflexTestCode { get; set; } = string.Empty;
    public string ReflexTestName { get; set; } = string.Empty;
    public bool AutoOrder { get; set; }
    public string TriggerReason { get; set; } = string.Empty;
}

public class EnterResultResponse
{
    public ResultResponse Result { get; set; } = new();
    public AutoVerificationResultInfo? AutoVerification { get; set; }
    public List<ReflexTriggerResponse> ReflexTriggered { get; set; } = new();
}

// --- Digital Signature DTOs ---

public class CreateDigitalSignatureRequest
{
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public string? LicenseNumber { get; set; }
    public string? SignatureImage { get; set; }
}

public class DigitalSignatureResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public string? LicenseNumber { get; set; }
    public bool HasSignatureImage { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedAt { get; set; }
}
