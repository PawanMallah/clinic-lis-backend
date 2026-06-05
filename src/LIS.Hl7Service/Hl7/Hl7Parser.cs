namespace LIS.Hl7Service.Hl7;

public static class Hl7Parser
{
    private const char SegmentSeparator = '\r';
    private const char FieldSeparator = '|';
    private const string StartBlock = "\x0B"; // VT (vertical tab)
    private const string EndBlock = "\x1C\r"; // FS + CR

    /// <summary>
    /// Parse a raw HL7 v2.x message string into structured Hl7Message.
    /// </summary>
    public static Hl7Message Parse(string rawMessage)
    {
        // Strip MLLP framing if present
        var cleaned = rawMessage
            .Replace(StartBlock, "")
            .Replace("\x1C", "")
            .Trim();

        var message = new Hl7Message { RawMessage = rawMessage };
        var segmentLines = cleaned.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in segmentLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var segment = new Hl7Segment();
            var fields = line.Split(FieldSeparator);

            segment.Name = fields[0];

            // For MSH segment, the field separator IS field 1
            if (segment.Name == "MSH")
            {
                segment.Fields = new List<string> { FieldSeparator.ToString() };
                segment.Fields.AddRange(fields.Skip(1));

                // Extract message type and control ID from MSH
                if (segment.Fields.Count > 8)
                {
                    var msgType = segment.GetField(8); // MSH-9
                    var parts = msgType.Split('^');
                    message.MessageType = parts.Length > 0 ? parts[0] : "";
                    message.TriggerEvent = parts.Length > 1 ? parts[1] : "";
                }
                if (segment.Fields.Count > 9)
                {
                    message.ControlId = segment.GetField(9); // MSH-10
                }
            }
            else
            {
                segment.Fields = fields.Skip(1).ToList();
            }

            message.Segments.Add(segment);
        }

        return message;
    }

    /// <summary>
    /// Wrap a raw HL7 message in MLLP (Minimal Lower Layer Protocol) framing.
    /// Used when sending messages over TCP.
    /// </summary>
    public static byte[] WrapMllp(string message)
    {
        return System.Text.Encoding.ASCII.GetBytes($"\x0B{message}\x1C\r");
    }

    /// <summary>
    /// Build an ACK response for a received message.
    /// </summary>
    public static string BuildAck(Hl7Message originalMessage, string ackCode = "AA")
    {
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var controlId = originalMessage.ControlId;
        var ack = $"MSH|^~\\&|LIS|LAB|{originalMessage.GetSegment("MSH")?.GetComponent(2, 0)}|{originalMessage.GetSegment("MSH")?.GetComponent(3, 0)}|{now}||ACK|{Guid.NewGuid():N}|P|2.5\rMSA|{ackCode}|{controlId}\r";
        return ack;
    }
}
