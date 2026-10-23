using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FloatView.App.Models;
using FloatView.App.Persistence;

namespace FloatView.App.Media;

public class BrowserBridgeService : IDisposable
{
    public const string PipeName = "FloatViewMediaBridge";
    public const int DefaultPort = 48123;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpListener? _httpListener;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<int, ConcurrentQueue<string>> _pendingCommands = new();
    private bool _disposed = false;
    private int _actualPort = DefaultPort;

    public int ListeningPort => _actualPort;
    public bool IsHttpListening => _httpListener?.IsListening == true;

    public event Action<MediaCandidate, MediaEventMessage>? MediaEventReceived;
    public event Action<int, string>? MediaEndedReceived;

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        StartHttpBridge(_cts.Token);
        StartNamedPipeBridge(_cts.Token);
    }

    private void StartHttpBridge(CancellationToken ct)
    {
        int[] candidatePorts = { DefaultPort, 48124, 48125 };
        foreach (int port in candidatePorts)
        {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();
                _httpListener = listener;
                _actualPort = port;
                SettingsService.Log($"BrowserBridge HTTP listener started on http://127.0.0.1:{port}/");
                break;
            }
            catch (Exception ex)
            {
                SettingsService.Log($"Could not bind HTTP bridge port {port}: {ex.Message}");
            }
        }

        if (_httpListener == null || !_httpListener.IsListening)
        {
            SettingsService.Log("Warning: BrowserBridge HTTP listener could not be started on candidate ports.");
            return;
        }

        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested && _httpListener?.IsListening == true)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = HandleHttpRequestAsync(context);
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested) break;
                    SettingsService.Log($"HttpListener error: {ex.Message}");
                }
            }
        }, ct);
    }

    private async Task HandleHttpRequestAsync(HttpListenerContext context)
    {
        try
        {
            var req = context.Request;
            var res = context.Response;

            // CORS headers for browser extensions
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string path = req.Url?.AbsolutePath ?? string.Empty;

            if (path.Equals("/api/media/event", StringComparison.OrdinalIgnoreCase) && req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string json = await reader.ReadToEndAsync();

                ProcessRawMessage(json, out string responseJson);

                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                res.ContentType = "application/json";
                res.ContentLength64 = buffer.Length;
                await res.OutputStream.WriteAsync(buffer);
                res.Close();
                return;
            }

            if (path.Equals("/api/media/commands", StringComparison.OrdinalIgnoreCase))
            {
                // Check if any pending command exists for this tab
                int tabId = -1;
                if (int.TryParse(req.QueryString["tabId"], out int tid)) tabId = tid;

                string? cmd = null;
                if (tabId != -1 && _pendingCommands.TryGetValue(tabId, out var queue) && queue.TryDequeue(out var pendingCmd))
                {
                    cmd = pendingCmd;
                }

                var respObj = new { command = cmd ?? "NONE", tabId };
                string json = JsonSerializer.Serialize(respObj);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json";
                res.ContentLength64 = buffer.Length;
                await res.OutputStream.WriteAsync(buffer);
                res.Close();
                return;
            }

            // Health check
            if (path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
            {
                byte[] buffer = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"service\":\"FloatView.BrowserBridge\"}");
                res.ContentType = "application/json";
                res.ContentLength64 = buffer.Length;
                await res.OutputStream.WriteAsync(buffer);
                res.Close();
                return;
            }

            res.StatusCode = 404;
            res.Close();
        }
        catch (Exception ex)
        {
            SettingsService.Log($"HandleHttpRequest error: {ex.Message}");
        }
    }

    private void StartNamedPipeBridge(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(ct);

                    _ = HandlePipeConnectionAsync(server, ct);
                }
                catch (OperationCanceledException)
                {
                    server?.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    server?.Dispose();
                    SettingsService.Log($"NamedPipeServer error: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
        }, ct);
    }

    private async Task HandlePipeConnectionAsync(NamedPipeServerStream server, CancellationToken ct)
    {
        using (server)
        using (var reader = new StreamReader(server, Encoding.UTF8))
        using (var writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true })
        {
            while (!ct.IsCancellationRequested && server.IsConnected)
            {
                string? line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) break;

                ProcessRawMessage(line, out string responseJson);
                await writer.WriteLineAsync(responseJson.AsMemory(), ct);
            }
        }
    }

    public void ProcessRawMessage(string json, out string responseJson)
    {
        responseJson = "{\"status\":\"ok\"}";
        try
        {
            var msg = JsonSerializer.Deserialize<MediaEventMessage>(json, JsonOpts);
            if (msg == null || string.IsNullOrWhiteSpace(msg.Type)) return;

            if (msg.Type.Equals("MEDIA_ENDED", StringComparison.OrdinalIgnoreCase))
            {
                MediaEndedReceived?.Invoke(msg.TabId, msg.Url);
                return;
            }

            string domain = msg.Domain;
            if (string.IsNullOrWhiteSpace(domain) && Uri.TryCreate(msg.Url, UriKind.Absolute, out var uri))
            {
                domain = uri.Host;
            }

            string browserName = !string.IsNullOrWhiteSpace(msg.BrowserName) ? msg.BrowserName : "External Browser";

            var candidate = new MediaCandidate
            {
                Id = $"ext:{msg.TabId}:{Math.Abs(msg.Url.GetHashCode())}",
                Title = string.IsNullOrWhiteSpace(msg.Title) ? "Browser Media" : msg.Title,
                Url = msg.Url,
                Domain = domain,
                BrowserName = browserName,
                TabId = msg.TabId,
                CurrentTime = msg.CurrentTime,
                Duration = msg.Duration,
                IsPlaying = msg.IsPlaying,
                SourceType = MediaSourceType.Extension,
                DetectedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };

            MediaEventReceived?.Invoke(candidate, msg);

            // If there's a pending command for this tab, return it in response
            if (msg.TabId != -1 && _pendingCommands.TryGetValue(msg.TabId, out var queue) && queue.TryDequeue(out var pendingCmd))
            {
                responseJson = JsonSerializer.Serialize(new { status = "ok", command = pendingCmd, tabId = msg.TabId });
            }
        }
        catch (Exception ex)
        {
            SettingsService.Log($"ProcessRawMessage error: {ex.Message}");
            responseJson = JsonSerializer.Serialize(new { status = "error", error = ex.Message });
        }
    }

    public void SendPauseCommand(int tabId, string url)
    {
        if (tabId <= 0) return;
        var queue = _pendingCommands.GetOrAdd(tabId, _ => new ConcurrentQueue<string>());
        queue.Enqueue("PAUSE");
        SettingsService.Log($"Queued PAUSE command for Tab ID {tabId} ({url})");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        try
        {
            _httpListener?.Stop();
            _httpListener?.Close();
        }
        catch { }
        _cts?.Dispose();
    }
}
