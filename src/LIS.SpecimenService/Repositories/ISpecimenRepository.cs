using LIS.SpecimenService.Models;

namespace LIS.SpecimenService.Repositories;

public interface ISpecimenRepository
{
    Task<Specimen> CreateAsync(Specimen specimen);
    Task<Specimen?> GetByIdAsync(Guid id);
    Task<Specimen?> GetByBarcodeAsync(string barcode);
    Task<List<Specimen>> GetByOrderIdAsync(Guid orderId);
    Task<List<Specimen>> GetAllAsync(Guid labId, string? barcode, string? status, Guid? orderId, int page, int pageSize);
    Task<int> GetCountAsync(Guid labId, string? barcode, string? status, Guid? orderId);
    Task UpdateStatusAsync(Guid id, string status);
    Task UpdateAsync(Specimen specimen);
    Task AddTrackingAsync(SpecimenTracking tracking);
    Task<List<SpecimenTracking>> GetTrackingAsync(Guid specimenId);
    Task<int> GetTodaySequenceAsync();
}
