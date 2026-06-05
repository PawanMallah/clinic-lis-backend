using LIS.ReportService.Services;

namespace LIS.ReportService.DTOs;

public class GenerateReportRequest
{
    public Guid OrderId { get; set; }
    public string? PatientName { get; set; }
}

public class GeneratePdfRequest
{
    // Facility
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public string AccreditationNumber { get; set; } = string.Empty;

    // Patient
    public string PatientName { get; set; } = string.Empty;
    public string? PatientUhid { get; set; }
    public string? PatientDob { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientAge { get; set; }
    public string? PatientMobile { get; set; }
    public string? AccountNumber { get; set; }
    public string? ReferredByDoctor { get; set; }
    public string? OrderingProvider { get; set; }
    public string? PerformingLab { get; set; }

    // Accession / Dates
    public string AccessionNumber { get; set; } = string.Empty;
    public string? CollectedAt { get; set; }
    public string? ReceivedAt { get; set; }
    public string? ReportedAt { get; set; }
    public string ReportStatus { get; set; } = "Final";

    // Test Results
    public List<PdfTestGroup> TestGroups { get; set; } = new();

    // Comments
    public string? Comments { get; set; }

    // Signatures
    public string? PerformedByName { get; set; }
    public string? PerformedByQualification { get; set; }
    public string? VerifiedByName { get; set; }
    public string? VerifiedByQualification { get; set; }
    public string? VerifiedByLicense { get; set; }
    public string? VerifiedAt { get; set; }
    public string? DigitalSignatureBase64 { get; set; }

    public LabReportData ToLabReportData()
    {
        return new LabReportData
        {
            FacilityName = FacilityName,
            FacilityAddress = FacilityAddress,
            AccreditationNumber = AccreditationNumber,
            PatientName = PatientName,
            PatientUhid = PatientUhid,
            PatientDob = PatientDob,
            PatientGender = PatientGender,
            PatientAge = PatientAge,
            PatientMobile = PatientMobile,
            AccountNumber = AccountNumber,
            ReferredByDoctor = ReferredByDoctor,
            OrderingProvider = OrderingProvider,
            PerformingLab = PerformingLab,
            AccessionNumber = AccessionNumber,
            CollectedAt = CollectedAt,
            ReceivedAt = ReceivedAt,
            ReportedAt = ReportedAt,
            ReportStatus = ReportStatus,
            TestGroups = TestGroups.Select(tg => new TestGroupData
            {
                TestName = tg.TestName,
                SpecimenType = tg.SpecimenType,
                Results = tg.Results.Select(r => new ResultRowData
                {
                    ParameterName = r.ParameterName,
                    ResultValue = r.ResultValue,
                    Flag = r.Flag,
                    Unit = r.Unit,
                    ReferenceRange = r.ReferenceRange
                }).ToList()
            }).ToList(),
            Comments = Comments,
            PerformedByName = PerformedByName,
            PerformedByQualification = PerformedByQualification,
            VerifiedByName = VerifiedByName,
            VerifiedByQualification = VerifiedByQualification,
            VerifiedByLicense = VerifiedByLicense,
            VerifiedAt = VerifiedAt,
            DigitalSignatureBytes = !string.IsNullOrEmpty(DigitalSignatureBase64)
                ? Convert.FromBase64String(DigitalSignatureBase64)
                : null
        };
    }
}

public class PdfTestGroup
{
    public string TestName { get; set; } = string.Empty;
    public string? SpecimenType { get; set; }
    public List<PdfResultRow> Results { get; set; } = new();
}

public class PdfResultRow
{
    public string ParameterName { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public string? Flag { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
}

public class ReportResponse
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string? ReportNumber { get; set; }
    public string? PatientName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? ReportPdfUrl { get; set; }
    public string? GeneratedAt { get; set; }
    public string? SignedByName { get; set; }
    public string? SignedAt { get; set; }
    public string? DeliveredAt { get; set; }
    public string? DeliveredVia { get; set; }
    public string? AmendmentReason { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class SignReportRequest
{
    public Guid SignedBy { get; set; }
    public string SignedByName { get; set; } = string.Empty;
}

public class DeliverReportRequest
{
    public string DeliveryMethod { get; set; } = "email";
    public string? RecipientAddress { get; set; }
}

public class AmendReportRequest
{
    public string AmendmentReason { get; set; } = string.Empty;
}

public class ReportListResponse
{
    public List<ReportResponse> Reports { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
