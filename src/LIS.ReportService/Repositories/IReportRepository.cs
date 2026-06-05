using LIS.ReportService.Models;

namespace LIS.ReportService.Repositories;

public interface IReportRepository
{
    Task<LabReport> CreateReportAsync(LabReport report);
    Task<LabReport?> GetByIdAsync(Guid id);
    Task<LabReport?> GetByOrderIdAsync(Guid orderId);
    Task UpdateStatusAsync(Guid id, string status);
    Task SignReportAsync(Guid id, Guid signedBy, string signedByName);
    Task DeliverReportAsync(Guid id, string deliveredVia);
    Task<LabReport> AmendReportAsync(LabReport report);
    Task<List<LabReport>> GetReportsAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<int> GetReportCountAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate);
    Task<string> GetNextReportNumberAsync(Guid labId);
}
