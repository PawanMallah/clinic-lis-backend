namespace LIS.QcService.DTOs;

public class CreateQcMaterialRequest
{
    public string MaterialName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string Level { get; set; } = "normal";
}

public class CreateTargetValueRequest
{
    public Guid MaterialId { get; set; }
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal ExpectedMean { get; set; }
    public decimal ExpectedSd { get; set; }
    public string? Unit { get; set; }
}

public class EnterQcRecordRequest
{
    public Guid MaterialId { get; set; }
    public Guid TargetValueId { get; set; }
    public Guid? InstrumentId { get; set; }
    public string? InstrumentName { get; set; }
    public decimal MeasuredValue { get; set; }
    public string? Comments { get; set; }
}

public class QcRecordResponse
{
    public string Id { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public decimal MeasuredValue { get; set; }
    public decimal ExpectedMean { get; set; }
    public decimal ExpectedSd { get; set; }
    public decimal? SdIndex { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? WestgardViolation { get; set; }
    public string? RunDate { get; set; }
    public string? RecordedByName { get; set; }
}

public class LeveyJenningsDataPoint
{
    public string Date { get; set; } = string.Empty;
    public decimal MeasuredValue { get; set; }
    public decimal Mean { get; set; }
    public decimal PlusOneSd { get; set; }
    public decimal MinusOneSd { get; set; }
    public decimal PlusTwoSd { get; set; }
    public decimal MinusTwoSd { get; set; }
    public decimal PlusThreeSd { get; set; }
    public decimal MinusThreeSd { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class QcStatusResponse
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? InstrumentName { get; set; }
    public string? LastQcDate { get; set; }
    public string? LastQcStatus { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}
