using Dapper;
using Microsoft.AspNetCore.Mvc;
using Shared.Database;
using Shared.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LIS.OrderService.Controllers;

[ApiController]
[Route("api/v1")]
public class ExternalApiController : ControllerBase
{
    private readonly DapperContext _db;

    public ExternalApiController(DapperContext db)
    {
        _db = db;
    }

    private Guid GetLabId()
    {
        var labIdStr = HttpContext.Items["labId"]?.ToString();
        if (!string.IsNullOrEmpty(labIdStr) && Guid.TryParse(labIdStr, out var labId))
            return labId;
        // Default lab ID for development (no auth)
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    // POST /api/v1/orders — Create order from external system
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] ExternalCreateOrderRequest request)
    {
        if (request.Tests == null || request.Tests.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("At least one test is required"));

        var labId = GetLabId();

        using var connection = _db.CreateConnection();

        var orderId = Guid.NewGuid();
        await connection.ExecuteAsync(
            @"INSERT INTO lab_orders (id, lab_id, patient_id, patient_name, patient_uhid, patient_age, 
                     patient_gender, patient_mobile, external_order_id, source_system, priority, status, 
                     clinical_notes, created_at, updated_at)
              VALUES (@Id, @LabId, @PatientId, @PatientName, @PatientUhid, @PatientAge, 
                     @PatientGender, @PatientMobile, @ExternalOrderId, @SourceSystem, @Priority::order_priority, 'ordered'::order_status,
                     @ClinicalNotes, NOW(), NOW())",
            new
            {
                Id = orderId,
                LabId = labId,
                request.PatientId,
                request.PatientName,
                request.PatientUhid,
                request.PatientAge,
                request.PatientGender,
                request.PatientMobile,
                request.ExternalOrderId,
                SourceSystem = request.SourceSystem ?? "external_api",
                Priority = request.Priority ?? "routine",
                request.ClinicalNotes
            });

        foreach (var test in request.Tests)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO lab_order_tests (id, order_id, test_id, test_code, test_name, status, created_at, updated_at)
                  VALUES (@Id, @OrderId, @TestId, @TestCode, @TestName, 'ordered'::order_status, NOW(), NOW())",
                new
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    test.TestId,
                    TestCode = test.TestCode ?? string.Empty,
                    TestName = test.TestName ?? string.Empty
                });
        }

        return Created($"/api/v1/orders/{orderId}", ApiResponse<object>.Ok(new
        {
            orderId = orderId.ToString(),
            status = "ordered",
            testsCount = request.Tests.Count
        }, "Order created successfully"));
    }

    // GET /api/v1/orders/{id} — Get order with status
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var labId = GetLabId();
        using var connection = _db.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, patient_name AS PatientName, patient_uhid AS PatientUhid, 
                     priority::text AS Priority, status::text AS Status, external_order_id AS ExternalOrderId, 
                     source_system AS SourceSystem, clinical_notes AS ClinicalNotes,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM lab_orders 
              WHERE id = @Id AND lab_id = @LabId",
            new { Id = id, LabId = labId });

        if (order == null)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        var tests = await connection.QueryAsync<dynamic>(
            @"SELECT id, test_code AS TestCode, test_name AS TestName, status::text AS Status
              FROM lab_order_tests WHERE order_id = @OrderId",
            new { OrderId = id });

        return Ok(ApiResponse<object>.Ok(new
        {
            id = order.id.ToString(),
            patientName = order.PatientName,
            patientUhid = order.PatientUhid,
            priority = order.Priority,
            status = order.Status,
            externalOrderId = order.ExternalOrderId,
            sourceSystem = order.SourceSystem,
            clinicalNotes = order.ClinicalNotes,
            tests = tests.Select(t => new
            {
                id = t.id.ToString(),
                testCode = t.TestCode,
                testName = t.TestName,
                status = t.Status
            }),
            createdAt = order.CreatedAt,
            updatedAt = order.UpdatedAt
        }));
    }

    // GET /api/v1/orders/{id}/results — Get results for order
    [HttpGet("orders/{id}/results")]
    public async Task<IActionResult> GetOrderResults(Guid id)
    {
        var labId = GetLabId();
        using var connection = _db.CreateConnection();

        // Verify order belongs to lab
        var orderExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM lab_orders WHERE id = @Id AND lab_id = @LabId)",
            new { Id = id, LabId = labId });

        if (!orderExists)
            return NotFound(ApiResponse<object>.Fail("Order not found"));

        var results = await connection.QueryAsync<dynamic>(
            @"SELECT r.id, r.test_code AS TestCode, r.test_name AS TestName, 
                     r.parameter_name AS ParameterName, r.result_value AS ResultValue,
                     r.result_unit AS ResultUnit, r.reference_range AS ReferenceRange,
                     r.flag::text AS Flag, r.status AS Status, r.verified_at AS VerifiedAt
              FROM results r 
              WHERE r.order_id = @OrderId",
            new { OrderId = id });

        return Ok(ApiResponse<object>.Ok(new
        {
            orderId = id.ToString(),
            results = results.Select(r => new
            {
                id = r.id.ToString(),
                testCode = r.TestCode,
                testName = r.TestName,
                parameterName = r.ParameterName,
                resultValue = r.ResultValue,
                resultUnit = r.ResultUnit,
                referenceRange = r.ReferenceRange,
                flag = r.Flag,
                status = r.Status,
                verifiedAt = r.VerifiedAt
            })
        }));
    }

    // GET /api/v1/tests/catalog — Get available tests with pricing
    [HttpGet("tests/catalog")]
    public async Task<IActionResult> GetTestCatalog([FromQuery] string? search, [FromQuery] string? category)
    {
        var labId = GetLabId();
        using var connection = _db.CreateConnection();

        var sql = @"SELECT id, test_code AS TestCode, test_name AS TestName, 
                           category, specimen_type AS SpecimenType, 
                           tat_hours AS TatHours, price, is_active AS IsActive
                    FROM test_master 
                    WHERE lab_id = @LabId AND is_active = true";

        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND (test_name ILIKE @Search OR test_code ILIKE @Search)";
        if (!string.IsNullOrWhiteSpace(category))
            sql += " AND category = @Category";

        sql += " ORDER BY test_name";

        var tests = await connection.QueryAsync<dynamic>(sql, new
        {
            LabId = labId,
            Search = $"%{search}%",
            Category = category
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            tests = tests.Select(t => new
            {
                id = t.id.ToString(),
                testCode = t.TestCode,
                testName = t.TestName,
                category = t.category,
                specimenType = t.SpecimenType,
                tatHours = t.TatHours,
                price = t.price
            })
        }));
    }

    // POST /api/v1/patients — Register/update patient
    [HttpPost("patients")]
    public async Task<IActionResult> RegisterPatient([FromBody] ExternalPatientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Patient name is required"));

        var labId = GetLabId();
        using var connection = _db.CreateConnection();

        // Check if patient exists by UHID
        Guid? existingId = null;
        if (!string.IsNullOrWhiteSpace(request.Uhid))
        {
            existingId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM patients WHERE uhid = @Uhid AND lab_id = @LabId",
                new { request.Uhid, LabId = labId });
        }

        if (existingId.HasValue)
        {
            // Update existing patient
            await connection.ExecuteAsync(
                @"UPDATE patients SET name = @Name, age = @Age, gender = @Gender, 
                         mobile = @Mobile, email = @Email, updated_at = NOW()
                  WHERE id = @Id",
                new
                {
                    Id = existingId.Value,
                    request.Name,
                    request.Age,
                    request.Gender,
                    request.Mobile,
                    request.Email
                });

            return Ok(ApiResponse<object>.Ok(new
            {
                patientId = existingId.Value.ToString(),
                action = "updated"
            }, "Patient updated"));
        }
        else
        {
            var patientId = Guid.NewGuid();
            await connection.ExecuteAsync(
                @"INSERT INTO patients (id, lab_id, uhid, name, age, gender, mobile, email, created_at, updated_at)
                  VALUES (@Id, @LabId, @Uhid, @Name, @Age, @Gender, @Mobile, @Email, NOW(), NOW())",
                new
                {
                    Id = patientId,
                    LabId = labId,
                    request.Uhid,
                    request.Name,
                    request.Age,
                    request.Gender,
                    request.Mobile,
                    request.Email
                });

            return Created($"/api/v1/patients/{patientId}", ApiResponse<object>.Ok(new
            {
                patientId = patientId.ToString(),
                action = "created"
            }, "Patient registered"));
        }
    }

    // POST /api/v1/webhooks — Register webhook URL
    [HttpPost("webhooks")]
    public async Task<IActionResult> RegisterWebhook([FromBody] RegisterWebhookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(ApiResponse<object>.Fail("Webhook URL is required"));

        var labId = GetLabId();
        using var connection = _db.CreateConnection();

        var webhookId = Guid.NewGuid();
        var secret = GenerateSecret();

        var events = request.Events?.Count > 0
            ? JsonSerializer.Serialize(request.Events)
            : "[\"result.verified\", \"report.finalized\"]";

        await connection.ExecuteAsync(
            @"INSERT INTO webhooks (id, lab_id, url, events, secret, is_active, created_at)
              VALUES (@Id, @LabId, @Url, @Events::jsonb, @Secret, true, NOW())",
            new
            {
                Id = webhookId,
                LabId = labId,
                request.Url,
                Events = events,
                Secret = secret
            });

        return Created($"/api/v1/webhooks/{webhookId}", ApiResponse<object>.Ok(new
        {
            webhookId = webhookId.ToString(),
            url = request.Url,
            events,
            secret
        }, "Webhook registered"));
    }

    // DELETE /api/v1/webhooks/{id} — Remove webhook
    [HttpDelete("webhooks/{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        var labId = GetLabId();
        using var connection = _db.CreateConnection();

        var deleted = await connection.ExecuteAsync(
            "DELETE FROM webhooks WHERE id = @Id AND lab_id = @LabId",
            new { Id = id, LabId = labId });

        if (deleted == 0)
            return NotFound(ApiResponse<object>.Fail("Webhook not found"));

        return Ok(ApiResponse<object>.Ok(new { deleted = true }, "Webhook removed"));
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// DTOs for External API
public class ExternalCreateOrderRequest
{
    public Guid? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientUhid { get; set; }
    public int? PatientAge { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientMobile { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? SourceSystem { get; set; }
    public string? Priority { get; set; }
    public string? ClinicalNotes { get; set; }
    public List<ExternalTestItem> Tests { get; set; } = new();
}

public class ExternalTestItem
{
    public Guid? TestId { get; set; }
    public string? TestCode { get; set; }
    public string? TestName { get; set; }
}

public class ExternalPatientRequest
{
    public string? Uhid { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
}

public class RegisterWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public List<string>? Events { get; set; }
}
