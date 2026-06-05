using LIS.QcService.Models;

namespace LIS.QcService.Repositories;

public interface IQcRepository
{
    // Materials
    Task<QcMaterial> CreateMaterialAsync(QcMaterial material);
    Task<List<QcMaterial>> GetMaterialsAsync(Guid labId);
    Task UpdateMaterialAsync(QcMaterial material);
    Task DeleteMaterialAsync(Guid id);

    // Target Values
    Task<QcTargetValue> CreateTargetValueAsync(QcTargetValue targetValue);
    Task<List<QcTargetValue>> GetTargetValuesAsync(Guid materialId);

    // QC Records
    Task<QcRecord> CreateQcRecordAsync(QcRecord record);
    Task<(List<QcRecord> Items, int Total)> GetQcRecordsAsync(
        Guid labId, Guid? testId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<List<decimal>> GetRecentValuesAsync(Guid labId, Guid targetValueId, int count);
    Task<List<QcRecord>> GetLeveyJenningsDataAsync(Guid labId, Guid targetValueId, int days);

    // QC Status
    Task<List<QcStatusInfo>> GetQcStatusAsync(Guid labId);

    // Blocks
    Task<QcBlock> CreateBlockAsync(QcBlock block);
    Task<List<QcBlock>> GetActiveBlocksAsync(Guid labId);
    Task ResolveBlockAsync(Guid blockId, Guid resolvedBy);
}

/// <summary>
/// Composite model for QC status per test/instrument.
/// </summary>
public class QcStatusInfo
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? InstrumentName { get; set; }
    public DateTime? LastQcDate { get; set; }
    public string? LastQcStatus { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}
