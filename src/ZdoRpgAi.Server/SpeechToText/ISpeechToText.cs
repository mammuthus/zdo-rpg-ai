namespace ZdoRpgAi.Server.SpeechToText;

public interface ISpeechToText : IDisposable {
    Task StartSessionAsync(CancellationToken ct = default);
    Task FeedAudioAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default);
    Task<string?> FinishSessionAsync(CancellationToken ct = default);

    event Action<string> InterimResultReceived;
}
