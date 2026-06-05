using LIS.Hl7Service.DTOs;
using LIS.Hl7Service.Hl7;
using LIS.Hl7Service.Models;
using LIS.Hl7Service.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LIS.Hl7Service.Controllers;

[ApiController]
[Route("hl7")]
public class Hl7Controller : ControllerBase
{
    private readonly IHl7Repository _repository;
    private readonly Hl7ConnectionManager _connectionManager;
    private readonly ILogger<Hl7Controller> _logger;

    public Hl7Controller(
        IHl7Repository repository,
        Hl7ConnectionManager connectionManager,
        ILogger<Hl7Controller> logger)
    {
        _repository = repository;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// GET /hl7/connections — List all connections with live status
    /// </summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
    {
        var labId = GetLabId();
        var connections = await _repository.GetAllConnectionsAsync(labId);
        var statuses = _connectionManager.GetConnectionStatuses();

        var response = connections.Select(c => new ConnectionResponse
        {
            Id = c.Id.ToString(),
            InstrumentName = c.InstrumentName,
            InstrumentModel = c.InstrumentModel,
            Manufacturer = c.Manufacturer,
            Host = c.Host,
            Port = c.Port,
            Protocol = c.Protocol,
            Direction = c.Direction,
            Status = c.Status,
            IsRunning = statuses.ContainsKey(c.Id) && statuses[c.Id],
            LastConnectedAt = c.LastConnectedAt?.ToString("o"),
            LastMessageAt = c.LastMessageAt?.ToString("o")
        });

        return Ok(new { success = true, data = response });
    }

    /// <summary>
    /// POST /hl7/connections — Add new instrument connection
    /// </summary>
    [HttpPost("connections")]
    public async Task<IActionResult> CreateConnection([FromBody] CreateConnectionRequest request)
    {
        var labId = GetLabId();

        var connection = new Hl7Connection
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            InstrumentName = request.InstrumentName,
            InstrumentModel = request.InstrumentModel,
            Manufacturer = request.Manufacturer,
            SerialNumber = request.SerialNumber,
            Host = request.Host,
            Port = request.Port,
            Protocol = request.Protocol,
            Direction = request.Direction,
            Status = "disconnected",
            AutoReconnect = request.AutoReconnect,
            TestCodeMapping = "{}",
            Settings = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.CreateConnectionAsync(connection);
        _logger.LogInformation("Created HL7 connection {Id} for instrument {Name} on port {Port}",
            result.Id, result.InstrumentName, result.Port);

        return Created($"/hl7/connections/{result.Id}", new { success = true, data = new ConnectionResponse
        {
            Id = result.Id.ToString(),
            InstrumentName = result.InstrumentName,
            InstrumentModel = result.InstrumentModel,
            Manufacturer = result.Manufacturer,
            Host = result.Host,
            Port = result.Port,
            Protocol = result.Protocol,
            Direction = result.Direction,
            Status = result.Status,
            IsRunning = false,
            LastConnectedAt = null,
            LastMessageAt = null
        }});
    }

    /// <summary>
    /// GET /hl7/connections/{id} — Get connection detail
    /// </summary>
    [HttpGet("connections/{id}")]
    public async Task<IActionResult> GetConnection(Guid id)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        var statuses = _connectionManager.GetConnectionStatuses();

        return Ok(new { success = true, data = new ConnectionResponse
        {
            Id = connection.Id.ToString(),
            InstrumentName = connection.InstrumentName,
            InstrumentModel = connection.InstrumentModel,
            Manufacturer = connection.Manufacturer,
            Host = connection.Host,
            Port = connection.Port,
            Protocol = connection.Protocol,
            Direction = connection.Direction,
            Status = connection.Status,
            IsRunning = statuses.ContainsKey(connection.Id) && statuses[connection.Id],
            LastConnectedAt = connection.LastConnectedAt?.ToString("o"),
            LastMessageAt = connection.LastMessageAt?.ToString("o")
        }});
    }

    /// <summary>
    /// DELETE /hl7/connections/{id} — Remove connection
    /// </summary>
    [HttpDelete("connections/{id}")]
    public async Task<IActionResult> DeleteConnection(Guid id)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        // Stop the server if running
        _connectionManager.StopServerForConnection(id);

        await _repository.DeleteConnectionAsync(id);
        _logger.LogInformation("Deleted HL7 connection {Id}", id);

