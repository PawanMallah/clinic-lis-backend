using Dapper;
using LIS.ReportService.Models;
using Shared.Database;

namespace LIS.ReportService.Repositories;

public class ReportRepository : BaseRepository, IReportRepository
{
    public ReportRepository(DapperContext context) : base(context) { }

    public async Task<LabReport> CreateReportAsync(LabReport report)
    {
        const string sql = @"
            INSERT INTO lab_reports (id, lab_id, order_id, patient_name, report_number, status, 
                report_pdf_url, report_json, generated_at, version, created_at, updated_at)
            VALUES (@Id, @LabId, @OrderId, @PatientName, @ReportNumber, @Status, 
                @ReportPdfUrl, @ReportJson::jsonb, @GeneratedAt, @Version, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, lab_id AS LabId, order_id AS OrderId, patient_name AS PatientName,
                report_number AS ReportNumber, status AS Status, report_pdf_url AS ReportPdfUrl,
                report_json AS ReportJson, generated_at AS GeneratedAt, delivered_at AS DeliveredAt,
                delivered_via AS DeliveredVia, version AS Version, amendment_reason AS AmendmentReason,
                signed_by AS SignedBy, signed_by_name AS SignedByName, signed_at AS SignedAt,
                created_at AS CreatedAt, updated_at AS UpdatedAt";

        using var connection = Connection;
        return await connection.QuerySingleAsync<LabReport>(sql, report);
    }

    public async Task<LabReport?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, patient_name AS PatientName,
                report_number AS ReportNumber, status AS Status, report_pdf_url AS ReportPdfUrl,
                report_json AS ReportJson, generated_at AS GeneratedAt, delivered_at AS DeliveredAt,
                delivered_via AS DeliveredVia, version AS Version, amendment_reason AS AmendmentReason,
                signed_by AS SignedBy, signed_by_name AS SignedByName, signed_at AS SignedAt,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM lab_reports WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<LabReport>(sql, new { Id = id });
    }

    public async Task<LabReport?> GetByOrderIdAsync(Guid orderId)
    {
        const string sql = @"
            SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, patient_name AS PatientName,
                report_number AS ReportNumber, status AS Status, report_pdf_url AS ReportPdfUrl,
                report_json AS ReportJson, generated_at AS GeneratedAt, delivered_at AS DeliveredAt,
                delivered_via AS DeliveredVia, version AS Version, amendment_reason AS AmendmentReason,
                signed_by AS SignedBy, signed_by_name AS SignedByName, signed_at AS SignedAt,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM lab_reports WHERE order_id = @OrderId ORDER BY version DESC LIMIT 1";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<LabReport>(sql, new { OrderId = orderId });
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        const string sql = @"
            UPDATE lab_reports SET status = @Status, updated_at = NOW() WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    public async Task SignReportAsync(Guid id, Guid signedBy, string signedByName)
    {
        const string sql = @"
            UPDATE lab_reports 
            SET status = 'finalized', signed_by = @SignedBy, signed_by_name = @SignedByName, 
                signed_at = NOW(), updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, SignedBy = signedBy, SignedByName = signedByName });
    }

    public async Task DeliverReportAsync(Guid id, string deliveredVia)
    {
        const string sql = @"
            UPDATE lab_reports 
            SET status = 'delivered', delivered_at = NOW(), delivered_via = @DeliveredVia, updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, DeliveredVia = deliveredVia });
    }

    public async Task<LabReport> AmendReportAsync(LabReport report)
    {
        const string sql = @"
            INSERT INTO lab_reports (id, lab_id, order_id, patient_name, report_number, status, 
                report_pdf_url, report_json, generated_at, version, amendment_reason, created_at, updated_at)
            VALUES (@Id, @LabId, @OrderId, @PatientName, @ReportNumber, @Status, 
                @ReportPdfUrl, @ReportJson::jsonb, @GeneratedAt, @Version, @AmendmentReason, @CreatedAt, @UpdatedAt)
            RETURNING id AS Id, lab_id AS LabId, order_id AS OrderId, patient_name AS PatientName,
                report_number AS ReportNumber, status AS Status, report_pdf_url AS ReportPdfUrl,
                report_json AS ReportJson, generated_at AS GeneratedAt, delivered_at AS DeliveredAt,
                delivered_via AS DeliveredVia, version AS Version, amendment_reason AS AmendmentReason,
                signed_by AS SignedBy, signed_by_name AS SignedByName, signed_at AS SignedAt,
                created_at AS CreatedAt, updated_at AS UpdatedAt";

        using var connection = Connection;
        return await connection.QuerySingleAsync<LabReport>(sql, report);
    }

    public async Task<List<LabReport>> GetReportsAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var sql = @"SELECT id AS Id, lab_id AS LabId, order_id AS OrderId, patient_name AS PatientName,
                report_number AS ReportNumber, status AS Status, report_pdf_url AS ReportPdfUrl,
                report_json AS ReportJson, generated_at AS GeneratedAt, delivered_at AS DeliveredAt,
                delivered_via AS DeliveredVia, version AS Version, amendment_reason AS AmendmentReason,
                signed_by AS SignedBy, signed_by_name AS SignedByName, signed_at AS SignedAt,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM lab_reports WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status";
            parameters.Add("Status", status);
        }

        if (fromDate.HasValue)
        {
            sql += " AND created_at >= @FromDate";
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND created_at <= @ToDate";
            parameters.Add("ToDate", toDate.Value);
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var results = await connection.QueryAsync<LabReport>(sql, parameters);
        return results.ToList();
    }

    public async Task<int> GetReportCountAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var sql = "SELECT COUNT(*) FROM lab_reports WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status";
            parameters.Add("Status", status);
        }

        if (fromDate.HasValue)
        {
            sql += " AND created_at >= @FromDate";
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND created_at <= @ToDate";
            parameters.Add("ToDate", toDate.Value);
        }

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<string> GetNextReportNumberAsync(Guid labId)
    {
        var datePrefix = DateTime.UtcNow.ToString("yyMMdd");
        var pattern = $"RPT-{datePrefix}-%";

        const string sql = @"
            SELECT report_number FROM lab_reports 
            WHERE lab_id = @LabId AND report_number LIKE @Pattern
            ORDER BY report_number DESC LIMIT 1";

        using var connection = Connection;
        var lastNumber = await connection.QuerySingleOrDefaultAsync<string>(sql, new { LabId = labId, Pattern = pattern });

        if (string.IsNullOrEmpty(lastNumber))
            return $"RPT-{datePrefix}-0001";

        var sequence = int.Parse(lastNumber.Split('-').Last()) + 1;
        return $"RPT-{datePrefix}-{sequence:D4}";
    }
}
