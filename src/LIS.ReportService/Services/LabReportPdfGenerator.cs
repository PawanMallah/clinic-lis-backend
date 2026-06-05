using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LIS.ReportService.Services;

public class LabReportPdfGenerator
{
    public byte[] GenerateReport(LabReportData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(c => ComposeFooter(c, data));
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private void ComposeHeader(IContainer container, LabReportData data)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1).PaddingBottom(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(data.FacilityName).FontSize(14).Bold();
                    c.Item().Text(data.FacilityAddress).FontSize(8);
                    c.Item().Text($"Accreditation: {data.AccreditationNumber}").FontSize(8);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, LabReportData data)
    {
        container.Column(col =>
        {
            // Patient Information Block
            col.Item().PaddingVertical(5).Border(1).Padding(8).Column(patCol =>
            {
                patCol.Item().Row(row =>
                {
                    row.RelativeItem().Text($"PATIENT: {data.PatientName}").Bold();
                    row.RelativeItem().Text($"UHID/MRN: {data.PatientUhid}");
                    row.RelativeItem().Text($"DOB: {data.PatientDob}");
                });
                patCol.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Sex: {data.PatientGender}");
                    row.RelativeItem().Text($"Age: {data.PatientAge}");
                    row.RelativeItem().Text($"Mobile: {data.PatientMobile}");
                });
                patCol.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Account/Visit: {data.AccountNumber}");
                    row.RelativeItem().Text($"Performing Lab: {data.PerformingLab}");
                });
                if (!string.IsNullOrEmpty(data.ReferredByDoctor))
                    patCol.Item().Text($"Referred By: {data.ReferredByDoctor}");
                if (!string.IsNullOrEmpty(data.OrderingProvider))
                    patCol.Item().Text($"Ordering Provider: {data.OrderingProvider}");
            });

            // Accession / Dates Block
            col.Item().PaddingVertical(5).Border(1).Padding(8).Column(accCol =>
            {
                accCol.Item().Text($"ACCESSION: {data.AccessionNumber}").Bold();
                accCol.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Collected: {data.CollectedAt}");
                    row.RelativeItem().Text($"Received: {data.ReceivedAt}");
                    row.RelativeItem().Text($"Reported: {data.ReportedAt}");
                });
                accCol.Item().Text($"Status: {data.ReportStatus}").Bold();
            });

            // Results Table — one per test group
            foreach (var testGroup in data.TestGroups)
            {
                col.Item().PaddingVertical(5).Column(testCol =>
                {
                    testCol.Item().Text($"TEST: {testGroup.TestName}").Bold().FontSize(11);
                    testCol.Item().Text($"Specimen: {testGroup.SpecimenType}").FontSize(9);
                    testCol.Item().PaddingTop(3);

                    testCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Parameter
                            columns.RelativeColumn(2); // Result
                            columns.RelativeColumn(1); // Flag
                            columns.RelativeColumn(2); // Units
                            columns.RelativeColumn(2); // Ref Range
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Parameter").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Result").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Flag").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Units").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Ref Range").Bold().FontSize(9);
                        });

                        foreach (var result in testGroup.Results)
                        {
                            var flagColor = result.Flag switch
                            {
                                "high" or "critical_high" => Colors.Red.Medium,
                                "low" or "critical_low" => Colors.Blue.Medium,
                                _ => Colors.Black
                            };
                            var flagText = result.Flag switch
                            {
                                "high" => "H",
                                "low" => "L",
                                "critical_high" => "HH",
                                "critical_low" => "LL",
                                _ => ""
                            };

                            table.Cell().BorderBottom(0.5f).Padding(3).Text(result.ParameterName).FontSize(9);
                            table.Cell().BorderBottom(0.5f).Padding(3).Text(result.ResultValue).FontSize(9).FontColor(flagColor).Bold();
                            table.Cell().BorderBottom(0.5f).Padding(3).Text(flagText).FontSize(9).FontColor(flagColor).Bold();
                            table.Cell().BorderBottom(0.5f).Padding(3).Text(result.Unit).FontSize(9);
                            table.Cell().BorderBottom(0.5f).Padding(3).Text(result.ReferenceRange).FontSize(9);
                        }
                    });
                });
            }

            // Comments / Interpretations
            if (!string.IsNullOrEmpty(data.Comments))
            {
                col.Item().PaddingVertical(5).Border(1).Padding(8).Column(cmtCol =>
                {
                    cmtCol.Item().Text("COMMENT:").Bold().FontSize(9);
                    cmtCol.Item().Text(data.Comments).FontSize(9);
                });
            }

            // Signature Block
            col.Item().PaddingTop(15).Column(sigCol =>
            {
                if (!string.IsNullOrEmpty(data.PerformedByName))
                    sigCol.Item().Text($"Performed By: {data.PerformedByName}, {data.PerformedByQualification}").FontSize(9);

                sigCol.Item().PaddingTop(10);
                sigCol.Item().Text("Verified By:").Bold().FontSize(9);

                // Digital signature placeholder
                if (data.DigitalSignatureBytes != null && data.DigitalSignatureBytes.Length > 0)
                {
                    sigCol.Item().Width(120).Height(40).Image(data.DigitalSignatureBytes);
                }

                sigCol.Item().Text($"    {data.VerifiedByName}").FontSize(10).Bold();
                if (!string.IsNullOrEmpty(data.VerifiedByQualification))
                    sigCol.Item().Text($"    {data.VerifiedByQualification}").FontSize(9);
                if (!string.IsNullOrEmpty(data.VerifiedByLicense))
                    sigCol.Item().Text($"    License: {data.VerifiedByLicense}").FontSize(9);
                sigCol.Item().Text($"    {data.VerifiedAt}").FontSize(9);
            });
        });
    }

    private void ComposeFooter(IContainer container, LabReportData data)
    {
        container.AlignCenter().Text("*** END OF REPORT ***").FontSize(8).Italic();
    }
}

// ─── Data Models ────────────────────────────────────────────────────────────────

public class LabReportData
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

    // Order / Accession
    public string AccessionNumber { get; set; } = string.Empty;
    public string? CollectedAt { get; set; }
    public string? ReceivedAt { get; set; }
    public string? ReportedAt { get; set; }
    public string ReportStatus { get; set; } = "Final";

    // Test Results
    public List<TestGroupData> TestGroups { get; set; } = new();

    // Comments / Interpretations
    public string? Comments { get; set; }

    // Signatures
    public string? PerformedByName { get; set; }
    public string? PerformedByQualification { get; set; }
    public string? VerifiedByName { get; set; }
    public string? VerifiedByQualification { get; set; }
    public string? VerifiedByLicense { get; set; }
    public string? VerifiedAt { get; set; }
    public byte[]? DigitalSignatureBytes { get; set; }
}

public class TestGroupData
{
    public string TestName { get; set; } = string.Empty;
    public string? SpecimenType { get; set; }
    public List<ResultRowData> Results { get; set; } = new();
}

public class ResultRowData
{
    public string ParameterName { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public string? Flag { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
}
