namespace LIS.Hl7Service.Hl7;

/// <summary>
/// Extracts test results from ORU^R01 messages.
/// ORU structure: MSH → PID → OBR (order) → OBX (results, repeating)
/// </summary>
public class Hl7ResultData
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string SpecimenBarcode { get; set; } = string.Empty;
    public List<Hl7TestResult> Results { get; set; } = new();
}

public class Hl7TestResult
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ReferenceRange { get; set; } = string.Empty;
    public string AbnormalFlag { get; set; } = string.Empty;
    public string ObservationDateTime { get; set; } = string.Empty;
}

public static class Hl7ResultExtractor
{
    /// <summary>
    /// Extract patient info and test results from an ORU^R01 message.
    /// </summary>
    public static Hl7ResultData Extract(Hl7Message message)
    {
        var data = new Hl7ResultData();

        // PID segment — patient demographics
        var pid = message.GetSegment("PID");
        if (pid != null)
        {
            data.PatientId = pid.GetField(2); // PID-3: Patient ID
            data.PatientName = pid.GetField(4); // PID-5: Patient Name
        }

        // OBR segment — order info
        var obr = message.GetSegment("OBR");
        if (obr != null)
        {
            data.OrderId = obr.GetField(1); // OBR-2: Placer Order Number
            data.SpecimenBarcode = obr.GetField(2); // OBR-3: Filler Order Number
        }

        // OBX segments — results (multiple)
        var obxSegments = message.GetSegments("OBX");
        foreach (var obx in obxSegments)
        {
            var result = new Hl7TestResult
            {
                TestCode = obx.GetComponent(2, 0), // OBX-3.1: Observation Identifier
                TestName = obx.GetComponent(2, 1), // OBX-3.2: Observation Text
                Value = obx.GetField(4),           // OBX-5: Observation Value
                Unit = obx.GetComponent(5, 0),     // OBX-6: Units
                ReferenceRange = obx.GetField(6),  // OBX-7: Reference Range
                AbnormalFlag = obx.GetField(7),    // OBX-8: Abnormal Flags
                ObservationDateTime = obx.GetField(13) // OBX-14: Date/Time of Observation
            };
            data.Results.Add(result);
        }

        return data;
    }
}
