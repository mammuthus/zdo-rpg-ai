using System.Text;
using System.Text.Json.Nodes;
using ZdoRpgAi.Core;

namespace ZdoRpgAi.Server.TextToSpeech.ElevenLabs;

public class ElevenLabsTextToSpeech : ITextToSpeech {
    private static readonly ILog Log = Logger.Get<ElevenLabsTextToSpeech>();

    private readonly HttpClient _http;
    private readonly string _model;
    private readonly double _stability;
    private readonly double _similarityBoost;
    private readonly double _style;
    private readonly bool _useSpeakerBoost;

    public ElevenLabsTextToSpeech(ElevenLabsConfig config) {
        _model = config.Model;
        _stability = config.Stability;
        _similarityBoost = config.SimilarityBoost;
        _style = config.Style;
        _useSpeakerBoost = config.UseSpeakerBoost;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("xi-api-key", config.ApiKey);
    }

    public async Task<Mp3Data> GenerateAsync(string text, IVoiceInfo voiceInfo) {
        if (voiceInfo is not ElevenLabsVoiceInfo elevenVoice) {
            throw new ArgumentException($"Expected {nameof(ElevenLabsVoiceInfo)}, got {voiceInfo.GetType().Name}");
        }

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{elevenVoice.VoiceId}";

        var body = new JsonObject {
            ["text"] = text,
            ["model_id"] = _model,
            ["voice_settings"] = new JsonObject {
                ["stability"] = _stability,
                ["similarity_boost"] = _similarityBoost,
                ["style"] = _style,
                ["use_speaker_boost"] = _useSpeakerBoost,
            },
        };

        var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        Log.Debug("Synthesizing {Length} chars with voice {VoiceId}", text.Length, elevenVoice.VoiceId);

        var resp = await _http.PostAsync(url, content);

        if (!resp.IsSuccessStatusCode) {
            var error = await resp.Content.ReadAsStringAsync();
            Log.Error("API error {StatusCode}: {Response}", resp.StatusCode, error);
            throw new Exception($"ElevenLabs API error {resp.StatusCode}: {error}");
        }

        var audio = await resp.Content.ReadAsByteArrayAsync();
        Log.Debug("Received {Size} bytes of audio", audio.Length);
        return new Mp3Data(audio);
    }
}
