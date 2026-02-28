namespace ZdoRpgAi.Server.TextToSpeech.Dummy;

public class DummyTextToSpeech : ITextToSpeech {
    public Task<ITextToSpeechOutput> GenerateAsync(ITextToSpeechInput input) =>
        Task.FromResult(new ITextToSpeechOutput { Mp3Bytes = new byte[128] });
}
