namespace LIS.ResultService.Services;

/// <summary>
/// Evaluates reflex test rules when a result is entered.
/// If conditions are met, triggers additional test orders.
/// </summary>
public class ReflexTestingService
{
    private readonly ILogger<ReflexTestingService> _logger;

    public ReflexTestingService(ILogger<ReflexTestingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if any reflex rules are triggered by the given result.
    /// Returns list of tests that should be ordered.
    /// </summary>
    public List<ReflexTestTrigger> EvaluateReflexRules(List<ReflexRule> rules, string testCode, decimal? resultValue)
    {
        var triggered = new List<ReflexTestTrigger>();

        if (!resultValue.HasValue) return triggered;

        foreach (var rule in rules.Where(r => r.TriggerTestCode == testCode && r.IsActive))
        {
            var conditionMet = rule.ConditionOperator switch
            {
                ">" => resultValue > rule.ConditionValue,
                "<" => resultValue < rule.ConditionValue,
                ">=" => resultValue >= rule.ConditionValue,
                "<=" => resultValue <= rule.ConditionValue,
                "=" => resultValue == rule.ConditionValue,
                "!=" => resultValue != rule.ConditionValue,
                _ => false
            };

            if (conditionMet)
            {
                _logger.LogInformation("Reflex rule triggered: {TestCode} {Op} {Value} → order {ReflexTest}",
                    testCode, rule.ConditionOperator, rule.ConditionValue, rule.ReflexTestName);

                triggered.Add(new ReflexTestTrigger
                {
                    RuleId = rule.Id,
                    ReflexTestId = rule.ReflexTestId,
                    ReflexTestCode = rule.ReflexTestCode,
                    ReflexTestName = rule.ReflexTestName,
                    AutoOrder = rule.AutoOrder,
                    TriggerReason = $"{testCode} {rule.ConditionOperator} {rule.ConditionValue} (actual: {resultValue})"
                });
            }
        }

        return triggered;
    }
}

public class ReflexRule
{
    public Guid Id { get; set; }
    public string TriggerTestCode { get; set; } = string.Empty;
    public string? TriggerParameter { get; set; }
    public string ConditionOperator { get; set; } = string.Empty;
    public decimal ConditionValue { get; set; }
    public Guid ReflexTestId { get; set; }
    public string ReflexTestCode { get; set; } = string.Empty;
    public string ReflexTestName { get; set; } = string.Empty;
    public bool AutoOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReflexTestTrigger
{
    public Guid RuleId { get; set; }
    public Guid ReflexTestId { get; set; }
    public string ReflexTestCode { get; set; } = string.Empty;
    public string ReflexTestName { get; set; } = string.Empty;
    public bool AutoOrder { get; set; }
    public string TriggerReason { get; set; } = string.Empty;
}
