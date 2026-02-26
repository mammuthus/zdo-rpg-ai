namespace ZdoRpgAi.Server.TextToSpeech.ElevenLabs;

public class ElevenLabsConfig {
    public required string ApiKey { get; init; }
    public string Model { get; init; } = "eleven_multilingual_v2";
    public double Stability { get; init; } = 0.75;
    public double SimilarityBoost { get; init; } = 0.75;
    public double Style { get; init; } = 0.2;
    public bool UseSpeakerBoost { get; init; }
}
