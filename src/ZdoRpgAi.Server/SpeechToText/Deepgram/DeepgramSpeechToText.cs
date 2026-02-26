using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ZdoRpgAi.Core;

namespace ZdoRpgAi.Server.SpeechToText.Deepgram;

internal record DeepgramResult(DeepgramChannel Channel, bool IsFinal, bool SpeechFinal);
internal record DeepgramChannel(DeepgramAlternative[] Alternatives);
internal record DeepgramAlternative(string Transcript);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(DeepgramResult))]
internal partial class DeepgramJsonContext : JsonSerializerContext;

public class DeepgramSpeechToText : ISpeechToText {
    private static readonly ILog Log = Logger.Get<DeepgramSpeechToText>();

    private readonly string _apiKey;
    private readonly int _sampleRate;
    private readonly string _encoding;
    private readonly string _language;
    private readonly string _model;
    private ClientWebSocket? _ws;
    private Task? _receiveTask;
    private CancellationTokenSource? _receiveCts;
    private string _accumulatedTranscript = "";
    private readonly object _bufferLock = new();
    private List<byte[]>? _preConnectBuffer;

    public event Action<string>? InterimResultReceived;

    public DeepgramSpeechToText(DeepgramConfig config) {
        _apiKey = config.ApiKey;
        _sampleRate = config.SampleRate;
        _encoding = config.Encoding;
        _language = config.Language;
        _model = config.Model;
    }

    public async Task StartSessionAsync(CancellationToken ct) {
        _accumulatedTranscript = "";

        lock (_bufferLock) {
            _preConnectBuffer = new List<byte[]>();
        }

        _ws = new ClientWebSocket();
        _ws.Options.SetRequestHeader("Authorization", $"Token {_apiKey}");

        var uri = $"wss://api.deepgram.com/v1/listen" +
            $"?encoding={_encoding}" +
            $"&sample_rate={_sampleRate}" +
            $"&language={_language}" +
            $"&model={_model}" +
            $"&interim_results=true" +
            $"&endpointing=300" +
            $"&vad_events=true";

        await _ws.ConnectAsync(new Uri(uri), ct);

        List<byte[]> buffered;
        lock (_bufferLock) {
            buffered = _preConnectBuffer!;
            _preConnectBuffer = null;
        }

        foreach (var buf in buffered) {
            await _ws.SendAsync(buf, WebSocketMessageType.Binary, true, ct);
        }

        Log.Debug("Deepgram session started, flushed {Count} buffered frames", buffered.Count);

        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _receiveTask = RunReceiveLoopAsync(_receiveCts.Token);
    }

    public async Task FeedAudioAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct) {
        lock (_bufferLock) {
            if (_preConnectBuffer != null) {
                _preConnectBuffer.Add(buffer.ToArray());
                return;
            }
        }
        if (_ws is not { State: WebSocketState.Open }) return;
        await _ws.SendAsync(buffer, WebSocketMessageType.Binary, true, ct);
    }

    public async Task<string?> FinishSessionAsync(CancellationToken ct) {
        if (_ws is { State: WebSocketState.Open }) {
            var closeMsg = "{\"type\":\"CloseStream\"}"u8.ToArray();
            try {
                await _ws.SendAsync(closeMsg, WebSocketMessageType.Text, true, ct);
            }
            catch (WebSocketException) { }

            if (_receiveTask != null) {
                using var timeout = new CancellationTokenSource(5000);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
                try {
                    await _receiveTask.WaitAsync(linked.Token);
                }
                catch (OperationCanceledException) { }
            }

            if (_ws.State == WebSocketState.Open) {
                try {
                    await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, ct);
                }
                catch (WebSocketException) { }
            }
        }

        _receiveCts?.Cancel();
        var result = _accumulatedTranscript.Trim();
        Log.Debug("Deepgram session finished: '{Text}'", result);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private async Task RunReceiveLoopAsync(CancellationToken ct) {
        var buffer = new byte[8192];
        try {
            while (_ws is { State: WebSocketState.Open } && !ct.IsCancellationRequested) {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do {
                    result = await _ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text) {
                    var json = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    ProcessMessage(json);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
    }

    private void ProcessMessage(string json) {
        try {
            var node = JsonNode.Parse(json);
            if (node == null) return;

            var type = node["type"]?.GetValue<string>();
            if (type != "Results") return;

            var result = node.Deserialize(DeepgramJsonContext.Default.DeepgramResult);
            if (result?.Channel?.Alternatives is not { Length: > 0 }) return;

            var transcript = result.Channel.Alternatives[0].Transcript;
            if (string.IsNullOrEmpty(transcript)) return;

            if (result.IsFinal) {
                _accumulatedTranscript += " " + transcript;
                InterimResultReceived?.Invoke(_accumulatedTranscript.Trim());
            }
            else {
                InterimResultReceived?.Invoke((_accumulatedTranscript + " " + transcript).Trim());
            }
        }
        catch (Exception ex) {
            Log.Debug("Error parsing Deepgram message: {Error}", ex.Message);
        }
    }

    public void Dispose() {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _ws?.Dispose();
    }
}
