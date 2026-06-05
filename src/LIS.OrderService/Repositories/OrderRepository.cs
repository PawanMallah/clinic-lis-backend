using Dapper;
using LIS.OrderService.Models;
using Shared.Database;

namespace LIS.OrderService.Repositories;

public class OrderRepository : BaseRepository, IOrderRepository
{
    public OrderRepository(DapperContext context) : base(context) { }

    public async Task<LabOrder> CreateAsync(LabOrder order)
    {
        const string sql = @"
            INSERT INTO lab_orders (id, lab_id, patient_id, patient_name, patient_uhid, patient_age, 
                patient_gender, patient_mobile, external_order_id, source_system, priority, status,
                ordered_by, ordered_by_name, clinical_notes, created_at, updated_at)
            VALUES (@Id, @LabId, @PatientId, @PatientName, @PatientUhid, @PatientAge, 
                @PatientGender, @PatientMobile, @ExternalOrderId, @SourceSystem, @Priority::order_priority, @Status::order_status,
                @OrderedBy, @OrderedByName, @ClinicalNotes, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, lab_id AS LabId, patient_id AS PatientId, patient_name AS PatientName,
                patient_uhid AS PatientUhid, patient_age AS PatientAge, patient_gender AS PatientGender,
                patient_mobile AS PatientMobile, external_order_id AS ExternalOrderId,
                source_system AS SourceSystem, priority::text AS Priority, status::text AS Status,
                ordered_by AS OrderedBy, ordered_by_name AS OrderedByName, clinical_notes AS ClinicalNotes,
                created_at AS CreatedAt, updated_at AS UpdatedAt";

        using var connection = Connection;
        var result = await connection.QuerySingleAsync<LabOrder>(sql, order);
        return result;
    }

    public async Task<LabOrder?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, patient_id AS PatientId, patient_name AS PatientName,
                patient_uhid AS PatientUhid, patient_age AS PatientAge, patient_gender AS PatientGender,
                patient_mobile AS PatientMobile, external_order_id AS ExternalOrderId,
                source_system AS SourceSystem, priority::text AS Priority, status::text AS Status,
                ordered_by AS OrderedBy, ordered_by_name AS OrderedByName, clinical_notes AS ClinicalNotes,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM lab_orders WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<LabOrder>(sql, new { Id = id });
    }

    public async Task<List<LabOrder>> GetAllAsync(Guid labId, string? status, string? priority, string? search, int page, int pageSize)
    {
        var sql = @"SELECT id AS Id, lab_id AS LabId, patient_id AS PatientId, patient_name AS PatientName, 
                           patient_uhid AS PatientUhid, patient_age AS PatientAge, patient_gender AS PatientGender,
                           patient_mobile AS PatientMobile, external_order_id AS ExternalOrderId,
                           source_system AS SourceSystem, priority::text AS Priority, status::text AS Status,
                           ordered_by AS OrderedBy, ordered_by_name AS OrderedByName, clinical_notes AS ClinicalNotes,
                           created_at AS CreatedAt, updated_at AS UpdatedAt
                    FROM lab_orders WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status::order_status";
            parameters.Add("Status", status);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            sql += " AND priority = @Priority::order_priority";
            parameters.Add("Priority", priority);
        }

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (patient_name ILIKE @Search OR patient_uhid ILIKE @Search OR external_order_id ILIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var results = await connection.QueryAsync<LabOrder>(sql, parameters);
        return results.ToList();
    }

    public async Task<int> GetCountAsync(Guid labId, string? status, string? priority, string? search)
    {
        var sql = @"SELECT COUNT(*) FROM lab_orders WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status::order_status";
            parameters.Add("Status", status);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            sql += " AND priority = @Priority::order_priority";
            parameters.Add("Priority", priority);
        }

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (patient_name ILIKE @Search OR patient_uhid ILIKE @Search OR external_order_id ILIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        const string sql = @"
            UPDATE lab_orders 
            SET status = @Status::order_status, updated_at = NOW() 
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    public async Task SaveOrderTestsAsync(Guid orderId, List<LabOrderTest> tests)
    {
        const string sql = @"
            INSERT INTO lab_order_tests (id, order_id, test_id, test_code, test_name, status, specimen_id,
                result_value, result_unit, result_flag, reference_low, reference_high, created_at, updated_at)
            VALUES (@Id, @OrderId, @TestId, @TestCode, @TestName, @Status::order_status, @SpecimenId,
                @ResultValue, @ResultUnit, @ResultFlag::result_flag, @ReferenceLow, @ReferenceHigh, @CreatedAt, @UpdatedAt)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, tests);
    }

    public async Task<List<LabOrderTest>> GetOrderTestsAsync(Guid orderId)
    {
        const string sql = @"
            SELECT id AS Id, order_id AS OrderId, test_id AS TestId, test_code AS TestCode,
                test_name AS TestName, status::text AS Status, specimen_id AS SpecimenId,
                result_value AS ResultValue, result_unit AS ResultUnit, result_flag::text AS ResultFlag,
                reference_low AS ReferenceLow, reference_high AS ReferenceHigh,
                verified_by AS VerifiedBy, verified_at AS VerifiedAt, reported_at AS ReportedAt,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM lab_order_tests WHERE order_id = @OrderId ORDER BY created_at";

        using var connection = Connection;
        var results = await connection.QueryAsync<LabOrderTest>(sql, new { OrderId = orderId });
        return results.ToList();
    }

    public async Task<int> GetTodayCountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM lab_orders 
            WHERE lab_id = @LabId AND created_at::date = CURRENT_DATE";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { LabId = labId });
    }

    public async Task<int> GetPendingCountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM lab_orders 
            WHERE lab_id = @LabId AND status IN ('ordered', 'collecting', 'received')";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { LabId = labId });
    }

    public async Task<int> GetCompletedCountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM lab_orders 
            WHERE lab_id = @LabId AND status = 'reported' AND created_at::date = CURRENT_DATE";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { LabId = labId });
    }

    public async Task<int> GetStatCountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM lab_orders 
            WHERE lab_id = @LabId AND priority = 'stat' AND status NOT IN ('reported', 'cancelled')";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { LabId = labId });
    }
}
