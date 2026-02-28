using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Server.Game.Npc;
using ZdoRpgAi.Server.Game.Story;
using ZdoRpgAi.Server.Llm;
using ZdoRpgAi.Server.Util.Mp3;

namespace ZdoRpgAi.Server.Game.Director;

public class Director {
    private static readonly ILog Log = Logger.Get<Director>();

    private readonly Story.Story _story;
    private readonly DirectorHelper _directorHelper;
    private readonly NpcSpeechGenerator _npcSpeechGenerator;
    private readonly SimpleReactiveStrategy _simpleReactive;
    private readonly IRpcChannel _rpc;
    private readonly NpcRepository _npcRepo;
    private readonly object _bufferLock = new();
    private readonly List<StoryEvent> _buffer = [];
    private bool _processing;
    private int _playerInterruptionIteration = 0;

    public Director(Story.Story story, DirectorHelper directorHelper, NpcSpeechGenerator npcSpeechGenerator, IRpcChannel rpc, ILlm mainLlm, ILlm simpleLlm, NpcRepository npcRepo) {
        _story = story;
        _directorHelper = directorHelper;
        _npcSpeechGenerator = npcSpeechGenerator;
        _rpc = rpc;
        _npcRepo = npcRepo;
        _simpleReactive = new SimpleReactiveStrategy(mainLlm, simpleLlm, story, npcRepo, rpc);
        story.EventRegistered += OnStoryEventRegistered;
    }

    private void OnStoryEventRegistered(StoryEvent evt) {
        lock (_bufferLock) {
            _buffer.Add(evt);
            if (_processing) {
                return;
            }

            _processing = true;
        }

        _ = DrainBufferAsync();
    }

    private async Task DrainBufferAsync() {
        while (true) {
            List<StoryEvent> batch;
            lock (_bufferLock) {
                if (_buffer.Count == 0) {
                    _processing = false;
                    return;
                }

                batch = [.. _buffer];
                _buffer.Clear();
            }

            if (batch.Count > 0) {
                await ProcessStoryEventsAsync(batch);
            }
        }
    }

    private async Task ProcessStoryEventsAsync(List<StoryEvent> events) {
        Log.Trace("Received {Count} story events", events.Count);

        var strategy = DetermineStrategy(events);
        if (strategy == null) {
            Log.Trace("No strategy decided");
            return;
        }

        try {
            var newEvents = await strategy.ProcessStoryEventsAsync(events);
            await RegisterAndPublishAsync(newEvents);
        }
        catch (Exception ex) {
            Log.Error("Strategy {Strategy} failed: {Error}", strategy.GetType().Name, ex.Message);
        }
    }

    private async Task RegisterAndPublishAsync(List<StoryEvent> events) {
        var observersCache = new Dictionary<string, string[]>();
        StoryEvent.NpcSpeak? npcSpeakEvent = null;

        foreach (var e in events) {
            var (mainCharId, targetCharId) = e switch {
                StoryEvent.PlayerSpeak ps => (ps.PlayerCharacterId, ps.TargetCharacterId),
                StoryEvent.NpcSpeak ns => (ns.NpcCharacterId, ns.TargetCharacterId),
                _ => ((string?)null, (string?)null),
            };

            if (mainCharId == null) {
                _story.RegisterEvent(e, []);
                continue;
            }

            if (!observersCache.TryGetValue(mainCharId, out var observerIds)) {
                observerIds = await _directorHelper.QueryObserverIdsAsync(mainCharId, targetCharId != null ? [targetCharId] : null);
                observersCache[mainCharId] = observerIds;
            }

            _story.RegisterEvent(e, observerIds);

            if (e is StoryEvent.NpcSpeak npcSpeak) {
                Log.Info("NPC {NpcId} speaks: {Text}", npcSpeak.NpcCharacterId, npcSpeak.Text);
                npcSpeakEvent = npcSpeak;
            }
        }

        if (npcSpeakEvent != null) {
            var npc = await _npcRepo.GetNpcInfoAsync(npcSpeakEvent.NpcCharacterId);
            if (npc != null) {
                var iterationBeforeGeneration = _playerInterruptionIteration;
                var mp3 = await _npcSpeechGenerator.GenerateAsync(npc, npcSpeakEvent.Text);
                if (_playerInterruptionIteration == iterationBeforeGeneration && mp3 != null) {
                    _directorHelper.PublishNpcSpeaksMp3(npcSpeakEvent.NpcCharacterId, npcSpeakEvent.Text, mp3);

                    var durationMs = (int)((Mp3Duration.Estimate(mp3.Mp3Bytes) ?? 0) * 1000);
                    if (durationMs > 0) {
                        await WaitUnlessInterruptedAsync(durationMs, iterationBeforeGeneration);
                    }
                }
            }
            else {
                Log.Warn($"Cannot get NPC info id={npcSpeakEvent.NpcCharacterId}");
            }
        }
    }

    private async Task WaitUnlessInterruptedAsync(int durationMs, int expectedIteration) {
        const int pollIntervalMs = 100;
        var remaining = durationMs;
        while (remaining > 0) {
            if (_playerInterruptionIteration != expectedIteration) {
                Log.Debug("Speech playback wait interrupted by player");
                return;
            }

            var delay = Math.Min(remaining, pollIntervalMs);
            await Task.Delay(delay);
            remaining -= delay;
        }
    }

    private IDirectorStrategy? DetermineStrategy(List<StoryEvent> events) {
        if (events.Last() is StoryEvent.PlayerSpeak) {
            return _simpleReactive;
        }

        return null;
    }
}
