namespace ZdoRpgAi.Server.TextToSpeech;

public interface ITextToSpeech {
    Task<ITextToSpeechOutput> GenerateAsync(ITextToSpeechInput input);
}

public class ITextToSpeechInput {
    public required string npcId;
    public required string npcName;
    public required string npcSex;
    public required string npcRace;
    public required string text;
}

public class ITextToSpeechOutput {
    public required byte[] Mp3Bytes;
}
