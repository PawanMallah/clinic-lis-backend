namespace LIS.Hl7Service.Hl7;

/// <summary>
/// Represents a parsed HL7 v2.x message with segments.
/// HL7 messages use | as field separator, ^ as component separator.
/// </summary>
public class Hl7Message
{
    public string MessageType { get; set; } = string.Empty; // e.g., "ORU"
    public string TriggerEvent { get; set; } = string.Empty; // e.g., "R01"
    public string ControlId { get; set; } = string.Empty;
    public string RawMessage { get; set; } = string.Empty;
    public List<Hl7Segment> Segments { get; set; } = new();

    public Hl7Segment? GetSegment(string name) => Segments.FirstOrDefault(s => s.Name == name);
    public List<Hl7Segment> GetSegments(string name) => Segments.Where(s => s.Name == name).ToList();
}

public class Hl7Segment
{
    public string Name { get; set; } = string.Empty; // MSH, PID, OBR, OBX, etc.
    public List<string> Fields { get; set; } = new();

    public string GetField(int index) => index >= 0 && index < Fields.Count ? Fields[index] : string.Empty;
    public string GetComponent(int fieldIndex, int componentIndex)
    {
        var field = GetField(fieldIndex);
        var components = field.Split('^');
        return componentIndex >= 0 && componentIndex < components.Length ? components[componentIndex] : string.Empty;
    }
}
