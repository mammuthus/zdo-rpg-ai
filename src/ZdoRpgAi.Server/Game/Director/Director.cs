using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Server.Game.Story;
using ZdoRpgAi.Server.Llm;

namespace ZdoRpgAi.Server.Game.Director;

public class Director {
    private static readonly ILog Log = Logger.Get<Director>();

    private readonly List<IDirectorStrategy> _strategies = [];

    public Director(Story.Story story, ILlm mainLlm, ILlm simpleLlm) {
        var simpleReactive = new SimpleReactiveStrategy(mainLlm, simpleLlm, story);
        _strategies.Add(simpleReactive);
        story.EventRegistered += OnStoryEventRegistered;
    }

    public void SetClient(IRpcChannel? client) {
        foreach (var strategy in _strategies) {
            if (strategy is SimpleReactiveStrategy reactive)
                reactive.SetClient(client);
        }
    }

    private void OnStoryEventRegistered(StoryEvent evt) {
        _ = ProcessStoryEventAsync(evt);
    }

    private async Task ProcessStoryEventAsync(StoryEvent evt) {
        var strategy = DetermineStrategy(evt);
        if (strategy == null) return;

        try {
            await strategy.ProcessStoryEventAsync(evt);
        }
        catch (Exception ex) {
            Log.Error("Strategy {Strategy} failed on event #{Id} ({Type}): {Error}",
                strategy.GetType().Name, evt.Id, evt.GetType().Name, ex.Message);
        }
    }

    private IDirectorStrategy? DetermineStrategy(StoryEvent evt) {
        return evt switch {
            StoryEvent.PlayerSpeak => _strategies.OfType<SimpleReactiveStrategy>().FirstOrDefault(),
            _ => null,
        };
    }
}
