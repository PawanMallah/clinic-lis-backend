namespace LIS.ResultService.Services;

/// <summary>
/// Auto-verification engine. Evaluates whether a test result can be automatically
/// released without manual review based on configurable rules.
/// 
/// Rules from document Section 9.1:
/// 1. QC status: Current QC passed for the instrument/test
/// 2. Delta check: Value within acceptable change from prior result
/// 3. Panic values: Not a critical/panic value
/// 4. Linearity: Within assay reportable range
/// 5. Flags: No instrument error flags
/// 6. Special comments: No manual comment required
/// 7. Patient type: Not neonatal or critical care (if excluded)
/// </summary>
public class AutoVerificationService
{
    private readonly ILogger<AutoVerificationService> _logger;

    public AutoVerificationService(ILogger<AutoVerificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluate whether a result passes auto-verification criteria.
    /// Returns (passed, list of failure reasons)
    /// </summary>
    public (bool Passed, List<string> FailureReasons) Evaluate(AutoVerificationContext context)
    {
        var failures = new List<string>();

        // Rule 1: QC must have passed
        if (context.RequireQcPass && !context.QcPassed)
            failures.Add("QC not passed for current run");

        // Rule 2: Not a critical/panic value
        if (context.ExcludeCritical && context.IsCriticalValue)
            failures.Add("Critical/panic value detected");

        // Rule 3: Delta check
        if (context.DeltaCheckPercent > 0 && context.PreviousValue.HasValue && context.CurrentNumericValue.HasValue)
        {
            if (context.PreviousValue.Value != 0)
            {
                var percentChange = Math.Abs((context.CurrentNumericValue.Value - context.PreviousValue.Value) / context.PreviousValue.Value * 100);
                if (percentChange > context.DeltaCheckPercent)
                    failures.Add($"Delta check failed: {percentChange:F1}% change exceeds {context.DeltaCheckPercent}% threshold");
            }
        }

        // Rule 4: Within reportable range (linearity)
        if (context.RequireInReportableRange)
        {
            if (context.ReportableRangeLow.HasValue && context.CurrentNumericValue < context.ReportableRangeLow)
                failures.Add("Below reportable range");
            if (context.ReportableRangeHigh.HasValue && context.CurrentNumericValue > context.ReportableRangeHigh)
                failures.Add("Above reportable range");
        }

        // Rule 5: No instrument error flags
        if (context.RequireNoInstrumentFlags && context.HasInstrumentFlags)
            failures.Add("Instrument error flags present");

        // Rule 6: Not first result for this analyte on patient
        if (context.ExcludeFirstResult && context.IsFirstResult)
            failures.Add("First result for this analyte - requires manual review");

        // Rule 7: Patient type exclusions
        if (context.ExcludeNeonatal && context.IsNeonatal)
            failures.Add("Neonatal patient - requires manual review");
        if (context.ExcludeCriticalCare && context.IsCriticalCarePatient)
            failures.Add("Critical care patient - requires manual review");

        var passed = failures.Count == 0;

        if (passed)
            _logger.LogInformation("Auto-verification PASSED for result on test {TestCode}", context.TestCode);
        else
            _logger.LogInformation("Auto-verification FAILED for test {TestCode}: {Reasons}", context.TestCode, string.Join("; ", failures));

        return (passed, failures);
    }
}

public class AutoVerificationContext
{
    public string TestCode { get; set; } = string.Empty;
    public decimal? CurrentNumericValue { get; set; }
    public decimal? PreviousValue { get; set; }
    public bool QcPassed { get; set; } = true;
    public bool IsCriticalValue { get; set; }
    public bool HasInstrumentFlags { get; set; }
    public bool IsFirstResult { get; set; }
    public bool IsNeonatal { get; set; }
    public bool IsCriticalCarePatient { get; set; }
    public decimal? ReportableRangeLow { get; set; }
    public decimal? ReportableRangeHigh { get; set; }

    // Rule configuration
    public bool RequireQcPass { get; set; } = true;
    public decimal DeltaCheckPercent { get; set; } = 20;
    public bool ExcludeCritical { get; set; } = true;
    public bool RequireInReportableRange { get; set; } = true;
    public bool RequireNoInstrumentFlags { get; set; } = true;
    public bool ExcludeFirstResult { get; set; } = true;
    public bool ExcludeNeonatal { get; set; }
    public bool ExcludeCriticalCare { get; set; }
}
