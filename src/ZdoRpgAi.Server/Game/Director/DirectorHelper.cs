using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Server.TextToSpeech;
using ZdoRpgAi.Server.Util.Mp3;

namespace ZdoRpgAi.Server.Game.Director;

public class DirectorHelper {
    private static readonly ILog Log = Logger.Get<DirectorHelper>();

    private readonly IRpcChannel _rpc;

    public DirectorHelper(IRpcChannel rpc) {
        _rpc = rpc;
    }

    public void PublishNpcSpeaksMp3(string npcId, string text, ITextToSpeechOutput mp3) {
        var duration = Mp3Duration.Estimate(mp3.Mp3Bytes) ?? 0;
        _rpc.Publish(
            nameof(ServerToClientMessageType.NpcSpeaksMp3),
            JsonExtensions.SerializeToObject(
                new NpcSpeaksMp3Payload(npcId, text, duration),
                PayloadJsonContext.Default.NpcSpeaksMp3Payload),
            mp3.Mp3Bytes);
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
