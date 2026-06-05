using Dapper;
using LIS.QcService.Models;
using Shared.Database;

namespace LIS.QcService.Repositories;

public class QcRepository : IQcRepository
{
    private readonly DapperContext _context;

    public QcRepository(DapperContext context)
    {
        _context = context;
    }

    // ─── Materials ───────────────────────────────────────────────────────────────

    public async Task<QcMaterial> CreateMaterialAsync(QcMaterial material)
    {
        const string sql = @"
            INSERT INTO qc_materials (id, lab_id, material_name, manufacturer, lot_number, expiry_date, level, is_active, created_at)
            VALUES (@Id, @LabId, @MaterialName, @Manufacturer, @LotNumber, @ExpiryDate, @Level, @IsActive, NOW())
            RETURNING id, lab_id AS LabId, material_name AS MaterialName, manufacturer, lot_number AS LotNumber,
                expiry_date AS ExpiryDate, level, is_active AS IsActive, created_at AS CreatedAt";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<QcMaterial>(sql, material);
    }

    public async Task<List<QcMaterial>> GetMaterialsAsync(Guid labId)
    {
        const string sql = @"
            SELECT id, lab_id AS LabId, material_name AS MaterialName, manufacturer, lot_number AS LotNumber,
                   expiry_date AS ExpiryDate, level, is_active AS IsActive, created_at AS CreatedAt
            FROM qc_materials
            WHERE lab_id = @LabId AND is_active = true
            ORDER BY created_at DESC";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QcMaterial>(sql, new { LabId = labId });
        return result.ToList();
    }

    public async Task UpdateMaterialAsync(QcMaterial material)
    {
        const string sql = @"
            UPDATE qc_materials
            SET material_name = @MaterialName, manufacturer = @Manufacturer,
                lot_number = @LotNumber, expiry_date = @ExpiryDate, level = @Level
            WHERE id = @Id";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, material);
    }

