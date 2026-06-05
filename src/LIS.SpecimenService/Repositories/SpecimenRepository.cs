using Dapper;
using LIS.SpecimenService.Models;
using Shared.Database;

namespace LIS.SpecimenService.Repositories;

public class SpecimenRepository : BaseRepository, ISpecimenRepository
{
    public SpecimenRepository(DapperContext context) : base(context) { }

    public async Task<Specimen> CreateAsync(Specimen specimen)
    {
        const string sql = @"
            INSERT INTO specimens (id, lab_id, order_id, barcode, specimen_type, tube_type, tube_color, 
                volume_ml, status, collected_by, collected_by_name, collected_at, notes, created_at, updated_at)
            VALUES (@Id, @LabId, @OrderId, @Barcode, @SpecimenType, @TubeType, @TubeColor, 
                @VolumeMl, @Status::specimen_status, @CollectedBy, @CollectedByName, @CollectedAt, @Notes, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, lab_id AS LabId, order_id AS OrderId, barcode AS Barcode,
                specimen_type AS SpecimenType, tube_type AS TubeType, tube_color AS TubeColor,
                volume_ml AS VolumeMl, status::text AS Status, collected_by AS CollectedBy,
                collected_by_name AS CollectedByName, collected_at AS CollectedAt,
                received_by AS ReceivedBy, received_by_name AS ReceivedByName, received_at AS ReceivedAt,
                reject_reason AS RejectReason, rejected_by AS RejectedBy, rejected_at AS RejectedAt,
                notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt";

        using var connection = Connection;
        var result = await connection.QuerySingleAsync<Specimen>(sql, specimen);
        return result;
    }

    public async Task<Specimen?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, barcode AS Barcode,
                specimen_type AS SpecimenType, tube_type AS TubeType, tube_color AS TubeColor,
                volume_ml AS VolumeMl, status::text AS Status, collected_by AS CollectedBy,
                collected_by_name AS CollectedByName, collected_at AS CollectedAt,
                received_by AS ReceivedBy, received_by_name AS ReceivedByName, received_at AS ReceivedAt,
                reject_reason AS RejectReason, rejected_by AS RejectedBy, rejected_at AS RejectedAt,
                notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM specimens WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<Specimen>(sql, new { Id = id });
    }

    public async Task<Specimen?> GetByBarcodeAsync(string barcode)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, barcode AS Barcode,
                specimen_type AS SpecimenType, tube_type AS TubeType, tube_color AS TubeColor,
                volume_ml AS VolumeMl, status::text AS Status, collected_by AS CollectedBy,
                collected_by_name AS CollectedByName, collected_at AS CollectedAt,
                received_by AS ReceivedBy, received_by_name AS ReceivedByName, received_at AS ReceivedAt,
                reject_reason AS RejectReason, rejected_by AS RejectedBy, rejected_at AS RejectedAt,
                notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM specimens WHERE barcode = @Barcode";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<Specimen>(sql, new { Barcode = barcode });
    }

    public async Task<List<Specimen>> GetByOrderIdAsync(Guid orderId)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, barcode AS Barcode,
                specimen_type AS SpecimenType, tube_type AS TubeType, tube_color AS TubeColor,
                volume_ml AS VolumeMl, status::text AS Status, collected_by AS CollectedBy,
                collected_by_name AS CollectedByName, collected_at AS CollectedAt,
                received_by AS ReceivedBy, received_by_name AS ReceivedByName, received_at AS ReceivedAt,
                reject_reason AS RejectReason, rejected_by AS RejectedBy, rejected_at AS RejectedAt,
                notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM specimens WHERE order_id = @OrderId ORDER BY created_at DESC";

        using var connection = Connection;
        var results = await connection.QueryAsync<Specimen>(sql, new { OrderId = orderId });
        return results.ToList();
    }

    public async Task<List<Specimen>> GetAllAsync(Guid labId, string? barcode, string? status, Guid? orderId, int page, int pageSize)
    {
        var sql = @"SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, barcode AS Barcode,
                specimen_type AS SpecimenType, tube_type AS TubeType, tube_color AS TubeColor,
                volume_ml AS VolumeMl, status::text AS Status, collected_by AS CollectedBy,
                collected_by_name AS CollectedByName, collected_at AS CollectedAt,
                received_by AS ReceivedBy, received_by_name AS ReceivedByName, received_at AS ReceivedAt,
                reject_reason AS RejectReason, rejected_by AS RejectedBy, rejected_at AS RejectedAt,
                notes AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM specimens WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(barcode))
        {
            sql += " AND barcode ILIKE @Barcode";
            parameters.Add("Barcode", $"%{barcode}%");
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status::specimen_status";
            parameters.Add("Status", status);
        }

        if (orderId.HasValue)
        {
            sql += " AND order_id = @OrderId";
            parameters.Add("OrderId", orderId.Value);
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var results = await connection.QueryAsync<Specimen>(sql, parameters);
        return results.ToList();
    }

    public async Task<int> GetCountAsync(Guid labId, string? barcode, string? status, Guid? orderId)
    {
        var sql = "SELECT COUNT(*) FROM specimens WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(barcode))
        {
            sql += " AND barcode ILIKE @Barcode";
            parameters.Add("Barcode", $"%{barcode}%");
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status::specimen_status";
            parameters.Add("Status", status);
        }

        if (orderId.HasValue)
        {
            sql += " AND order_id = @OrderId";
            parameters.Add("OrderId", orderId.Value);
        }

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        const string sql = @"
            UPDATE specimens 
            SET status = @Status::specimen_status, updated_at = NOW() 
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    public async Task UpdateAsync(Specimen specimen)
    {
        const string sql = @"
            UPDATE specimens SET 
                status = @Status::specimen_status,
                collected_by = @CollectedBy,
                collected_by_name = @CollectedByName,
                collected_at = @CollectedAt,
                received_by = @ReceivedBy,
                received_by_name = @ReceivedByName,
                received_at = @ReceivedAt,
                reject_reason = @RejectReason,
                rejected_by = @RejectedBy,
                rejected_at = @RejectedAt,
                notes = @Notes,
                updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, specimen);
    }

    public async Task AddTrackingAsync(SpecimenTracking tracking)
    {
        const string sql = @"
            INSERT INTO specimen_tracking (id, specimen_id, action, performed_by, performed_by_name, performed_at, notes)
            VALUES (@Id, @SpecimenId, @Action, @PerformedBy, @PerformedByName, @PerformedAt, @Notes)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, tracking);
    }

    public async Task<List<SpecimenTracking>> GetTrackingAsync(Guid specimenId)
    {
        const string sql = @"
            SELECT id AS Id, specimen_id AS SpecimenId, action AS Action,
                performed_by AS PerformedBy, performed_by_name AS PerformedByName,
                performed_at AS PerformedAt, notes AS Notes
            FROM specimen_tracking WHERE specimen_id = @SpecimenId ORDER BY performed_at DESC";

        using var connection = Connection;
        var results = await connection.QueryAsync<SpecimenTracking>(sql, new { SpecimenId = specimenId });
        return results.ToList();
    }

    public async Task<int> GetTodaySequenceAsync()
    {
        const string sql = @"
            SELECT COUNT(*) + 1 FROM specimens 
            WHERE created_at::date = CURRENT_DATE";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
