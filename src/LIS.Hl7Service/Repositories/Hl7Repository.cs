using Dapper;
using LIS.Hl7Service.Models;
using Shared.Database;

namespace LIS.Hl7Service.Repositories;

public class Hl7Repository : BaseRepository, IHl7Repository
{
    public Hl7Repository(DapperContext context) : base(context) { }

    #region Connections

    public async Task<Hl7Connection> CreateConnectionAsync(Hl7Connection connection)
    {
        const string sql = @"
            INSERT INTO hl7_connections (id, lab_id, instrument_name, instrument_model, manufacturer,
                serial_number, host, port, protocol, direction, status, auto_reconnect,
                test_code_mapping, settings, created_at, updated_at)
            VALUES (@Id, @LabId, @InstrumentName, @InstrumentModel, @Manufacturer,
                @SerialNumber, @Host, @Port, @Protocol, @Direction, @Status, @AutoReconnect,
                @TestCodeMapping::jsonb, @Settings::jsonb, @CreatedAt, @UpdatedAt)
            RETURNING *";

        using var connection2 = Connection;
        return await connection2.QuerySingleAsync<Hl7Connection>(sql, connection);
    }

    public async Task<Hl7Connection?> GetConnectionByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM hl7_connections WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<Hl7Connection>(sql, new { Id = id });
    }

    public async Task<List<Hl7Connection>> GetAllConnectionsAsync(Guid labId)
    {
        const string sql = "SELECT * FROM hl7_connections WHERE lab_id = @LabId ORDER BY created_at DESC";

        using var connection = Connection;
        var results = await connection.QueryAsync<Hl7Connection>(sql, new { LabId = labId });
        return results.ToList();
    }

    public async Task<List<Hl7Connection>> GetActiveConnectionsAsync()
    {
        const string sql = "SELECT * FROM hl7_connections WHERE status != 'error' AND auto_reconnect = true";

        using var connection = Connection;
        var results = await connection.QueryAsync<Hl7Connection>(sql);
        return results.ToList();
    }

    public async Task UpdateConnectionStatusAsync(Guid id, string status)
    {
        var sql = @"
            UPDATE hl7_connections 
            SET status = @Status, updated_at = NOW()";

        if (status == "connected")
        {
            sql += ", last_connected_at = NOW()";
        }

        sql += " WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    public async Task UpdateLastMessageTimeAsync(Guid connectionId)
    {
        const string sql = @"
            UPDATE hl7_connections 
            SET last_message_at = NOW(), updated_at = NOW() 
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = connectionId });
    }

    public async Task DeleteConnectionAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM instrument_test_mapping WHERE connection_id = @Id;
            DELETE FROM hl7_message_log WHERE connection_id = @Id;
            DELETE FROM hl7_connections WHERE id = @Id;";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    #endregion

    #region Message Logs

    public async Task LogMessageAsync(Hl7MessageLog log)
    {
        const string sql = @"
            INSERT INTO hl7_message_log (id, connection_id, direction, message_type, trigger_event,
                control_id, raw_message, parsed_json, status, error_message, processing_time_ms, created_at)
            VALUES (@Id, @ConnectionId, @Direction, @MessageType, @TriggerEvent,
                @ControlId, @RawMessage, @ParsedJson::jsonb, @Status, @ErrorMessage, @ProcessingTimeMs, @CreatedAt)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, log);
    }

    public async Task<List<Hl7MessageLog>> GetMessageLogsAsync(Guid connectionId, int page, int pageSize)
    {
        const string sql = @"
            SELECT * FROM hl7_message_log 
            WHERE connection_id = @ConnectionId 
            ORDER BY created_at DESC 
            LIMIT @PageSize OFFSET @Offset";

        using var connection = Connection;
        var results = await connection.QueryAsync<Hl7MessageLog>(sql, new
        {
            ConnectionId = connectionId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });
        return results.ToList();
    }

    public async Task<Hl7MessageLog?> GetMessageLogByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM hl7_message_log WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<Hl7MessageLog>(sql, new { Id = id });
    }

    public async Task<int> GetMessageLogCountAsync(Guid connectionId)
    {
        const string sql = "SELECT COUNT(*) FROM hl7_message_log WHERE connection_id = @ConnectionId";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { ConnectionId = connectionId });
    }

    #endregion

    #region Mappings

    public async Task<InstrumentTestMapping> CreateMappingAsync(InstrumentTestMapping mapping)
    {
        const string sql = @"
            INSERT INTO instrument_test_mapping (id, connection_id, instrument_test_code, instrument_test_name,
                lis_test_id, lis_test_code, lis_test_name, is_active, created_at)
            VALUES (@Id, @ConnectionId, @InstrumentTestCode, @InstrumentTestName,
                @LisTestId, @LisTestCode, @LisTestName, @IsActive, @CreatedAt)
            RETURNING *";

        using var connection = Connection;
        return await connection.QuerySingleAsync<InstrumentTestMapping>(sql, mapping);
    }

    public async Task<List<InstrumentTestMapping>> GetMappingsAsync(Guid connectionId)
    {
        const string sql = @"
            SELECT * FROM instrument_test_mapping 
            WHERE connection_id = @ConnectionId 
            ORDER BY instrument_test_code";

        using var connection = Connection;
        var results = await connection.QueryAsync<InstrumentTestMapping>(sql, new { ConnectionId = connectionId });
        return results.ToList();
    }

    public async Task DeleteMappingAsync(Guid id)
    {
        const string sql = "DELETE FROM instrument_test_mapping WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    #endregion
}
