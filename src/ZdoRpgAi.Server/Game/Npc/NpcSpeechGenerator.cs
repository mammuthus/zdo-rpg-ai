using ZdoRpgAi.Core;
using ZdoRpgAi.Server.TextToSpeech;
using ZdoRpgAi.Server.Util.Mp3;

namespace ZdoRpgAi.Server.Game.Npc;

public class NpcSpeechGenerator {
    private static readonly ILog Log = Logger.Get<NpcSpeechGenerator>();

    private readonly ITextToSpeech _tts;
    private readonly Mp3SpeedAdjuster _speedAdjuster;

    public NpcSpeechGenerator(ITextToSpeech tts, Mp3SpeedAdjuster speedAdjuster) {
        _tts = tts;
        _speedAdjuster = speedAdjuster;
    }

    public async Task<ITextToSpeechOutput?> GenerateAsync(NpcInfo npc, string text) {
        try {
            var input = new ITextToSpeechInput {
                npcId = npc.Id,
                npcName = npc.Name,
                npcRace = npc.Race,
                npcSex = npc.Sex,
                text = text,
            };
            var output = await _tts.GenerateAsync(input);

            var duration = Mp3Duration.Estimate(output.Mp3Bytes);
            if (duration != null) {
                output.Mp3Bytes = await _speedAdjuster.AdjustSpeedAsync(output.Mp3Bytes, text, duration.Value);
            }
            else {
                Log.Warn("Could not estimate MP3 duration for NPC {NpcId}, skipping speed adjustment", npc.Id);
            }

            return output;
        }
        catch (Exception ex) {
            Log.Error("Failed to generate speech for NPC {NpcId}: {Error}", npc.Id, ex.Message);
            return null;
        }
    }
}
