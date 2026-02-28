namespace ZdoRpgAi.Repository.Data;

public enum ConversationEntryType {
    Speak,
}

public record ConversationEntry(
    long Id,
    string SpeakerCharacterId,
    string? TargetCharacterId,
    string CreatedAtGameTime,
    string CreatedAtRealTime,
    ConversationEntryType Type,
    object Data,
    string[] ListenerCharacterIds);

public record SpeakEntryData(string Text);
