using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LIS.Hl7Service.Hl7;

/// <summary>
/// TCP server that listens for incoming HL7 messages from lab instruments.
/// Uses MLLP (Minimal Lower Layer Protocol) framing.
/// </summary>
public class Hl7TcpServer : IDisposable
{
    private readonly int _port;
    private readonly ILogger _logger;
    private readonly Func<Hl7Message, Guid, Task> _messageHandler;
    private readonly Guid _connectionId;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Port => _port;

    public Hl7TcpServer(int port, Guid connectionId, Func<Hl7Message, Guid, Task> messageHandler, ILogger logger)
    {
        _port = port;
        _connectionId = connectionId;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;
        _logger.LogInformation("HL7 TCP server started on port {Port} for connection {ConnectionId}", _port, _connectionId);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientAsync(client, _cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HL7 TCP server error on port {Port}", _port);
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogInformation("HL7 client connected from {Endpoint} on port {Port}", endpoint, _port);

        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[65536];
            var messageBuffer = new StringBuilder();

            while (!ct.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead == 0) break;

                var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(data);

                // Check for MLLP end block (FS + CR = \x1C\r)
                var content = messageBuffer.ToString();
                while (content.Contains("\x1C"))
                {
                    var endIndex = content.IndexOf("\x1C");
                    var messageContent = content[..endIndex].Replace("\x0B", "");
                    content = content[(endIndex + 2)..]; // Skip \x1C\r
                    messageBuffer.Clear();
                    messageBuffer.Append(content);

                    if (!string.IsNullOrWhiteSpace(messageContent))
                    {
                        try
                        {
                            var parsedMessage = Hl7Parser.Parse(messageContent);
                            await _messageHandler(parsedMessage, _connectionId);

                            // Send ACK
                            var ack = Hl7Parser.BuildAck(parsedMessage, "AA");
                            var ackBytes = Hl7Parser.WrapMllp(ack);
                            await stream.WriteAsync(ackBytes, 0, ackBytes.Length, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing HL7 message on port {Port}", _port);
                            // Send NAK
                            var nak = $"MSH|^~\\&|LIS|LAB|||{DateTime.UtcNow:yyyyMMddHHmmss}||ACK|{Guid.NewGuid():N}|P|2.5\rMSA|AE|ERROR\r";
                            var nakBytes = Hl7Parser.WrapMllp(nak);
                            await stream.WriteAsync(nakBytes, 0, nakBytes.Length, ct);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HL7 client disconnected from port {Port}", _port);
        }
        finally
        {
            client.Close();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _isRunning = false;
        _logger.LogInformation("HL7 TCP server stopped on port {Port}", _port);
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
