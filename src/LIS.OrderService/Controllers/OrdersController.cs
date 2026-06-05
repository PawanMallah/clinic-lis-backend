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

    public OrdersController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    private Guid GetLabId()
    {
        var labIdStr = HttpContext.Items["labId"]?.ToString();
        if (string.IsNullOrEmpty(labIdStr) || !Guid.TryParse(labIdStr, out var labId))
            throw new UnauthorizedAccessException("Lab context not found");
        return labId;
    }

    private (Guid? userId, string? name) GetUserInfo()
    {
        var userIdStr = HttpContext.Items["userId"]?.ToString();
        var name = HttpContext.Items["name"]?.ToString();
        Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : null;
        return (userId, name);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
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

        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        if (order.Status == "cancelled")
            return BadRequest(ApiResponse<object>.Fail("Cannot update a cancelled order"));

        await _orderRepository.UpdateStatusAsync(id, request.Status);

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
}
