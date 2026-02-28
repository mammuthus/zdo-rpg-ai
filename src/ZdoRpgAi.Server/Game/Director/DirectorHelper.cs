using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;

namespace ZdoRpgAi.Server.Game.Director;

public class DirectorHelper {
    private static readonly ILog Log = Logger.Get<DirectorHelper>();

    private readonly IRpcChannel _rpc;

    public DirectorHelper(IRpcChannel rpc) {
        _rpc = rpc;
    }

    public async Task<string[]> QueryObserverIdsAsync(string characterId, string[]? excludeIds) {
        try {
            var response = await _rpc.CallAsync(
                nameof(ServerToModMessageType.GetCharactersWhoHear),
                JsonExtensions.SerializeToObject(
                    new GetCharactersWhoHearRequestPayload(characterId),
                    PayloadJsonContext.Default.GetCharactersWhoHearRequestPayload));

            var payload = response.Json?.DeserializeSafe(PayloadJsonContext.Default.GetCharactersWhoHearResponsePayload);
            var characters = payload?.Characters ?? [];

            return characters
                .Select(c => c.CharacterId)
                .Where(id => id != characterId && (excludeIds == null ? true : !excludeIds.Contains(id)))
                .ToArray();
        }
        catch (Exception ex) {
            Log.Warn("Failed to query observers: {Error}", ex.Message);
            return [];
        }
    }
}