        return Ok(new { success = true, message = "Connection deleted" });
    }

    /// <summary>
    /// POST /hl7/connections/{id}/start — Start TCP server for connection
    /// </summary>
    [HttpPost("connections/{id}/start")]
    public async Task<IActionResult> StartConnection(Guid id)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        await _connectionManager.StartServerForConnection(id, connection.Port);
        await _repository.UpdateConnectionStatusAsync(id, "connected");

        _logger.LogInformation("Started HL7 server for connection {Id} on port {Port}", id, connection.Port);

        return Ok(new { success = true, message = $"Server started on port {connection.Port}" });
    }

    /// <summary>
    /// POST /hl7/connections/{id}/stop — Stop TCP server for connection
    /// </summary>
    [HttpPost("connections/{id}/stop")]
    public async Task<IActionResult> StopConnection(Guid id)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        _connectionManager.StopServerForConnection(id);
        await _repository.UpdateConnectionStatusAsync(id, "disconnected");

        _logger.LogInformation("Stopped HL7 server for connection {Id}", id);

        return Ok(new { success = true, message = "Server stopped" });
    }

    /// <summary>
    /// GET /hl7/connections/{id}/logs — Get message logs (paginated)
    /// </summary>
    [HttpGet("connections/{id}/logs")]
    public async Task<IActionResult> GetMessageLogs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        var logs = await _repository.GetMessageLogsAsync(id, page, pageSize);
        var totalCount = await _repository.GetMessageLogCountAsync(id);

        var response = logs.Select(l => new MessageLogResponse
        {
            Id = l.Id.ToString(),
            Direction = l.Direction,
            MessageType = l.MessageType,
            TriggerEvent = l.TriggerEvent,
            Status = l.Status,
            ErrorMessage = l.ErrorMessage,
            ProcessingTimeMs = l.ProcessingTimeMs,
            CreatedAt = l.CreatedAt.ToString("o")
        });

        return Ok(new { success = true, data = response, total = totalCount, page, pageSize });
    }

    /// <summary>
    /// GET /hl7/connections/{id}/logs/{logId} — Get single log with raw message
    /// </summary>
    [HttpGet("connections/{id}/logs/{logId}")]
    public async Task<IActionResult> GetMessageLogDetail(Guid id, Guid logId)
    {
        var log = await _repository.GetMessageLogByIdAsync(logId);
        if (log == null || log.ConnectionId != id)
            return NotFound(new { success = false, error = "Log entry not found" });

        return Ok(new { success = true, data = new MessageLogDetailResponse
        {
            Id = log.Id.ToString(),
            Direction = log.Direction,
            MessageType = log.MessageType,
            TriggerEvent = log.TriggerEvent,
            ControlId = log.ControlId,
            RawMessage = log.RawMessage,
            ParsedJson = log.ParsedJson,
            Status = log.Status,
            ErrorMessage = log.ErrorMessage,
            ProcessingTimeMs = log.ProcessingTimeMs,
            CreatedAt = log.CreatedAt.ToString("o")
        }});
    }

    /// <summary>
    /// GET /hl7/connections/{id}/mappings — Get test code mappings
    /// </summary>
    [HttpGet("connections/{id}/mappings")]
    public async Task<IActionResult> GetMappings(Guid id)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        var mappings = await _repository.GetMappingsAsync(id);

        var response = mappings.Select(m => new MappingResponse
        {
            Id = m.Id.ToString(),
            InstrumentTestCode = m.InstrumentTestCode,
            InstrumentTestName = m.InstrumentTestName,
            LisTestCode = m.LisTestCode,
            LisTestName = m.LisTestName,
            IsActive = m.IsActive
        });

        return Ok(new { success = true, data = response });
    }

    /// <summary>
    /// POST /hl7/connections/{id}/mappings — Add test code mapping
    /// </summary>
    [HttpPost("connections/{id}/mappings")]
    public async Task<IActionResult> CreateMapping(Guid id, [FromBody] CreateMappingRequest request)
    {
        var connection = await _repository.GetConnectionByIdAsync(id);
        if (connection == null)
            return NotFound(new { success = false, error = "Connection not found" });

        var mapping = new InstrumentTestMapping
        {
            Id = Guid.NewGuid(),
            ConnectionId = id,
            InstrumentTestCode = request.InstrumentTestCode,
            InstrumentTestName = request.InstrumentTestName,
            LisTestId = request.LisTestId,
            LisTestCode = request.LisTestCode,
            LisTestName = request.LisTestName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repository.CreateMappingAsync(mapping);

        return Created($"/hl7/connections/{id}/mappings", new { success = true, data = new MappingResponse
        {
            Id = result.Id.ToString(),
            InstrumentTestCode = result.InstrumentTestCode,
            InstrumentTestName = result.InstrumentTestName,
            LisTestCode = result.LisTestCode,
            LisTestName = result.LisTestName,
            IsActive = result.IsActive
        }});
    }

    /// <summary>
    /// DELETE /hl7/mappings/{id} — Delete mapping
    /// </summary>
    [HttpDelete("mappings/{id}")]
    public async Task<IActionResult> DeleteMapping(Guid id)
    {
        await _repository.DeleteMappingAsync(id);
        return Ok(new { success = true, message = "Mapping deleted" });
    }

    /// <summary>
    /// GET /hl7/status — Overall HL7 service status (all connections)
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var statuses = _connectionManager.GetConnectionStatuses();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalConnections = statuses.Count,
                runningConnections = statuses.Count(s => s.Value),
                connections = statuses.Select(s => new
                {
                    connectionId = s.Key.ToString(),
                    isRunning = s.Value
                })
            }
        });
    }

    private Guid GetLabId()
    {
        var labIdClaim = HttpContext.Items["LabId"]?.ToString()
                      ?? HttpContext.Items["labId"]?.ToString();
        if (Guid.TryParse(labIdClaim, out var labId))
            return labId;
        // Default lab ID for development (no auth)
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
