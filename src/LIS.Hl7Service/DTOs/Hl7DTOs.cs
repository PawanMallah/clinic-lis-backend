namespace LIS.Hl7Service.DTOs;

public class CreateConnectionRequest
{
    public string InstrumentName { get; set; } = string.Empty;
    public string? InstrumentModel { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; }
    public string Protocol { get; set; } = "hl7v2";
    public string Direction { get; set; } = "bidirectional";
    public bool AutoReconnect { get; set; } = true;
}

public class ConnectionResponse
{
    public string Id { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public string? InstrumentModel { get; set; }
    public string? Manufacturer { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public string? LastConnectedAt { get; set; }
    public string? LastMessageAt { get; set; }
}

public class MessageLogResponse
{
    public string Id { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string? MessageType { get; set; }
    public string? TriggerEvent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class MessageLogDetailResponse
{
    public string Id { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string? MessageType { get; set; }
    public string? TriggerEvent { get; set; }
    public string? ControlId { get; set; }
    public string RawMessage { get; set; } = string.Empty;
    public string? ParsedJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateMappingRequest
{
    public string InstrumentTestCode { get; set; } = string.Empty;
    public string? InstrumentTestName { get; set; }
    public Guid? LisTestId { get; set; }
    public string? LisTestCode { get; set; }
    public string? LisTestName { get; set; }
}

public class MappingResponse
{
    public string Id { get; set; } = string.Empty;
    public string InstrumentTestCode { get; set; } = string.Empty;
    public string? InstrumentTestName { get; set; }
    public string? LisTestCode { get; set; }
    public string? LisTestName { get; set; }
    public bool IsActive { get; set; }
}
