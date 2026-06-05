namespace LIS.Hl7Service.Models;

public class Hl7Connection
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public string InstrumentName { get; set; } = string.Empty;
    public string? InstrumentModel { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = "hl7v2";
    public string Direction { get; set; } = "bidirectional";
    public string Status { get; set; } = "disconnected";
    public bool AutoReconnect { get; set; } = true;
    public DateTime? LastConnectedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? TestCodeMapping { get; set; }
    public string? Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Hl7MessageLog
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? MessageType { get; set; }
    public string? TriggerEvent { get; set; }
    public string? ControlId { get; set; }
    public string RawMessage { get; set; } = string.Empty;
    public string? ParsedJson { get; set; }
    public string Status { get; set; } = "received";
    public string? ErrorMessage { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InstrumentTestMapping
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string InstrumentTestCode { get; set; } = string.Empty;
    public string? InstrumentTestName { get; set; }
    public Guid? LisTestId { get; set; }
    public string? LisTestCode { get; set; }
    public string? LisTestName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
