namespace LIS.OrderService.DTOs;

public class CreateOrderRequest
{
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public int? PatientAge { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientMobile { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? SourceSystem { get; set; }
    public string Priority { get; set; } = "routine";
    public string? ClinicalNotes { get; set; }
    public List<OrderTestItem> Tests { get; set; } = new();
}

public class OrderTestItem
{
    public Guid TestId { get; set; }
    public string? TestCode { get; set; }
    public string? TestName { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class OrderResponse
{
    public string Id { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? PatientUhid { get; set; }
    public int? PatientAge { get; set; }
    public string? PatientGender { get; set; }
    public string Priority { get; set; } = "routine";
    public string Status { get; set; } = "ordered";
    public string? OrderedByName { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? SourceSystem { get; set; }
    public List<OrderTestResponse> Tests { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class OrderTestResponse
{
    public string Id { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Status { get; set; } = "ordered";
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public string? ResultFlag { get; set; }
}

public class OrderListResponse
{
    public List<OrderResponse> Orders { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class OrderStatsResponse
{
    public int TotalToday { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int StatOrders { get; set; }
}

// --- Add-on Test DTOs ---
public class AddAddonTestRequest
{
    public Guid TestId { get; set; }
    public string? TestCode { get; set; }
    public string? TestName { get; set; }
    public string? Reason { get; set; }
    public bool SpecimenValid { get; set; } = true;
    public string? Notes { get; set; }
}

public class AddonTestResponse
{
    public string Id { get; set; } = string.Empty;
    public string OriginalOrderId { get; set; } = string.Empty;
    public string AddonOrderTestId { get; set; } = string.Empty;
    public string? TestCode { get; set; }
    public string? TestName { get; set; }
    public string? Reason { get; set; }
    public string? RequestedByName { get; set; }
    public bool SpecimenValid { get; set; }
    public string? Notes { get; set; }
    public string RequestedAt { get; set; } = string.Empty;
}

// --- Order History DTOs ---
public class OrderStatusHistoryResponse
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string? Reason { get; set; }
    public string ChangedAt { get; set; } = string.Empty;
}

// --- Reference Lab Sendout DTOs ---
public class CreateSendoutRequest
{
    public Guid OrderTestId { get; set; }
    public string ReferenceLabName { get; set; } = string.Empty;
    public string? ReferenceLabCode { get; set; }
    public string? ExternalAccession { get; set; }
    public DateTime? SentDate { get; set; }
    public int? ExpectedTatDays { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Courier { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSendoutRequest
{
    public string? Status { get; set; }
    public string? ExternalAccession { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public bool? ResultEntered { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Courier { get; set; }
    public string? Notes { get; set; }
}

public class SendoutResponse
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderTestId { get; set; } = string.Empty;
    public string ReferenceLabName { get; set; } = string.Empty;
    public string? ReferenceLabCode { get; set; }
    public string? ExternalAccession { get; set; }
    public string? SentDate { get; set; }
    public int? ExpectedTatDays { get; set; }
    public string? ReceivedDate { get; set; }
    public bool ResultEntered { get; set; }
    public string Status { get; set; } = "pending";
    public string? TrackingNumber { get; set; }
    public string? Courier { get; set; }
    public string? Notes { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
