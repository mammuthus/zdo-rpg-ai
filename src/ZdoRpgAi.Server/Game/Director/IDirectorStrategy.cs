using ZdoRpgAi.Server.Game.Story;

namespace ZdoRpgAi.Server.Game.Director;

public interface IDirectorStrategy {
    Task ProcessStoryEventAsync(StoryEvent evt);
}
