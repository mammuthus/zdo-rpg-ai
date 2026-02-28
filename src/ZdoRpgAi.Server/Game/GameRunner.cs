using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Repository;
using ZdoRpgAi.Server.Llm;
using ZdoRpgAi.Server.Lua;
using ZdoRpgAi.Server.SpeechToText;
using ZdoRpgAi.Server.TextToSpeech;

namespace ZdoRpgAi.Server.Game;

public class GameRunner {
    private readonly IMainRepository _mainRepo;
    private readonly ISaveGameRepository _saveGameRepo;
    private readonly ITextToSpeech _tts;
    private readonly ISpeechToText _stt;
    private readonly ILlm _llm;
    private readonly LuaSandbox _lua;
    private IRpcChannel? _client;

    public GameRunner(
        IMainRepository mainRepo, ISaveGameRepository saveGameRepo,
        ITextToSpeech tts, ISpeechToText stt, ILlm llm, LuaSandbox lua) {
        _mainRepo = mainRepo;
        _saveGameRepo = saveGameRepo;
        _tts = tts;
        _stt = stt;
        _llm = llm;
        _lua = lua;
    }

    public void SetActiveClient(IRpcChannel? rpc) {
        _client = rpc;

        if (rpc != null)
            rpc.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(Message msg) {
    }
}
