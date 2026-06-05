using Dapper;
using LIS.BillingService.Models;
using Shared.Database;

namespace LIS.BillingService.Repositories;

public class BillingRepository : BaseRepository, IBillingRepository
{
    public BillingRepository(DapperContext context) : base(context) { }

    // ─── Invoice Operations ──────────────────────────────────────────────

    public async Task<LabInvoice> CreateInvoiceAsync(LabInvoice invoice)
    {
        const string sql = @"
            INSERT INTO lab_invoices (id, lab_id, invoice_number, order_id, patient_id, patient_name, 
                patient_uhid, referred_by, pricing_tier, subtotal, discount_percent, discount_amount, 
                tax_percent, tax_amount, total_amount, amount_paid, amount_due, payment_mode, status, notes, 
                created_at, updated_at)
            VALUES (@Id, @LabId, @InvoiceNumber, @OrderId, @PatientId, @PatientName, 
                @PatientUhid, @ReferredBy, @PricingTier, @Subtotal, @DiscountPercent, @DiscountAmount, 
                @TaxPercent, @TaxAmount, @TotalAmount, @AmountPaid, @AmountDue, @PaymentMode, @Status, @Notes, 
                @CreatedAt, @UpdatedAt)
            RETURNING *";

        using var connection = Connection;
        return await connection.QuerySingleAsync<LabInvoice>(sql, invoice);
    }

