using LIS.BillingService.Models;

namespace LIS.BillingService.Repositories;

public interface IBillingRepository
{
    // Invoice operations
    Task<LabInvoice> CreateInvoiceAsync(LabInvoice invoice);
    Task SaveInvoiceItemsAsync(List<LabInvoiceItem> items);
    Task<List<LabInvoice>> GetInvoicesAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate, string? search, int page, int pageSize);
    Task<int> GetInvoiceCountAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate, string? search);
    Task<LabInvoice?> GetInvoiceByIdAsync(Guid id);
    Task<List<LabInvoiceItem>> GetInvoiceItemsAsync(Guid invoiceId);
    Task UpdatePaymentAsync(Guid id, decimal amountPaid, string paymentMode);
    Task<string> GetNextInvoiceNumberAsync(Guid labId);

    // Billing stats
    Task<decimal> GetTodayRevenueAsync(Guid labId);
    Task<int> GetPendingBillsCountAsync(Guid labId);
    Task<int> GetTotalInvoicesCountAsync(Guid labId);
    Task<decimal> GetOutstandingAmountAsync(Guid labId);

    // Inventory operations
    Task<ReagentInventory> CreateReagentAsync(ReagentInventory reagent);
    Task<List<ReagentInventory>> GetReagentsAsync(Guid labId, string? status, string? instrument, int page, int pageSize);
    Task<int> GetReagentCountAsync(Guid labId, string? status, string? instrument);
    Task<ReagentInventory?> GetReagentByIdAsync(Guid id);
    Task UpdateReagentAsync(Guid id, decimal? quantityRemaining, string? status, string? notes);
    Task<List<ReagentInventory>> GetLowStockAlertsAsync(Guid labId);
    Task<List<ReagentInventory>> GetExpiringAlertsAsync(Guid labId, int daysAhead = 30);
    Task LogConsumptionAsync(ReagentConsumptionLog log);
}
