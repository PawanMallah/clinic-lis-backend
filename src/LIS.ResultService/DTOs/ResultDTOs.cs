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
