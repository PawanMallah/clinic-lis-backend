using LIS.OrderService.Models;

namespace LIS.OrderService.Repositories;

public interface IOrderRepository
{
    Task<LabOrder> CreateAsync(LabOrder order);
    Task<LabOrder?> GetByIdAsync(Guid id);
    Task<List<LabOrder>> GetAllAsync(Guid labId, string? status, string? priority, string? search, int page, int pageSize);
    Task<int> GetCountAsync(Guid labId, string? status, string? priority, string? search);
    Task UpdateStatusAsync(Guid id, string status);
    Task SaveOrderTestsAsync(Guid orderId, List<LabOrderTest> tests);
    Task<List<LabOrderTest>> GetOrderTestsAsync(Guid orderId);
    Task<int> GetTodayCountAsync(Guid labId);
    Task<int> GetPendingCountAsync(Guid labId);
    Task<int> GetCompletedCountAsync(Guid labId);
    Task<int> GetStatCountAsync(Guid labId);
}
