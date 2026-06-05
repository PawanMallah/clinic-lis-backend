namespace LIS.Hl7Service.Hl7;

/// <summary>
/// Manages multiple HL7 TCP server instances (one per instrument connection).
/// Handles start/stop/reconnection for all active connections.
/// </summary>
public class Hl7ConnectionManager : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Hl7ConnectionManager> _logger;
    private readonly Dictionary<Guid, Hl7TcpServer> _servers = new();
    private CancellationTokenSource? _cts;

    public Hl7ConnectionManager(IServiceProvider serviceProvider, ILogger<Hl7ConnectionManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger.LogInformation("HL7 Connection Manager starting...");

        // Load active connections from database and start TCP servers
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repositories.IHl7Repository>();
        var connections = await repo.GetActiveConnectionsAsync();

        foreach (var conn in connections)
        {
            await StartServerForConnection(conn.Id, conn.Port, _cts.Token);
        }

        _logger.LogInformation("HL7 Connection Manager started with {Count} active connections", _servers.Count);
    }

    public async Task StartServerForConnection(Guid connectionId, int port, CancellationToken ct = default)
    {
        if (_servers.ContainsKey(connectionId))
        {
            _logger.LogWarning("Server already running for connection {ConnectionId}", connectionId);
            return;
        }

        var server = new Hl7TcpServer(port, connectionId, HandleIncomingMessage, _logger);
        _servers[connectionId] = server;
        _ = server.StartAsync(ct);
        _logger.LogInformation("Started HL7 server for connection {ConnectionId} on port {Port}", connectionId, port);
    }

    public void StopServerForConnection(Guid connectionId)
    {
        if (_servers.TryGetValue(connectionId, out var server))
        {
            server.Stop();
            _servers.Remove(connectionId);
            _logger.LogInformation("Stopped HL7 server for connection {ConnectionId}", connectionId);
        }
    }

    private async Task HandleIncomingMessage(Hl7Message message, Guid connectionId)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repositories.IHl7Repository>();

        try
        {
            // Log the raw message
            var logEntry = new Models.Hl7MessageLog
            {
                Id = Guid.NewGuid(),
                ConnectionId = connectionId,
                Direction = "inbound",
                MessageType = message.MessageType,
                TriggerEvent = message.TriggerEvent,
                ControlId = message.ControlId,
                RawMessage = message.RawMessage,
                ParsedJson = System.Text.Json.JsonSerializer.Serialize(new { message.MessageType, message.TriggerEvent, segmentCount = message.Segments.Count }),
                Status = "received",
                CreatedAt = DateTime.UtcNow
            };

            // Process based on message type
            if (message.MessageType == "ORU")
            {
                // Result message from instrument
                var resultData = Hl7ResultExtractor.Extract(message);
                // TODO: Route results to ResultService via HTTP call
                logEntry.Status = "processed";
                _logger.LogInformation("Processed ORU message with {Count} results from connection {ConnectionId}",
                    resultData.Results.Count, connectionId);
            }
            else
            {
                logEntry.Status = "received";
                _logger.LogInformation("Received {MessageType}^{TriggerEvent} from connection {ConnectionId}",
                    message.MessageType, message.TriggerEvent, connectionId);
            }

            sw.Stop();
            logEntry.ProcessingTimeMs = (int)sw.ElapsedMilliseconds;

            await repo.LogMessageAsync(logEntry);
            await repo.UpdateLastMessageTimeAsync(connectionId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error handling HL7 message from connection {ConnectionId}", connectionId);

            await repo.LogMessageAsync(new Models.Hl7MessageLog
            {
                Id = Guid.NewGuid(),
                ConnectionId = connectionId,
                Direction = "inbound",
                MessageType = message.MessageType,
                TriggerEvent = message.TriggerEvent,
                ControlId = message.ControlId,
                RawMessage = message.RawMessage,
                Status = "error",
                ErrorMessage = ex.Message,
                ProcessingTimeMs = (int)sw.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public Dictionary<Guid, bool> GetConnectionStatuses()
    {
        return _servers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IsRunning);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HL7 Connection Manager stopping...");
        foreach (var server in _servers.Values)
        {
            server.Stop();
        }
        _servers.Clear();
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var server in _servers.Values)
        {
            server.Dispose();
        }
        _servers.Clear();
        _cts?.Dispose();
    }
}
