using LIS.OrderService.Models;

namespace LIS.OrderService.Repositories;

public interface IPatientRepository
{
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient?> FindByMatchAsync(Guid labId, string firstName, string? mobile, DateTime? dateOfBirth);
    Task<List<Patient>> SearchAsync(Guid labId, string? search, int page, int pageSize);
    Task UpdateAsync(Patient patient);
}
