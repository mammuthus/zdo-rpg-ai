namespace ZdoRpgAi.Server.SpeechToText.Deepgram;

public class DeepgramConfig {
    public required string ApiKey { get; init; }
    public int SampleRate { get; init; } = 16_000;
    public string Encoding { get; init; } = "linear16";
    public string Language { get; init; } = "en";
    public string Model { get; init; } = "nova-2";
}
