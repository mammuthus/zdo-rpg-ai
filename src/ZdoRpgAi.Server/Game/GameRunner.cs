using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Repository;
using ZdoRpgAi.Server.Game.Listeners;
using ZdoRpgAi.Server.Llm;
using ZdoRpgAi.Server.Lua;
using ZdoRpgAi.Server.SpeechToText;
using ZdoRpgAi.Server.TextToSpeech;

namespace ZdoRpgAi.Server.Game;

public class GameRunner {
    private static readonly ILog Log = Logger.Get<GameRunner>();

    private readonly IMainRepository _mainRepo;
    private readonly ISaveGameRepository _saveGameRepo;
    private readonly ITextToSpeech _tts;
    private readonly ISpeechToText _stt;
    private readonly ILlm _llm;
    private readonly LuaSandbox _lua;
    private readonly IGameMessageListener[] _listeners;

    public GameRunner(
        IMainRepository mainRepo, ISaveGameRepository saveGameRepo,
        ITextToSpeech tts, ISpeechToText stt, ILlm llm, LuaSandbox lua) {
        _mainRepo = mainRepo;
        _saveGameRepo = saveGameRepo;
        _tts = tts;
        _stt = stt;
        _llm = llm;
        _lua = lua;
        _listeners = [
            new PlayerMessageListener(saveGameRepo)
        ];
    }

    public void SetActiveClient(IRpcChannel? rpc) {
        if (rpc != null) {
            foreach (var listener in _listeners)
                listener.OnClientConnected(rpc);
        }
        else {
            foreach (var listener in _listeners)
                listener.OnClientDisconnected();
        }
    }
}
