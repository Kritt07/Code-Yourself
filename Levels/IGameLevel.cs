using CodeYourself.Models;

namespace CodeYourself.Levels
{
    public interface IGameLevel
    {
        string Name { get; }
        void Apply(GameModel model);
    }
}

