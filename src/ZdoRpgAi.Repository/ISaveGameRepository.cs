using ZdoRpgAi.Repository.Data;

namespace ZdoRpgAi.Repository;

public interface ISaveGameRepository : IDisposable {
    long AddConversationEntry(string speakerCharacterId, string? targetCharacterId,
        string createdAtGameTime, ConversationEntryType type, object data, string[] listenerCharacterIds);
}
