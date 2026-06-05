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

    // Phase B: Order Lifecycle
    Task UpdateStatusWithHistoryAsync(Guid orderId, string newStatus, Guid? userId, string? userName, string? reason);
    Task<AddonTest> AddAddonTestAsync(Guid orderId, LabOrderTest test, string? reason, Guid? userId, string? userName, bool specimenValid, string? notes);
    Task<List<OrderStatusHistory>> GetOrderHistoryAsync(Guid orderId);
    Task<ReferenceLabSendout> CreateSendoutAsync(ReferenceLabSendout sendout);
    Task<List<ReferenceLabSendout>> GetSendoutsAsync(Guid labId, string? status);
    Task<ReferenceLabSendout?> GetSendoutByIdAsync(Guid id);
    Task UpdateSendoutAsync(Guid id, ReferenceLabSendout sendout);
}
