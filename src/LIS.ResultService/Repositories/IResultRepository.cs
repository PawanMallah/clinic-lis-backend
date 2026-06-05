using LIS.ResultService.Models;

namespace LIS.ResultService.Repositories;

public interface IResultRepository
{
    Task<TestResult> CreateResultAsync(TestResult result);
    Task<TestResult?> GetByIdAsync(Guid id);
    Task<List<TestResult>> GetByOrderIdAsync(Guid orderId);
    Task<(List<WorklistRow> Items, int Total)> GetWorklistAsync(Guid labId, string? department, string? status, int page, int pageSize);
    Task UpdateResultAsync(Guid id, string value, string? unit, string? remarks);
    Task<List<ResultVerification>> GetVerificationsAsync(Guid resultId);
    Task AddVerificationAsync(ResultVerification verification);
    Task UpdateResultStatusAsync(Guid id, string status);
    Task<List<CriticalAlert>> GetCriticalAlertsAsync(Guid labId, bool unacknowledgedOnly);
    Task AcknowledgeCriticalAlertAsync(Guid alertId, Guid userId);
    Task CreateCriticalAlertAsync(CriticalAlert alert);
    Task<List<TestResult>> GetPatientPreviousResultsAsync(Guid labId, Guid? patientId, string testCode, int limit);
    Task<TestResult?> GetByOrderTestIdAsync(Guid orderTestId);

    // Auto-verification
    Task<List<AutoVerificationRule>> GetAutoVerificationRulesAsync(Guid labId, string? testCode = null);
    Task<AutoVerificationRule> CreateAutoVerificationRuleAsync(AutoVerificationRule rule);
    Task UpdateAutoVerificationRuleAsync(AutoVerificationRule rule);
    Task CreateAutoVerificationLogAsync(AutoVerificationLog log);
    Task<List<ReflexRuleRow>> GetReflexRulesAsync(Guid labId, string testCode);

    // Digital signatures
    Task<DigitalSignature> CreateDigitalSignatureAsync(DigitalSignature signature);
    Task<List<DigitalSignature>> GetDigitalSignaturesAsync(Guid labId);
}

public class WorklistRow
{
    public Guid OrderId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Priority { get; set; } = "routine";
    public string Status { get; set; } = "pending";
    public DateTime? OrderedAt { get; set; }
}

public class ReflexRuleRow
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
