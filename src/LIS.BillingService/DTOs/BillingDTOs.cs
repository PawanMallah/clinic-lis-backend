namespace LIS.BillingService.DTOs;

public class CreateInvoiceRequest
{
    public Guid? OrderId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public string? ReferredBy { get; set; }
    public string PricingTier { get; set; } = "walk_in";
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public string PaymentMode { get; set; } = "cash";
    public decimal AmountPaid { get; set; }
    public string? Notes { get; set; }
}

public class InvoiceItemRequest
{
    public Guid? TestId { get; set; }
    public string? TestCode { get; set; }
    public string TestName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
}

public class RecordPaymentRequest
{
    public decimal AmountPaid { get; set; }
    public string PaymentMode { get; set; } = "cash";
}

public class InvoiceResponse
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public string? ReferredBy { get; set; }
    public string PricingTier { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<InvoiceItemResponse> Items { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class InvoiceItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string? TestCode { get; set; }
    public string TestName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceListResponse
{
    public List<InvoiceResponse> Invoices { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class BillingStatsResponse
{
    public decimal TodayRevenue { get; set; }
    public int PendingBills { get; set; }
    public int TotalInvoices { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public class CreateReagentRequest
{
    public string ReagentName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? CatalogNumber { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityTotal { get; set; }
    public string Unit { get; set; } = "ml";
    public int? TestsPerUnit { get; set; }
    public decimal ReorderLevel { get; set; } = 10;
    public decimal? CostPerUnit { get; set; }
    public string? InstrumentName { get; set; }
    public string? Notes { get; set; }
}

public class UpdateReagentRequest
{
    public decimal? QuantityRemaining { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class ConsumeReagentRequest
{
    public decimal QuantityUsed { get; set; }
    public string? TestName { get; set; }
    public Guid? OrderId { get; set; }
    public string? Notes { get; set; }
}

public class ReagentResponse
{
    public string Id { get; set; } = string.Empty;
    public string ReagentName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? CatalogNumber { get; set; }
    public string? LotNumber { get; set; }
    public string? ExpiryDate { get; set; }
    public decimal QuantityTotal { get; set; }
    public decimal QuantityRemaining { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int? TestsPerUnit { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal? CostPerUnit { get; set; }
    public string? InstrumentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class ReagentListResponse
{
    public List<ReagentResponse> Reagents { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class InventoryAlertResponse
{
    public string Id { get; set; } = string.Empty;
    public string ReagentName { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal QuantityRemaining { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? ExpiryDate { get; set; }
    public string AlertType { get; set; } = string.Empty;
}
