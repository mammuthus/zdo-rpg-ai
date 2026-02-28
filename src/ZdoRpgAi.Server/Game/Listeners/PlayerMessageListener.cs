using System.Text.Json;
using System.Text.Json.Nodes;
using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Repository;
using ZdoRpgAi.Repository.Data;

namespace ZdoRpgAi.Server.Game.Listeners;

public class PlayerMessageListener : IGameMessageListener {
    private static readonly ILog Log = Logger.Get<PlayerMessageListener>();

    private readonly ISaveGameRepository _saveGameRepo;
    private IRpcChannel? _client;

    public PlayerMessageListener(ISaveGameRepository saveGameRepo) {
        _saveGameRepo = saveGameRepo;
    }

    public void OnClientConnected(IRpcChannel client) {
        _client = client;
        client.MessageReceived += OnMessageReceived;
    }

    public void OnClientDisconnected() {
        _client = null;
    }

    private void OnMessageReceived(Message msg) {
        switch (msg.Type) {
            case nameof(ClientToServerMessageType.PlayerSpeaksText):
                _ = HandlePlayerSpeaksTextAsync(msg);
                break;
        }
    }

    private async Task HandlePlayerSpeaksTextAsync(Message msg) {
        var payload = msg.Json?.Deserialize(PayloadJsonContext.Default.PlayerSpeaksTextPayload);
        if (payload == null) return;

        var client = _client;
        if (client == null) return;

        Log.Info("Player {PlayerId} speaks: {Text}", payload.PlayerId, payload.Text);

        var response = await client.CallAsync(
            nameof(ServerToModMessageType.GetCharactersWhoHear),
            JsonSerializer.SerializeToNode(
                new GetCharactersWhoHearRequestPayload(payload.PlayerId),
                PayloadJsonContext.Default.GetCharactersWhoHearRequestPayload
            ) as JsonObject);

        var hearResponse = response.Json?.Deserialize(PayloadJsonContext.Default.GetCharactersWhoHearResponsePayload);
        var listeners = hearResponse?.Characters ?? [];

        Log.Info("Player {PlayerId} heard by {Count} characters", payload.PlayerId, listeners.Length);

        var listenerIds = listeners
            .Select(l => l.CharacterId)
            .Where(id => id != payload.PlayerId && id != payload.TargetCharacterId)
            .ToArray();

        _saveGameRepo.AddConversationEntry(
            payload.PlayerId,
            payload.TargetCharacterId,
            payload.GameTime,
            ConversationEntryType.Speak,
            new SpeakEntryData(payload.Text),
            listenerIds);
    }
}
