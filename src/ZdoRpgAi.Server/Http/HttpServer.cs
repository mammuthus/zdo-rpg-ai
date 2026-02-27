using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Server.Bootstrap;

namespace ZdoRpgAi.Server.Http;

public class HttpServer {
    private static readonly ILog Log = Logger.Get<HttpServer>();

    private readonly WebApplication _app;
    private readonly int _maxMessageSize;
    private readonly int _rpcTimeoutMs;
    private RpcChannel? _activeRpc;
    private readonly object _lock = new();

    public event Action<RpcChannel>? OnClientConnected;

    public HttpServer(HttpServerSection config) {
        _maxMessageSize = config.MaxMessageSize;
        _rpcTimeoutMs = config.RpcTimeoutMs;

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://{config.Host}:{config.Port}");
        _app = builder.Build();

        _app.UseWebSockets();

        _app.MapGet("/ping", () => "pong");

        _app.Map("/ws", async context => {
            if (!context.WebSockets.IsWebSocketRequest) {
                context.Response.StatusCode = 400;
                return;
            }

            lock (_lock) {
                if (_activeRpc != null) {
                    context.Response.StatusCode = 409;
                    return;
                }
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var channel = new WebSocketChannel(socket, _maxMessageSize);
            var rpc = new RpcChannel(channel, _rpcTimeoutMs);

            lock (_lock) {
                if (_activeRpc != null) {
                    socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "Already connected", CancellationToken.None).Wait();
                    return;
                }
                _activeRpc = rpc;
            }

            rpc.Disconnected += () => {
                lock (_lock) {
                    if (_activeRpc == rpc) {
                        _activeRpc = null;
                    }
                }
            };

            Log.Info("Client connected");
            OnClientConnected?.Invoke(rpc);

            await rpc.RunAsync();
            Log.Info("Client disconnected");
        });
    }

    public async Task StartAsync(CancellationToken ct = default) {
        _app.Lifetime.ApplicationStopping.Register(() => {
            lock (_lock) {
                _activeRpc?.Close();
            }
        });
        await ((IHost)_app).RunAsync(ct);
    }
}
