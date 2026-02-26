namespace ZdoRpgAi.Server.TextToSpeech;

public interface ITextToSpeech {
    Task<Mp3Data> GenerateAsync(string text, IVoiceInfo voiceInfo);
}
