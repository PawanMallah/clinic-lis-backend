using Dapper;
using LIS.ResultService.Helpers;
using LIS.ResultService.Models;
using Shared.Database;

namespace LIS.ResultService.Repositories;

public class ResultRepository : BaseRepository, IResultRepository
{
    public ResultRepository(DapperContext context) : base(context) { }

    public async Task<TestResult> CreateResultAsync(TestResult result)
    {
        const string sql = @"
            INSERT INTO test_results (id, lab_id, order_id, order_test_id, test_id, test_code, test_name,
                parameter_name, result_value, result_numeric, result_unit, reference_low, reference_high,
                critical_low, critical_high, flag, is_critical, instrument_id, instrument_name, raw_value,
                method, remarks, entered_by, entered_by_name, entered_at, status, created_at, updated_at)
            VALUES (@Id, @LabId, @OrderId, @OrderTestId, @TestId, @TestCode, @TestName,
                @ParameterName, @ResultValue, @ResultNumeric, @ResultUnit, @ReferenceLow, @ReferenceHigh,
                @CriticalLow, @CriticalHigh, @Flag::result_flag, @IsCritical, @InstrumentId, @InstrumentName, @RawValue,
                @Method, @Remarks, @EnteredBy, @EnteredByName, @EnteredAt, @Status, @CreatedAt, @UpdatedAt)
            RETURNING *";

        using var connection = Connection;
        return await connection.QuerySingleAsync<TestResult>(sql, result);
    }

    public async Task<TestResult?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM test_results WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<TestResult>(sql, new { Id = id });
    }

    public async Task<TestResult?> GetByOrderTestIdAsync(Guid orderTestId)
    {
        const string sql = "SELECT * FROM test_results WHERE order_test_id = @OrderTestId ORDER BY created_at DESC LIMIT 1";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<TestResult>(sql, new { OrderTestId = orderTestId });
    }

    public async Task<List<TestResult>> GetByOrderIdAsync(Guid orderId)
    {
        const string sql = "SELECT * FROM test_results WHERE order_id = @OrderId ORDER BY test_name, parameter_name";

        using var connection = Connection;
        var results = await connection.QueryAsync<TestResult>(sql, new { OrderId = orderId });
        return results.ToList();
    }

    public async Task<(List<WorklistRow> Items, int Total)> GetWorklistAsync(Guid labId, string? department, string? status, int page, int pageSize)
    {
        var baseSql = @"
            FROM lab_order_tests ot
            INNER JOIN lab_orders o ON o.id = ot.order_id
            WHERE o.lab_id = @LabId";

        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            baseSql += " AND ot.status = @Status::order_status";
            parameters.Add("Status", status);
        }
        else
        {
            baseSql += " AND ot.status IN ('received', 'in_progress')";
        }

        var countSql = $"SELECT COUNT(*) {baseSql}";

        var querySql = $@"
            SELECT ot.order_id AS OrderId, o.patient_name AS PatientName, o.patient_uhid AS PatientUhid,
                   ot.test_code AS TestCode, ot.test_name AS TestName, o.priority AS Priority,
                   ot.status AS Status, o.created_at AS OrderedAt
            {baseSql}
            ORDER BY 
                CASE o.priority 
                    WHEN 'stat' THEN 1 
                    WHEN 'asap' THEN 2 
                    WHEN 'urgent' THEN 3 
                    ELSE 4 
                END,
                o.created_at ASC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<WorklistRow>(querySql, parameters);

        return (items.ToList(), total);
    }

    public async Task UpdateResultAsync(Guid id, string value, string? unit, string? remarks)
    {
        const string sql = @"
            UPDATE test_results 
            SET result_value = @Value, result_unit = @Unit, remarks = @Remarks, updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, Value = value, Unit = unit, Remarks = remarks });
    }

    public async Task<List<ResultVerification>> GetVerificationsAsync(Guid resultId)
    {
        const string sql = @"
            SELECT * FROM result_verifications 
            WHERE result_id = @ResultId 
            ORDER BY verified_at DESC";

        using var connection = Connection;
        var results = await connection.QueryAsync<ResultVerification>(sql, new { ResultId = resultId });
        return results.ToList();
    }

    public async Task AddVerificationAsync(ResultVerification verification)
    {
        const string sql = @"
            INSERT INTO result_verifications (id, result_id, verification_level, verified_by, 
                verified_by_name, verified_at, status, comments, previous_value, corrected_value)
            VALUES (@Id, @ResultId, @VerificationLevel::verification_level, @VerifiedBy, 
                @VerifiedByName, @VerifiedAt, @Status, @Comments, @PreviousValue, @CorrectedValue)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, verification);
    }

    public async Task UpdateResultStatusAsync(Guid id, string status)
    {
        const string sql = @"
            UPDATE test_results 
            SET status = @Status, updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    public async Task<List<CriticalAlert>> GetCriticalAlertsAsync(Guid labId, bool unacknowledgedOnly)
    {
        var sql = "SELECT * FROM critical_alerts WHERE lab_id = @LabId";

        if (unacknowledgedOnly)
            sql += " AND acknowledged_at IS NULL";

        sql += " ORDER BY created_at DESC";

        using var connection = Connection;
        var results = await connection.QueryAsync<CriticalAlert>(sql, new { LabId = labId });
        return results.ToList();
    }

    public async Task AcknowledgeCriticalAlertAsync(Guid alertId, Guid userId)
    {
        const string sql = @"
            UPDATE critical_alerts 
            SET acknowledged_by = @UserId, acknowledged_at = NOW()
            WHERE id = @AlertId";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { AlertId = alertId, UserId = userId });
    }

    public async Task CreateCriticalAlertAsync(CriticalAlert alert)
    {
        const string sql = @"
            INSERT INTO critical_alerts (id, lab_id, result_id, order_id, patient_name, test_name,
                result_value, critical_type, created_at)
            VALUES (@Id, @LabId, @ResultId, @OrderId, @PatientName, @TestName,
                @ResultValue, @CriticalType, @CreatedAt)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, alert);
    }

    public async Task<List<TestResult>> GetPatientPreviousResultsAsync(Guid labId, Guid? patientId, string testCode, int limit)
    {
        if (!patientId.HasValue) return new List<TestResult>();

        const string sql = @"
            SELECT tr.* FROM test_results tr
            INNER JOIN lab_orders o ON o.id = tr.order_id
            WHERE tr.lab_id = @LabId 
              AND o.patient_id = @PatientId 
              AND tr.test_code = @TestCode
            ORDER BY tr.created_at DESC
            LIMIT @Limit";

        using var connection = Connection;
        var results = await connection.QueryAsync<TestResult>(sql, new
        {
            LabId = labId,
            PatientId = patientId.Value,
            TestCode = testCode,
            Limit = limit
        });
        return results.ToList();
    }
}
