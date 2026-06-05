namespace LIS.OrderService.Models;

public class Patient
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? Uhid { get; set; }
    public string? Mrn { get; set; }
    public string? BloodGroup { get; set; }
    public string? ReferredByDoctor { get; set; }
    public string? TreatingDoctor { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
