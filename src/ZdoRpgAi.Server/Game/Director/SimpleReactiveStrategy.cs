using System.Text.Json.Nodes;
using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Server.Game.Story;
using ZdoRpgAi.Server.Llm;

namespace ZdoRpgAi.Server.Game.Director;

public class SimpleReactiveStrategy : IDirectorStrategy {
    private static readonly ILog Log = Logger.Get<SimpleReactiveStrategy>();

    private readonly ILlm _mainLlm;
    private readonly ILlm _simpleLlm;
    private readonly Story.Story _story;
    private IRpcChannel? _client;

    public SimpleReactiveStrategy(ILlm mainLlm, ILlm simpleLlm, Story.Story story) {
        _mainLlm = mainLlm;
        _simpleLlm = simpleLlm;
        _story = story;
    }

    public void SetClient(IRpcChannel? client) {
        _client = client;
    }

    public async Task ProcessStoryEventAsync(StoryEvent evt) {
        if (evt is not StoryEvent.PlayerSpeak playerSpeak) return;
        await HandlePlayerSpeakAsync(playerSpeak);
    }

    private async Task HandlePlayerSpeakAsync(StoryEvent.PlayerSpeak evt) {
        var client = _client;
        if (client == null) {
            Log.Warn("No client connected, cannot react to player speech");
            return;
        }

        try {
            var npcId = evt.TargetCharacterId ?? await DetermineTargetNpcAsync(client, evt);
            if (npcId == null) {
                Log.Warn("Could not determine target NPC for player speech");
                return;
            }

            var npcInfo = await GetNpcInfoAsync(client, npcId);
            var playerInfo = await GetPlayerInfoAsync(client, evt.PlayerCharacterId);

            var response = await GenerateNpcResponseAsync(npcInfo, playerInfo, evt.Text);
            if (response == null) {
                Log.Warn("LLM returned no response for NPC {NpcId}", npcId);
                return;
            }

            var npcSpeak = StoryEvent.Create(new StoryEvent.NpcSpeak {
                NpcCharacterId = npcId,
                TargetCharacterId = evt.PlayerCharacterId,
                GameTime = evt.GameTime,
                Text = response,
            });
            _story.RegisterEvent(npcSpeak);

            client.Publish(
                nameof(ServerToModMessageType.NpcSpeaks),
                JsonExtensions.SerializeToObject(
                    new NpcSpeaksPayload(npcId, response),
                    PayloadJsonContext.Default.NpcSpeaksPayload));

            Log.Info("NPC {NpcId} responds: {Text}", npcId, response);
        }
        catch (Exception ex) {
            Log.Error("Failed to handle player speech: {Error}", ex.Message);
        }
    }

    private async Task<string?> DetermineTargetNpcAsync(IRpcChannel client, StoryEvent.PlayerSpeak evt) {
        var hearResponse = await client.CallAsync(
            nameof(ServerToModMessageType.GetCharactersWhoHear),
            JsonExtensions.SerializeToObject(
                new GetCharactersWhoHearRequestPayload(evt.PlayerCharacterId),
                PayloadJsonContext.Default.GetCharactersWhoHearRequestPayload));

        var payload = hearResponse.Json?.DeserializeSafe(PayloadJsonContext.Default.GetCharactersWhoHearResponsePayload);
        var nearby = payload?.Characters
            .Where(c => c.CharacterId != evt.PlayerCharacterId)
            .OrderBy(c => c.Distance)
            .ToArray() ?? [];

        if (nearby.Length == 0) {
            Log.Debug("No nearby NPCs to respond");
            return null;
        }

        if (nearby.Length == 1) {
            return nearby[0].CharacterId;
        }

        // Use simple LLM to determine who the player is addressing
        var npcInfos = new List<(string Id, GetNpcInfoResponsePayload Info)>();
        foreach (var npc in nearby) {
            var info = await GetNpcInfoAsync(client, npc.CharacterId);
            npcInfos.Add((npc.CharacterId, info));
        }

        var npcList = string.Join("\n", npcInfos.Select((n, i) =>
            $"- {n.Id}: {n.Info.Name} ({n.Info.Race} {n.Info.Sex}), distance: {nearby[i].Distance:F1}"));

        var request = new LlmRequest {
            SystemPrompt = "You are deciding which NPC a player is talking to. " +
                           "Respond with ONLY the character ID of the most likely target. " +
                           "Consider the speech content and NPC proximity. " +
                           "If unsure, pick the closest NPC.",
            Messages = [
                new LlmMessage {
                    Role = LlmRole.User,
                    Text = $"Nearby NPCs:\n{npcList}\n\nPlayer said: \"{evt.Text}\"\n\nWhich NPC ID is the player addressing?",
                },
            ],
        };

        var response = await _simpleLlm.ChatAsync(request);
        var chosenId = response.Text?.Trim();

        if (chosenId != null && npcInfos.Any(n => n.Id == chosenId)) {
            Log.Debug("Simple LLM chose NPC {NpcId} as target", chosenId);
            return chosenId;
        }

        // Fallback to closest NPC
        Log.Debug("Simple LLM response '{Response}' did not match any NPC, falling back to closest", response.Text ?? "");
        return nearby[0].CharacterId;
    }

    private static async Task<GetNpcInfoResponsePayload> GetNpcInfoAsync(IRpcChannel client, string npcId) {
        var response = await client.CallAsync(
            nameof(ServerToModMessageType.GetNpcInfo),
            JsonExtensions.SerializeToObject(
                new GetNpcInfoRequestPayload(npcId),
                PayloadJsonContext.Default.GetNpcInfoRequestPayload));

        return response.Json?.DeserializeSafe(PayloadJsonContext.Default.GetNpcInfoResponsePayload)
               ?? new GetNpcInfoResponsePayload("unknown", "Unknown", "Unknown", "Unknown");
    }

    private static async Task<GetPlayerInfoResponsePayload> GetPlayerInfoAsync(IRpcChannel client, string playerId) {
        var response = await client.CallAsync(
            nameof(ServerToModMessageType.GetPlayerInfo),
            JsonExtensions.SerializeToObject(
                new GetPlayerInfoRequestPayload(playerId),
                PayloadJsonContext.Default.GetPlayerInfoRequestPayload));

        return response.Json?.DeserializeSafe(PayloadJsonContext.Default.GetPlayerInfoResponsePayload)
               ?? new GetPlayerInfoResponsePayload("unknown", "Unknown", "Unknown", "Unknown");
    }

    private async Task<string?> GenerateNpcResponseAsync(
        GetNpcInfoResponsePayload npc, GetPlayerInfoResponsePayload player, string playerText) {
        var request = new LlmRequest {
            SystemPrompt = $"You are {npc.Name}, a {npc.Race} {npc.Sex} NPC in a fantasy RPG. " +
                           $"The player {player.Name} ({player.Race} {player.Sex}) is speaking to you. " +
                           "Respond in character with a single short phrase. " +
                           "Stay in character. Do not use quotation marks.",
            Messages = [
                new LlmMessage {
                    Role = LlmRole.User,
                    Text = playerText,
                },
            ],
        };

        var response = await _mainLlm.ChatAsync(request);
        return response.Text?.Trim();
    }
}
