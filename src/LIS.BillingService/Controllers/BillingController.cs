using LIS.BillingService.DTOs;
using LIS.BillingService.Models;
using LIS.BillingService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace LIS.BillingService.Controllers;

[ApiController]
[Route("[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingRepository _billingRepository;

    public BillingController(IBillingRepository billingRepository)
    {
        _billingRepository = billingRepository;
    }

    private Guid GetLabId()
    {
        var labIdStr = HttpContext.Items["labId"]?.ToString();
        if (string.IsNullOrEmpty(labIdStr) || !Guid.TryParse(labIdStr, out var labId))
            throw new UnauthorizedAccessException("Lab context not found");
        return labId;
    }

    // ─── Invoice Endpoints ───────────────────────────────────────────────

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("At least one item is required"));

        var labId = GetLabId();
        var invoiceNumber = await _billingRepository.GetNextInvoiceNumberAsync(labId);

        // Calculate amounts
        var subtotal = request.Items.Sum(i => i.Rate * i.Quantity);
        var discountAmount = subtotal * (request.DiscountPercent / 100m);
        var taxableAmount = subtotal - discountAmount;
        var taxAmount = taxableAmount * (request.TaxPercent / 100m);
        var totalAmount = taxableAmount + taxAmount;
        var amountDue = totalAmount - request.AmountPaid;

        var status = request.AmountPaid >= totalAmount ? "paid" 
            : request.AmountPaid > 0 ? "partial" 
            : "unpaid";

        var invoice = new LabInvoice
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            InvoiceNumber = invoiceNumber,
            OrderId = request.OrderId,
            PatientName = request.PatientName,
            PatientUhid = request.PatientUhid,
            ReferredBy = request.ReferredBy,
            PricingTier = request.PricingTier,
            Subtotal = subtotal,
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = discountAmount,
            TaxPercent = request.TaxPercent,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            AmountPaid = request.AmountPaid,
            AmountDue = amountDue,
            PaymentMode = request.PaymentMode,
            Status = status,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _billingRepository.CreateInvoiceAsync(invoice);

        var items = request.Items.Select((item, index) => new LabInvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = created.Id,
            TestId = item.TestId,
            TestCode = item.TestCode,
            TestName = item.TestName,
            Quantity = item.Quantity,
            Rate = item.Rate,
            Amount = item.Rate * item.Quantity,
            SortOrder = index
        }).ToList();

        await _billingRepository.SaveInvoiceItemsAsync(items);

        var response = MapToInvoiceResponse(created, items);
        return Created($"/billing/invoices/{created.Id}", ApiResponse<InvoiceResponse>.Ok(response, "Invoice created successfully"));
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var invoices = await _billingRepository.GetInvoicesAsync(labId, status, fromDate, toDate, search, page, pageSize);
        var total = await _billingRepository.GetInvoiceCountAsync(labId, status, fromDate, toDate, search);

        var invoiceResponses = new List<InvoiceResponse>();
        foreach (var invoice in invoices)
        {
            var items = await _billingRepository.GetInvoiceItemsAsync(invoice.Id);
            invoiceResponses.Add(MapToInvoiceResponse(invoice, items));
        }

        var result = new InvoiceListResponse
        {
            Invoices = invoiceResponses,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<InvoiceListResponse>.Ok(result));
    }

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        var invoice = await _billingRepository.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return NotFound(ApiResponse<object>.Fail("Invoice not found"));

        var items = await _billingRepository.GetInvoiceItemsAsync(id);
        var response = MapToInvoiceResponse(invoice, items);

        return Ok(ApiResponse<InvoiceResponse>.Ok(response));
    }

    [HttpPost("invoices/{id}/payment")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        if (request.AmountPaid <= 0)
            return BadRequest(ApiResponse<object>.Fail("Amount must be greater than zero"));

        var invoice = await _billingRepository.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return NotFound(ApiResponse<object>.Fail("Invoice not found"));

        if (invoice.Status == "paid")
            return BadRequest(ApiResponse<object>.Fail("Invoice is already fully paid"));

        if (invoice.Status == "cancelled")
            return BadRequest(ApiResponse<object>.Fail("Cannot pay a cancelled invoice"));

        await _billingRepository.UpdatePaymentAsync(id, request.AmountPaid, request.PaymentMode);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), amountPaid = request.AmountPaid }, "Payment recorded successfully"));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var labId = GetLabId();

        var stats = new BillingStatsResponse
        {
            TodayRevenue = await _billingRepository.GetTodayRevenueAsync(labId),
            PendingBills = await _billingRepository.GetPendingBillsCountAsync(labId),
            TotalInvoices = await _billingRepository.GetTotalInvoicesCountAsync(labId),
            OutstandingAmount = await _billingRepository.GetOutstandingAmountAsync(labId)
        };

        return Ok(ApiResponse<BillingStatsResponse>.Ok(stats));
    }

    // ─── Inventory Endpoints ─────────────────────────────────────────────

    [HttpPost("inventory")]
    public async Task<IActionResult> CreateReagent([FromBody] CreateReagentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReagentName))
            return BadRequest(ApiResponse<object>.Fail("Reagent name is required"));

        var labId = GetLabId();

        var reagent = new ReagentInventory
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            ReagentName = request.ReagentName,
            Manufacturer = request.Manufacturer,
            CatalogNumber = request.CatalogNumber,
            LotNumber = request.LotNumber,
            ExpiryDate = request.ExpiryDate,
            QuantityTotal = request.QuantityTotal,
            QuantityRemaining = request.QuantityTotal,
            Unit = request.Unit,
            TestsPerUnit = request.TestsPerUnit,
            ReorderLevel = request.ReorderLevel,
            CostPerUnit = request.CostPerUnit,
            InstrumentName = request.InstrumentName,
            Status = "in_stock",
            ReceivedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _billingRepository.CreateReagentAsync(reagent);
        var response = MapToReagentResponse(created);

        return Created($"/billing/inventory/{created.Id}", ApiResponse<ReagentResponse>.Ok(response, "Reagent added to inventory"));
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetReagents(
        [FromQuery] string? status,
        [FromQuery] string? instrument,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var reagents = await _billingRepository.GetReagentsAsync(labId, status, instrument, page, pageSize);
        var total = await _billingRepository.GetReagentCountAsync(labId, status, instrument);

        var result = new ReagentListResponse
        {
            Reagents = reagents.Select(MapToReagentResponse).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<ReagentListResponse>.Ok(result));
    }

    [HttpPatch("inventory/{id}")]
    public async Task<IActionResult> UpdateReagent(Guid id, [FromBody] UpdateReagentRequest request)
    {
        var reagent = await _billingRepository.GetReagentByIdAsync(id);
        if (reagent == null)
            return NotFound(ApiResponse<object>.Fail("Reagent not found"));

        await _billingRepository.UpdateReagentAsync(id, request.QuantityRemaining, request.Status, request.Notes);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString() }, "Reagent updated"));
    }

    [HttpGet("inventory/alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        var labId = GetLabId();

        var lowStock = await _billingRepository.GetLowStockAlertsAsync(labId);
        var expiring = await _billingRepository.GetExpiringAlertsAsync(labId);

        var alerts = new List<InventoryAlertResponse>();

        alerts.AddRange(lowStock.Select(r => new InventoryAlertResponse
        {
            Id = r.Id.ToString(),
            ReagentName = r.ReagentName,
            LotNumber = r.LotNumber,
            Status = r.Status,
            QuantityRemaining = r.QuantityRemaining,
            ReorderLevel = r.ReorderLevel,
            ExpiryDate = r.ExpiryDate?.ToString("yyyy-MM-dd"),
            AlertType = "low_stock"
        }));

        alerts.AddRange(expiring.Select(r => new InventoryAlertResponse
        {
            Id = r.Id.ToString(),
            ReagentName = r.ReagentName,
            LotNumber = r.LotNumber,
            Status = r.Status,
            QuantityRemaining = r.QuantityRemaining,
            ReorderLevel = r.ReorderLevel,
            ExpiryDate = r.ExpiryDate?.ToString("yyyy-MM-dd"),
            AlertType = "expiring"
        }));

        return Ok(ApiResponse<List<InventoryAlertResponse>>.Ok(alerts));
    }

    [HttpPost("inventory/{id}/consume")]
    public async Task<IActionResult> ConsumeReagent(Guid id, [FromBody] ConsumeReagentRequest request)
    {
        if (request.QuantityUsed <= 0)
            return BadRequest(ApiResponse<object>.Fail("Quantity must be greater than zero"));

        var reagent = await _billingRepository.GetReagentByIdAsync(id);
        if (reagent == null)
            return NotFound(ApiResponse<object>.Fail("Reagent not found"));

        if (reagent.QuantityRemaining < request.QuantityUsed)
            return BadRequest(ApiResponse<object>.Fail("Insufficient quantity"));

        var newQuantity = reagent.QuantityRemaining - request.QuantityUsed;
        var newStatus = newQuantity <= 0 ? "out_of_stock" 
            : newQuantity <= reagent.ReorderLevel ? "low" 
            : reagent.Status;

        await _billingRepository.UpdateReagentAsync(id, newQuantity, newStatus, null);

        var log = new ReagentConsumptionLog
        {
            Id = Guid.NewGuid(),
            ReagentId = id,
            QuantityUsed = request.QuantityUsed,
            TestName = request.TestName,
            OrderId = request.OrderId,
            ConsumedAt = DateTime.UtcNow,
            Notes = request.Notes
        };

        await _billingRepository.LogConsumptionAsync(log);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), quantityRemaining = newQuantity, status = newStatus }, "Consumption recorded"));
    }

    // ─── Mapping Helpers ─────────────────────────────────────────────────

    private static InvoiceResponse MapToInvoiceResponse(LabInvoice invoice, List<LabInvoiceItem> items)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id.ToString(),
            InvoiceNumber = invoice.InvoiceNumber,
            OrderId = invoice.OrderId?.ToString(),
            PatientName = invoice.PatientName,
            PatientUhid = invoice.PatientUhid,
            ReferredBy = invoice.ReferredBy,
            PricingTier = invoice.PricingTier,
            Subtotal = invoice.Subtotal,
            DiscountPercent = invoice.DiscountPercent,
            DiscountAmount = invoice.DiscountAmount,
            TaxPercent = invoice.TaxPercent,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            AmountDue = invoice.AmountDue,
            PaymentMode = invoice.PaymentMode,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Items = items.Select(i => new InvoiceItemResponse
            {
                Id = i.Id.ToString(),
                TestCode = i.TestCode,
                TestName = i.TestName,
                Quantity = i.Quantity,
                Rate = i.Rate,
                Amount = i.Amount
            }).ToList(),
            CreatedAt = invoice.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private static ReagentResponse MapToReagentResponse(ReagentInventory reagent)
    {
        return new ReagentResponse
        {
            Id = reagent.Id.ToString(),
            ReagentName = reagent.ReagentName,
            Manufacturer = reagent.Manufacturer,
            CatalogNumber = reagent.CatalogNumber,
            LotNumber = reagent.LotNumber,
            ExpiryDate = reagent.ExpiryDate?.ToString("yyyy-MM-dd"),
            QuantityTotal = reagent.QuantityTotal,
            QuantityRemaining = reagent.QuantityRemaining,
            Unit = reagent.Unit,
            TestsPerUnit = reagent.TestsPerUnit,
            ReorderLevel = reagent.ReorderLevel,
            CostPerUnit = reagent.CostPerUnit,
            InstrumentName = reagent.InstrumentName,
            Status = reagent.Status,
            Notes = reagent.Notes,
            CreatedAt = reagent.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }
}
