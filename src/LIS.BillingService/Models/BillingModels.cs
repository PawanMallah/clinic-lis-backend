namespace LIS.BillingService.Models;

public class LabInvoice
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public string? ReferredBy { get; set; }
    public string PricingTier { get; set; } = "walk_in";
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string PaymentMode { get; set; } = "cash";
    public string Status { get; set; } = "unpaid";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LabInvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? TestId { get; set; }
    public string? TestCode { get; set; }
    public string TestName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

public class ReagentInventory
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public string ReagentName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? CatalogNumber { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityTotal { get; set; }
    public decimal QuantityRemaining { get; set; }
    public string Unit { get; set; } = "ml";
    public int? TestsPerUnit { get; set; }
    public decimal ReorderLevel { get; set; } = 10;
    public decimal? CostPerUnit { get; set; }
    public Guid? InstrumentId { get; set; }
    public string? InstrumentName { get; set; }
    public string Status { get; set; } = "in_stock";
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReagentConsumptionLog
{
    public Guid Id { get; set; }
    public Guid ReagentId { get; set; }
    public decimal QuantityUsed { get; set; }
    public string? TestName { get; set; }
    public Guid? OrderId { get; set; }
    public DateTime ConsumedAt { get; set; }
    public string? Notes { get; set; }
}
