namespace ZdoRpgAi.Server.SpeechToText;

public interface ISpeechToText : IDisposable {
    void Start();
    void FeedAudio(ReadOnlyMemory<byte> buffer);
    void Finish();
    void Cancel();

    event Action<string> InterimResultReceived;
    event Action<string> FinalResultReceived;
}
