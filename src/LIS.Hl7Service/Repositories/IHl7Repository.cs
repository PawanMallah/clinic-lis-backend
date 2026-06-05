using LIS.Hl7Service.Models;

namespace LIS.Hl7Service.Repositories;

public interface IHl7Repository
{
    // Connections
    Task<Hl7Connection> CreateConnectionAsync(Hl7Connection connection);
    Task<Hl7Connection?> GetConnectionByIdAsync(Guid id);
    Task<List<Hl7Connection>> GetAllConnectionsAsync(Guid labId);
    Task<List<Hl7Connection>> GetActiveConnectionsAsync();
    Task UpdateConnectionStatusAsync(Guid id, string status);
    Task UpdateLastMessageTimeAsync(Guid connectionId);
    Task DeleteConnectionAsync(Guid id);

    // Message Logs
    Task LogMessageAsync(Hl7MessageLog log);
    Task<List<Hl7MessageLog>> GetMessageLogsAsync(Guid connectionId, int page, int pageSize);
    Task<Hl7MessageLog?> GetMessageLogByIdAsync(Guid id);
    Task<int> GetMessageLogCountAsync(Guid connectionId);

    // Mappings
    Task<InstrumentTestMapping> CreateMappingAsync(InstrumentTestMapping mapping);
    Task<List<InstrumentTestMapping>> GetMappingsAsync(Guid connectionId);
    Task DeleteMappingAsync(Guid id);
}