    public async Task SaveInvoiceItemsAsync(List<LabInvoiceItem> items)
    {
        const string sql = @"
            INSERT INTO lab_invoice_items (id, invoice_id, test_id, test_code, test_name, quantity, rate, amount, sort_order)
            VALUES (@Id, @InvoiceId, @TestId, @TestCode, @TestName, @Quantity, @Rate, @Amount, @SortOrder)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, items);
    }

    public async Task<List<LabInvoice>> GetInvoicesAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate, string? search, int page, int pageSize)
    {
        var sql = "SELECT * FROM lab_invoices WHERE lab_id = @LabId";
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

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (patient_name ILIKE @Search OR patient_uhid ILIKE @Search OR invoice_number ILIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var results = await connection.QueryAsync<LabInvoice>(sql, parameters);
        return results.ToList();
    }

    public async Task<int> GetInvoiceCountAsync(Guid labId, string? status, DateTime? fromDate, DateTime? toDate, string? search)
    {
        var sql = "SELECT COUNT(*) FROM lab_invoices WHERE lab_id = @LabId";
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

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (patient_name ILIKE @Search OR patient_uhid ILIKE @Search OR invoice_number ILIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<LabInvoice?> GetInvoiceByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM lab_invoices WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<LabInvoice>(sql, new { Id = id });
    }

    public async Task<List<LabInvoiceItem>> GetInvoiceItemsAsync(Guid invoiceId)
    {
        const string sql = "SELECT * FROM lab_invoice_items WHERE invoice_id = @InvoiceId ORDER BY sort_order";

        using var connection = Connection;
        var results = await connection.QueryAsync<LabInvoiceItem>(sql, new { InvoiceId = invoiceId });
        return results.ToList();
    }

    public async Task UpdatePaymentAsync(Guid id, decimal amountPaid, string paymentMode)
    {
        const string sql = @"
            UPDATE lab_invoices 
            SET amount_paid = amount_paid + @AmountPaid, 
                amount_due = total_amount - (amount_paid + @AmountPaid),
                payment_mode = @PaymentMode,
                status = CASE 
                    WHEN (amount_paid + @AmountPaid) >= total_amount THEN 'paid'
                    WHEN (amount_paid + @AmountPaid) > 0 THEN 'partial'
                    ELSE status
                END,
                updated_at = NOW()
            WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, new { Id = id, AmountPaid = amountPaid, PaymentMode = paymentMode });
    }

    public async Task<string> GetNextInvoiceNumberAsync(Guid labId)
    {
        var datePrefix = DateTime.UtcNow.ToString("yyMMdd");
        var pattern = $"LAB-{datePrefix}-%";

        const string sql = @"
            SELECT invoice_number FROM lab_invoices 
            WHERE lab_id = @LabId AND invoice_number LIKE @Pattern
            ORDER BY invoice_number DESC LIMIT 1";

        using var connection = Connection;
        var lastNumber = await connection.QuerySingleOrDefaultAsync<string>(sql, new { LabId = labId, Pattern = pattern });

        if (string.IsNullOrEmpty(lastNumber))
            return $"LAB-{datePrefix}-0001";

        var sequence = int.Parse(lastNumber.Split('-').Last()) + 1;
        return $"LAB-{datePrefix}-{sequence:D4}";
    }

    // ─── Billing Stats ───────────────────────────────────────────────────

    public async Task<decimal> GetTodayRevenueAsync(Guid labId)
    {
        const string sql = @"
            SELECT COALESCE(SUM(amount_paid), 0) FROM lab_invoices 
            WHERE lab_id = @LabId AND created_at::date = CURRENT_DATE AND status != 'cancelled'";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<decimal>(sql, new { LabId = labId });
    }

    public async Task<int> GetPendingBillsCountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM lab_invoices 
            WHERE lab_id = @LabId AND status IN ('unpaid', 'partial')";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { LabId = labId });
    }

    public async Task<int> GetTotalInvoicesCountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM lab_invoices 
            WHERE lab_id = @LabId AND created_at::date = CURRENT_DATE";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, new { LabId = labId });
    }

    public async Task<decimal> GetOutstandingAmountAsync(Guid labId)
    {
        const string sql = @"
            SELECT COALESCE(SUM(amount_due), 0) FROM lab_invoices 
            WHERE lab_id = @LabId AND status IN ('unpaid', 'partial')";

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<decimal>(sql, new { LabId = labId });
    }

    // ─── Inventory Operations ────────────────────────────────────────────

    public async Task<ReagentInventory> CreateReagentAsync(ReagentInventory reagent)
    {
        const string sql = @"
            INSERT INTO reagent_inventory (id, lab_id, reagent_name, manufacturer, catalog_number, lot_number, 
                expiry_date, quantity_total, quantity_remaining, unit, tests_per_unit, reorder_level, 
                cost_per_unit, instrument_id, instrument_name, status, received_date, notes, created_at, updated_at)
            VALUES (@Id, @LabId, @ReagentName, @Manufacturer, @CatalogNumber, @LotNumber, 
                @ExpiryDate, @QuantityTotal, @QuantityRemaining, @Unit, @TestsPerUnit, @ReorderLevel, 
                @CostPerUnit, @InstrumentId, @InstrumentName, @Status, @ReceivedDate, @Notes, @CreatedAt, @UpdatedAt)
            RETURNING *";

        using var connection = Connection;
        return await connection.QuerySingleAsync<ReagentInventory>(sql, reagent);
    }

    public async Task<List<ReagentInventory>> GetReagentsAsync(Guid labId, string? status, string? instrument, int page, int pageSize)
    {
        var sql = "SELECT * FROM reagent_inventory WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status";
            parameters.Add("Status", status);
        }

        if (!string.IsNullOrEmpty(instrument))
        {
            sql += " AND instrument_name ILIKE @Instrument";
            parameters.Add("Instrument", $"%{instrument}%");
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = Connection;
        var results = await connection.QueryAsync<ReagentInventory>(sql, parameters);
        return results.ToList();
    }

    public async Task<int> GetReagentCountAsync(Guid labId, string? status, string? instrument)
    {
        var sql = "SELECT COUNT(*) FROM reagent_inventory WHERE lab_id = @LabId";
        var parameters = new DynamicParameters();
        parameters.Add("LabId", labId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND status = @Status";
            parameters.Add("Status", status);
        }

        if (!string.IsNullOrEmpty(instrument))
        {
            sql += " AND instrument_name ILIKE @Instrument";
            parameters.Add("Instrument", $"%{instrument}%");
        }

        using var connection = Connection;
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<ReagentInventory?> GetReagentByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM reagent_inventory WHERE id = @Id";

        using var connection = Connection;
        return await connection.QuerySingleOrDefaultAsync<ReagentInventory>(sql, new { Id = id });
    }

    public async Task UpdateReagentAsync(Guid id, decimal? quantityRemaining, string? status, string? notes)
    {
        var setClauses = new List<string> { "updated_at = NOW()" };
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        if (quantityRemaining.HasValue)
        {
            setClauses.Add("quantity_remaining = @QuantityRemaining");
            parameters.Add("QuantityRemaining", quantityRemaining.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            setClauses.Add("status = @Status");
            parameters.Add("Status", status);
        }

        if (notes != null)
        {
            setClauses.Add("notes = @Notes");
            parameters.Add("Notes", notes);
        }

        var sql = $"UPDATE reagent_inventory SET {string.Join(", ", setClauses)} WHERE id = @Id";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<List<ReagentInventory>> GetLowStockAlertsAsync(Guid labId)
    {
        const string sql = @"
            SELECT * FROM reagent_inventory 
            WHERE lab_id = @LabId AND quantity_remaining <= reorder_level AND status != 'out_of_stock'
            ORDER BY quantity_remaining ASC";

        using var connection = Connection;
        var results = await connection.QueryAsync<ReagentInventory>(sql, new { LabId = labId });
        return results.ToList();
    }

    public async Task<List<ReagentInventory>> GetExpiringAlertsAsync(Guid labId, int daysAhead = 30)
    {
        const string sql = @"
            SELECT * FROM reagent_inventory 
            WHERE lab_id = @LabId AND expiry_date IS NOT NULL 
                AND expiry_date <= (CURRENT_DATE + @DaysAhead * INTERVAL '1 day')
                AND status NOT IN ('expired', 'out_of_stock')
            ORDER BY expiry_date ASC";

        using var connection = Connection;
        var results = await connection.QueryAsync<ReagentInventory>(sql, new { LabId = labId, DaysAhead = daysAhead });
        return results.ToList();
    }

    public async Task LogConsumptionAsync(ReagentConsumptionLog log)
    {
        const string sql = @"
            INSERT INTO reagent_consumption_log (id, reagent_id, quantity_used, test_name, order_id, consumed_at, notes)
            VALUES (@Id, @ReagentId, @QuantityUsed, @TestName, @OrderId, @ConsumedAt, @Notes)";

        using var connection = Connection;
        await connection.ExecuteAsync(sql, log);
    }
}
