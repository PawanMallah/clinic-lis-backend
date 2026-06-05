using Dapper;
using LIS.OrderService.Models;
using Shared.Database;

namespace LIS.OrderService.Repositories;

public class PatientRepository : BaseRepository, IPatientRepository
{
    public PatientRepository(DapperContext context) : base(context) { }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        const string sql = @"
            INSERT INTO patients (id, lab_id, first_name, last_name, full_name, date_of_birth, gender, age,
                mobile, email, address, city, state, pincode, uhid, mrn, blood_group,
                referred_by_doctor, treating_doctor, insurance_provider, insurance_id,
                is_active, created_at, updated_at)
            VALUES (@Id, @LabId, @FirstName, @LastName, @FullName, @DateOfBirth, @Gender, @Age,
                @Mobile, @Email, @Address, @City, @State, @Pincode, @Uhid, @Mrn, @BloodGroup,
                @ReferredByDoctor, @TreatingDoctor, @InsuranceProvider, @InsuranceId,
                @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, lab_id AS LabId, first_name AS FirstName, last_name AS LastName,
                full_name AS FullName, date_of_birth AS DateOfBirth, gender AS Gender, age AS Age,
                mobile AS Mobile, email AS Email, address AS Address, city AS City, state AS State,
                pincode AS Pincode, uhid AS Uhid, mrn AS Mrn, blood_group AS BloodGroup,
                referred_by_doctor AS ReferredByDoctor, treating_doctor AS TreatingDoctor,
                insurance_provider AS InsuranceProvider, insurance_id AS InsuranceId,
                is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt";

        using var connection = Connection;
        return await connection.QuerySingleAsync<Patient>(sql, patient);
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, first_name AS FirstName, last_name AS LastName,
                full_name AS FullName, date_of_birth AS DateOfBirth, gender AS Gender, age AS Age,
                mobile AS Mobile, email AS Email, address AS Address, city AS City, state AS State,
                pincode AS Pincode, uhid AS Uhid, mrn AS Mrn, blood_group AS BloodGroup,
                referred_by_doctor AS ReferredByDoctor, treating_doctor AS TreatingDoctor,
                insurance_provider AS InsuranceProvider, insurance_id AS InsuranceId,
                is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM patients WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<Patient>(sql, new { Id = id });
    }

    public async Task<Patient?> FindByMatchAsync(Guid labId, string firstName, string? mobile, DateTime? dateOfBirth)
    {
        // Dedup logic: match on lab + first_name + mobile + dob
        if (string.IsNullOrWhiteSpace(mobile) || dateOfBirth == null)
        {
            // If mobile or dob missing, try matching on UHID or name+mobile
            if (!string.IsNullOrWhiteSpace(mobile))
            {
                const string sqlByMobile = @"
                    SELECT id AS Id, lab_id AS LabId, first_name AS FirstName, last_name AS LastName,
                        full_name AS FullName, date_of_birth AS DateOfBirth, gender AS Gender, age AS Age,
                        mobile AS Mobile, email AS Email, address AS Address, city AS City, state AS State,
                        pincode AS Pincode, uhid AS Uhid, mrn AS Mrn, blood_group AS BloodGroup,
                        referred_by_doctor AS ReferredByDoctor, treating_doctor AS TreatingDoctor,
                        insurance_provider AS InsuranceProvider, insurance_id AS InsuranceId,
                        is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                    FROM patients 
                    WHERE lab_id = @LabId AND LOWER(first_name) = LOWER(@FirstName) AND mobile = @Mobile
                    LIMIT 1";

                using var conn = Connection;
                return await conn.QuerySingleOrDefaultAsync<Patient>(sqlByMobile, new { LabId = labId, FirstName = firstName, Mobile = mobile });
            }
            return null;
        }

        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, first_name AS FirstName, last_name AS LastName,
                full_name AS FullName, date_of_birth AS DateOfBirth, gender AS Gender, age AS Age,
                mobile AS Mobile, email AS Email, address AS Address, city AS City, state AS State,
                pincode AS Pincode, uhid AS Uhid, mrn AS Mrn, blood_group AS BloodGroup,
                referred_by_doctor AS ReferredByDoctor, treating_doctor AS TreatingDoctor,
                insurance_provider AS InsuranceProvider, insurance_id AS InsuranceId,
                is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM patients 
            WHERE lab_id = @LabId AND LOWER(first_name) = LOWER(@FirstName) AND mobile = @Mobile AND date_of_birth = @DateOfBirth
            LIMIT 1";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<Patient>(sql, new { LabId = labId, FirstName = firstName, Mobile = mobile, DateOfBirth = dateOfBirth });
    }

    public async Task<List<Patient>> SearchAsync(Guid labId, string? search, int page, int pageSize)
    {
        var sql = @"
            SELECT id AS Id, lab_id AS LabId, first_name AS FirstName, last_name AS LastName,
                full_name AS FullName, date_of_birth AS DateOfBirth, gender AS Gender, age AS Age,
                mobile AS Mobile, email AS Email, uhid AS Uhid, mrn AS Mrn,
                is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM patients 
            WHERE lab_id = @LabId AND is_active = true";

        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (first_name ILIKE @Search OR last_name ILIKE @Search OR mobile ILIKE @Search OR uhid ILIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var results = await connection.QueryAsync<Patient>(sql, parameters);
        return results.ToList();
    }

    public async Task UpdateAsync(Patient patient)
    {
        const string sql = @"
            UPDATE patients SET
                first_name = @FirstName, last_name = @LastName, full_name = @FullName,
                date_of_birth = @DateOfBirth, gender = @Gender, age = @Age,
                mobile = @Mobile, email = @Email, address = @Address, city = @City,
                state = @State, pincode = @Pincode, uhid = @Uhid, mrn = @Mrn,
                blood_group = @BloodGroup, referred_by_doctor = @ReferredByDoctor,
                treating_doctor = @TreatingDoctor, insurance_provider = @InsuranceProvider,
                insurance_id = @InsuranceId, is_active = @IsActive, updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, patient);
    }
}
