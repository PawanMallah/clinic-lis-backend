using LIS.OrderService.DTOs;
using LIS.OrderService.Models;
using LIS.OrderService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace LIS.OrderService.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderRepository orderRepository, ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    private Guid GetLabId()
    {
        var labIdStr = HttpContext.Items["labId"]?.ToString();
        if (!string.IsNullOrEmpty(labIdStr) && Guid.TryParse(labIdStr, out var labId))
            return labId;
        // Default lab ID for development (no auth)
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private (Guid? userId, string? name) GetUserInfo()
    {
        var userIdStr = HttpContext.Items["userId"]?.ToString();
        var name = HttpContext.Items["name"]?.ToString() ?? "Dev User";
        Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
        return (userId, name);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("=== LIS: Received new lab order ===");
        _logger.LogInformation("Patient: {PatientName} ({PatientUhid}), Tests: {TestCount}, Source: {Source}, Priority: {Priority}",
            request.PatientName, request.PatientUhid, request.Tests.Count, request.SourceSystem, request.Priority);

        if (request.Tests.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("At least one test is required"));

        var labId = GetLabId();
        var (userId, userName) = GetUserInfo();

        var order = new LabOrder
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            PatientUhid = request.PatientUhid,
            PatientAge = request.PatientAge,
            PatientGender = request.PatientGender,
            PatientMobile = request.PatientMobile,
            ExternalOrderId = request.ExternalOrderId,
            SourceSystem = request.SourceSystem ?? "manual",
            Priority = request.Priority,
            Status = "ordered",
            OrderedBy = userId,
            OrderedByName = userName,
            ClinicalNotes = request.ClinicalNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _orderRepository.CreateAsync(order);

        var tests = request.Tests.Select(t => new LabOrderTest
        {
            Id = Guid.NewGuid(),
            OrderId = created.Id,
            TestId = t.TestId,
            TestCode = t.TestCode ?? string.Empty,
            TestName = t.TestName ?? string.Empty,
            Status = "ordered",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _orderRepository.SaveOrderTestsAsync(created.Id, tests);

        _logger.LogInformation("=== LIS: Order created successfully! ID: {OrderId}, Tests: {Tests} ===",
            created.Id, string.Join(", ", tests.Select(t => t.TestName)));

        var response = MapToResponse(created, tests);
        return Created($"/orders/{created.Id}", ApiResponse<OrderResponse>.Ok(response, "Order created successfully"));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var labId = GetLabId();

        var orders = await _orderRepository.GetAllAsync(labId, status, priority, search, page, pageSize);
        var total = await _orderRepository.GetCountAsync(labId, status, priority, search);

        var orderResponses = new List<OrderResponse>();
        foreach (var order in orders)
        {
            var tests = await _orderRepository.GetOrderTestsAsync(order.Id);
            orderResponses.Add(MapToResponse(order, tests));
        }

        var result = new OrderListResponse
        {
            Orders = orderResponses,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<OrderListResponse>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        var tests = await _orderRepository.GetOrderTestsAsync(id);
        var response = MapToResponse(order, tests);

        return Ok(ApiResponse<OrderResponse>.Ok(response));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(ApiResponse<object>.Fail("Status is required"));

        var validStatuses = new HashSet<string>
        {
            "ordered", "received_by_lab", "collecting", "specimen_collected", "specimen_received",
            "in_process", "preliminary", "final", "corrected", "cancelled",
            "pending", "collected", "received", "in_progress", "completed", "verified", "reported", "rejected"
        };

        if (!validStatuses.Contains(request.Status))
            return BadRequest(ApiResponse<object>.Fail($"Invalid status: {request.Status}"));

        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        if (order.Status == "cancelled")
            return BadRequest(ApiResponse<object>.Fail("Cannot update a cancelled order"));

        if (order.Status == "final" && request.Status != "corrected" && request.Status != "cancelled")
            return BadRequest(ApiResponse<object>.Fail("Final orders can only be corrected or cancelled"));

        var (userId, userName) = GetUserInfo();
        await _orderRepository.UpdateStatusWithHistoryAsync(id, request.Status, userId, userName, request.Reason);

        return Ok(ApiResponse<object>.Ok(new { id = id.ToString(), status = request.Status }, "Order status updated"));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var labId = GetLabId();

        var totalToday = await _orderRepository.GetTodayCountAsync(labId);
        var pending = await _orderRepository.GetPendingCountAsync(labId);
        var completed = await _orderRepository.GetCompletedCountAsync(labId);
        var statOrders = await _orderRepository.GetStatCountAsync(labId);

        var stats = new OrderStatsResponse
        {
            TotalToday = totalToday,
            Pending = pending,
            Completed = completed,
            StatOrders = statOrders
        };

        return Ok(ApiResponse<OrderStatsResponse>.Ok(stats));
    }

    [HttpPost("{id}/addon-tests")]
    public async Task<IActionResult> AddAddonTest(Guid id, [FromBody] AddAddonTestRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        if (order.Status == "cancelled")
            return BadRequest(ApiResponse<object>.Fail("Cannot add tests to a cancelled order"));

        var (userId, userName) = GetUserInfo();

        var test = new LabOrderTest
        {
            Id = Guid.NewGuid(),
            OrderId = id,
            TestId = request.TestId,
            TestCode = request.TestCode ?? string.Empty,
            TestName = request.TestName ?? string.Empty,
            Status = "ordered",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var addon = await _orderRepository.AddAddonTestAsync(id, test, request.Reason, userId, userName, request.SpecimenValid, request.Notes);

        var response = new AddonTestResponse
        {
            Id = addon.Id.ToString(),
            OriginalOrderId = id.ToString(),
            AddonOrderTestId = test.Id.ToString(),
            TestCode = test.TestCode,
            TestName = test.TestName,
            Reason = addon.Reason,
            RequestedByName = addon.RequestedByName,
            SpecimenValid = addon.SpecimenValid,
            Notes = addon.Notes,
            RequestedAt = addon.RequestedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return Created($"/orders/{id}/addon-tests", ApiResponse<AddonTestResponse>.Ok(response, "Add-on test added"));
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetOrderHistory(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        var history = await _orderRepository.GetOrderHistoryAsync(id);

        var response = history.Select(h => new OrderStatusHistoryResponse
        {
            Id = h.Id.ToString(),
            OrderId = h.OrderId.ToString(),
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangedByName = h.ChangedByName,
            Reason = h.Reason,
            ChangedAt = h.ChangedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();

        return Ok(ApiResponse<List<OrderStatusHistoryResponse>>.Ok(response));
    }

    [HttpPost("{orderId}/sendout")]
    public async Task<IActionResult> CreateSendout(Guid orderId, [FromBody] CreateSendoutRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        var labId = GetLabId();

        var sendout = new ReferenceLabSendout
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            OrderId = orderId,
            OrderTestId = request.OrderTestId,
            ReferenceLabName = request.ReferenceLabName,
            ReferenceLabCode = request.ReferenceLabCode,
            ExternalAccession = request.ExternalAccession,
            SentDate = request.SentDate,
            ExpectedTatDays = request.ExpectedTatDays,
            Status = request.SentDate.HasValue ? "sent" : "pending",
            TrackingNumber = request.TrackingNumber,
            Courier = request.Courier,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _orderRepository.CreateSendoutAsync(sendout);

        var response = MapSendoutResponse(created);
        return Created($"/orders/sendouts/{created.Id}", ApiResponse<SendoutResponse>.Ok(response, "Sendout created"));
    }

    [HttpPatch("sendouts/{id}")]
    public async Task<IActionResult> UpdateSendout(Guid id, [FromBody] UpdateSendoutRequest request)
    {
        var existing = await _orderRepository.GetSendoutByIdAsync(id);
        if (existing == null)
            return NotFound(ApiResponse<object>.Fail("Sendout not found"));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validSendoutStatuses = new HashSet<string> { "pending", "sent", "received", "result_entered", "cancelled" };
            if (!validSendoutStatuses.Contains(request.Status))
                return BadRequest(ApiResponse<object>.Fail($"Invalid sendout status: {request.Status}"));
            existing.Status = request.Status;
        }

        if (request.ExternalAccession != null) existing.ExternalAccession = request.ExternalAccession;
        if (request.SentDate.HasValue) existing.SentDate = request.SentDate;
        if (request.ReceivedDate.HasValue) existing.ReceivedDate = request.ReceivedDate;
        if (request.ResultEntered.HasValue) existing.ResultEntered = request.ResultEntered.Value;
        if (request.TrackingNumber != null) existing.TrackingNumber = request.TrackingNumber;
        if (request.Courier != null) existing.Courier = request.Courier;
        if (request.Notes != null) existing.Notes = request.Notes;

        await _orderRepository.UpdateSendoutAsync(id, existing);

        var response = MapSendoutResponse(existing);
        return Ok(ApiResponse<SendoutResponse>.Ok(response, "Sendout updated"));
    }

    [HttpGet("sendouts")]
    public async Task<IActionResult> GetSendouts([FromQuery] string? status)
    {
        var labId = GetLabId();
        var sendouts = await _orderRepository.GetSendoutsAsync(labId, status);

        var response = sendouts.Select(MapSendoutResponse).ToList();
        return Ok(ApiResponse<List<SendoutResponse>>.Ok(response));
    }

    private static OrderResponse MapToResponse(LabOrder order, List<LabOrderTest> tests)
    {
        return new OrderResponse
        {
            Id = order.Id.ToString(),
            PatientName = order.PatientName,
            PatientUhid = order.PatientUhid,
            PatientAge = order.PatientAge,
            PatientGender = order.PatientGender,
            Priority = order.Priority,
            Status = order.Status,
            OrderedByName = order.OrderedByName,
            ClinicalNotes = order.ClinicalNotes,
            ExternalOrderId = order.ExternalOrderId,
            SourceSystem = order.SourceSystem,
            Tests = tests.Select(t => new OrderTestResponse
            {
                Id = t.Id.ToString(),
                TestCode = t.TestCode,
                TestName = t.TestName,
                Status = t.Status,
                ResultValue = t.ResultValue,
                ResultUnit = t.ResultUnit,
                ResultFlag = t.ResultFlag
            }).ToList(),
            CreatedAt = order.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private static SendoutResponse MapSendoutResponse(ReferenceLabSendout s)
    {
        return new SendoutResponse
        {
            Id = s.Id.ToString(),
            OrderId = s.OrderId.ToString(),
            OrderTestId = s.OrderTestId.ToString(),
            ReferenceLabName = s.ReferenceLabName,
            ReferenceLabCode = s.ReferenceLabCode,
            ExternalAccession = s.ExternalAccession,
            SentDate = s.SentDate?.ToString("yyyy-MM-dd"),
            ExpectedTatDays = s.ExpectedTatDays,
            ReceivedDate = s.ReceivedDate?.ToString("yyyy-MM-dd"),
            ResultEntered = s.ResultEntered,
            Status = s.Status,
            TrackingNumber = s.TrackingNumber,
            Courier = s.Courier,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }
}
