namespace LIS.SpecimenService.DTOs;

public class CreateSpecimenRequest
{
    public Guid OrderId { get; set; }
    public string SpecimenType { get; set; } = string.Empty;
    public string? TubeType { get; set; }
    public string? TubeColor { get; set; }
    public decimal? VolumeMl { get; set; }
    public string? Notes { get; set; }
}

public class CollectSpecimenRequest
{
    public string? Notes { get; set; }
}

public class ReceiveSpecimenRequest
{
    public string? Notes { get; set; }
}

public class RejectSpecimenRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class SpecimenResponse
{
    public string Id { get; set; } = string.Empty;
    public string LabId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = string.Empty;
    public string? TubeType { get; set; }
    public string? TubeColor { get; set; }
    public decimal? VolumeMl { get; set; }
    public string Status { get; set; } = "pending";
    public string? CollectedByName { get; set; }
    public string? CollectedAt { get; set; }
    public string? ReceivedByName { get; set; }
    public string? ReceivedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? Notes { get; set; }
    public List<SpecimenTrackingResponse> Tracking { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class SpecimenTrackingResponse
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? PerformedByName { get; set; }
    public string PerformedAt { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class SpecimenListResponse
{
    public List<SpecimenResponse> Specimens { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