    public async Task DeleteMaterialAsync(Guid id)
    {
        const string sql = "UPDATE qc_materials SET is_active = false WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    // ─── Target Values ───────────────────────────────────────────────────────────

    public async Task<QcTargetValue> CreateTargetValueAsync(QcTargetValue targetValue)
    {
        const string sql = @"
            INSERT INTO qc_target_values (id, material_id, test_id, test_code, test_name, expected_mean, expected_sd, unit, created_at)
            VALUES (@Id, @MaterialId, @TestId, @TestCode, @TestName, @ExpectedMean, @ExpectedSd, @Unit, NOW())
            RETURNING id, material_id AS MaterialId, test_id AS TestId, test_code AS TestCode,
                test_name AS TestName, expected_mean AS ExpectedMean, expected_sd AS ExpectedSd,
                unit, created_at AS CreatedAt";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<QcTargetValue>(sql, targetValue);
    }

    public async Task<List<QcTargetValue>> GetTargetValuesAsync(Guid materialId)
    {
        const string sql = @"
            SELECT id, material_id AS MaterialId, test_id AS TestId, test_code AS TestCode,
                   test_name AS TestName, expected_mean AS ExpectedMean, expected_sd AS ExpectedSd,
                   unit, created_at AS CreatedAt
            FROM qc_target_values
            WHERE material_id = @MaterialId
            ORDER BY test_name";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QcTargetValue>(sql, new { MaterialId = materialId });
        return result.ToList();
    }

    // ─── QC Records ──────────────────────────────────────────────────────────────

    public async Task<QcRecord> CreateQcRecordAsync(QcRecord record)
    {
        const string sql = @"
            INSERT INTO qc_records (id, lab_id, material_id, target_value_id, instrument_id, instrument_name,
                test_id, test_code, test_name, level, lot_number, measured_value, expected_mean, expected_sd,
                sd_index, status, westgard_violation, run_date, recorded_by, recorded_by_name, recorded_at, comments, created_at)
            VALUES (@Id, @LabId, @MaterialId, @TargetValueId, @InstrumentId, @InstrumentName,
                @TestId, @TestCode, @TestName, @Level, @LotNumber, @MeasuredValue, @ExpectedMean, @ExpectedSd,
                @SdIndex, @Status::qc_status, @WestgardViolation, @RunDate, @RecordedBy, @RecordedByName, NOW(), @Comments, NOW())
            RETURNING id, lab_id AS LabId, material_id AS MaterialId, target_value_id AS TargetValueId,
                instrument_id AS InstrumentId, instrument_name AS InstrumentName,
                test_id AS TestId, test_code AS TestCode, test_name AS TestName, level,
                lot_number AS LotNumber, measured_value AS MeasuredValue,
                expected_mean AS ExpectedMean, expected_sd AS ExpectedSd,
                sd_index AS SdIndex, status::text AS Status, westgard_violation AS WestgardViolation,
                run_date AS RunDate, recorded_by AS RecordedBy, recorded_by_name AS RecordedByName,
                recorded_at AS RecordedAt, comments, created_at AS CreatedAt";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<QcRecord>(sql, record);
    }

    public async Task<(List<QcRecord> Items, int Total)> GetQcRecordsAsync(
        Guid labId, Guid? testId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var whereClause = "WHERE lab_id = @LabId";
        if (testId.HasValue) whereClause += " AND test_id = @TestId";
        if (fromDate.HasValue) whereClause += " AND run_date >= @FromDate";
        if (toDate.HasValue) whereClause += " AND run_date <= @ToDate";

        var countSql = $"SELECT COUNT(*) FROM qc_records {whereClause}";
        var dataSql = $@"
            SELECT id, lab_id AS LabId, material_id AS MaterialId, target_value_id AS TargetValueId,
                   instrument_id AS InstrumentId, instrument_name AS InstrumentName,
                   test_id AS TestId, test_code AS TestCode, test_name AS TestName, level,
                   lot_number AS LotNumber, measured_value AS MeasuredValue,
                   expected_mean AS ExpectedMean, expected_sd AS ExpectedSd,
                   sd_index AS SdIndex, status::text AS Status, westgard_violation AS WestgardViolation,
                   run_date AS RunDate, recorded_by AS RecordedBy, recorded_by_name AS RecordedByName,
                   recorded_at AS RecordedAt, comments, created_at AS CreatedAt
            FROM qc_records
            {whereClause}
            ORDER BY recorded_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var parameters = new
        {
            LabId = labId,
            TestId = testId,
            FromDate = fromDate,
            ToDate = toDate,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        };

        using var connection = _context.CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<QcRecord>(dataSql, parameters);
        return (items.ToList(), total);
    }

    public async Task<List<decimal>> GetRecentValuesAsync(Guid labId, Guid targetValueId, int count)
    {
        const string sql = @"
            SELECT measured_value
            FROM qc_records
            WHERE lab_id = @LabId AND target_value_id = @TargetValueId
            ORDER BY recorded_at DESC
            LIMIT @Count";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<decimal>(sql, new { LabId = labId, TargetValueId = targetValueId, Count = count });
        return result.ToList();
    }

    public async Task<List<QcRecord>> GetLeveyJenningsDataAsync(Guid labId, Guid targetValueId, int days)
    {
        const string sql = @"
            SELECT id, lab_id AS LabId, material_id AS MaterialId, target_value_id AS TargetValueId,
                   instrument_id AS InstrumentId, instrument_name AS InstrumentName,
                   test_id AS TestId, test_code AS TestCode, test_name AS TestName, level,
                   lot_number AS LotNumber, measured_value AS MeasuredValue,
                   expected_mean AS ExpectedMean, expected_sd AS ExpectedSd,
                   sd_index AS SdIndex, status::text AS Status, westgard_violation AS WestgardViolation,
                   run_date AS RunDate, recorded_by AS RecordedBy, recorded_by_name AS RecordedByName,
                   recorded_at AS RecordedAt, comments, created_at AS CreatedAt
            FROM qc_records
            WHERE lab_id = @LabId AND target_value_id = @TargetValueId
              AND run_date >= CURRENT_DATE - @Days
            ORDER BY run_date ASC, recorded_at ASC";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QcRecord>(sql, new { LabId = labId, TargetValueId = targetValueId, Days = days });
        return result.ToList();
    }

    // ─── QC Status ───────────────────────────────────────────────────────────────

    public async Task<List<QcStatusInfo>> GetQcStatusAsync(Guid labId)
    {
        const string sql = @"
            SELECT DISTINCT ON (r.test_code, r.instrument_name)
                r.test_code AS TestCode,
                r.test_name AS TestName,
                r.instrument_name AS InstrumentName,
                r.run_date AS LastQcDate,
                r.status::text AS LastQcStatus,
                CASE WHEN b.id IS NOT NULL THEN true ELSE false END AS IsBlocked,
                b.reason AS BlockReason
            FROM qc_records r
            LEFT JOIN qc_blocks b ON b.lab_id = r.lab_id AND b.test_id = r.test_id
                AND (b.instrument_id = r.instrument_id OR (b.instrument_id IS NULL AND r.instrument_id IS NULL))
                AND b.is_active = true
            WHERE r.lab_id = @LabId
            ORDER BY r.test_code, r.instrument_name, r.recorded_at DESC";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QcStatusInfo>(sql, new { LabId = labId });
        return result.ToList();
    }

    // ─── Blocks ──────────────────────────────────────────────────────────────────

    public async Task<QcBlock> CreateBlockAsync(QcBlock block)
    {
        const string sql = @"
            INSERT INTO qc_blocks (id, lab_id, test_id, instrument_id, reason, blocked_by, blocked_at, is_active)
            VALUES (@Id, @LabId, @TestId, @InstrumentId, @Reason, @BlockedBy, NOW(), true)
            RETURNING id, lab_id AS LabId, test_id AS TestId, instrument_id AS InstrumentId,
                reason, blocked_by AS BlockedBy, blocked_at AS BlockedAt,
                resolved_by AS ResolvedBy, resolved_at AS ResolvedAt, is_active AS IsActive";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<QcBlock>(sql, block);
    }

    public async Task<List<QcBlock>> GetActiveBlocksAsync(Guid labId)
    {
        const string sql = @"
            SELECT id, lab_id AS LabId, test_id AS TestId, instrument_id AS InstrumentId,
                   reason, blocked_by AS BlockedBy, blocked_at AS BlockedAt,
                   resolved_by AS ResolvedBy, resolved_at AS ResolvedAt, is_active AS IsActive
            FROM qc_blocks
            WHERE lab_id = @LabId AND is_active = true
            ORDER BY blocked_at DESC";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QcBlock>(sql, new { LabId = labId });
        return result.ToList();
    }

    public async Task ResolveBlockAsync(Guid blockId, Guid resolvedBy)
    {
        const string sql = @"
            UPDATE qc_blocks
            SET is_active = false, resolved_by = @ResolvedBy, resolved_at = NOW()
            WHERE id = @BlockId";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { BlockId = blockId, ResolvedBy = resolvedBy });
    }
}
