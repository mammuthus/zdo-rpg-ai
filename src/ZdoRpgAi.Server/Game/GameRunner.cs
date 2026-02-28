using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Repository;
using ZdoRpgAi.Server.Game.Director;
using ZdoRpgAi.Server.Llm;
using ZdoRpgAi.Server.Lua;
using ZdoRpgAi.Server.SpeechToText;
using ZdoRpgAi.Server.Game.Story;
using ZdoRpgAi.Server.TextToSpeech;

namespace ZdoRpgAi.Server.Game;

public class GameRunner {
    private static readonly ILog Log = Logger.Get<GameRunner>();

    private readonly IMainRepository _mainRepo;
    private readonly ISaveGameRepository _saveGameRepo;
    private readonly ITextToSpeech _tts;
    private readonly ISpeechToText _stt;
    private readonly LuaSandbox _lua;
    private readonly PlayerMessageHandler _playerHandler;
    private readonly StoryComposer _storyComposer;
    private readonly Director.Director _director;

    public GameRunner(
        IMainRepository mainRepo, ISaveGameRepository saveGameRepo,
        ITextToSpeech tts, ISpeechToText stt, ILlm mainLlm, ILlm simpleLlm, LuaSandbox lua) {
        _mainRepo = mainRepo;
        _saveGameRepo = saveGameRepo;
        _tts = tts;
        _stt = stt;
        _lua = lua;

        var story = new Story.Story(saveGameRepo);
        _playerHandler = new PlayerMessageHandler(saveGameRepo, stt);
        _storyComposer = new StoryComposer(story);
        _director = new Director.Director(story, mainLlm, simpleLlm);

        _playerHandler.PlayerSpoke += _storyComposer.OnPlayerSpeak;
    }

    public void SetActiveClient(IRpcChannel? rpc) {
        if (rpc != null) {
            _playerHandler.OnClientConnected(rpc);
            _storyComposer.OnClientConnected(rpc);
            _director.SetClient(rpc);
        }
        else {
            _playerHandler.OnClientDisconnected();
            _storyComposer.OnClientDisconnected();
            _director.SetClient(null);
        }
    }
}
