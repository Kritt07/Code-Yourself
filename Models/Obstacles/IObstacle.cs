using System.Drawing;

namespace CodeYourself.Models.Obstacles
{
    public enum ObstacleKind
    {
        Saw,
        MovingPlatform
    }

    public interface IObstacle
    {
        ObstacleKind Kind { get; }
        Rectangle Bounds { get; }

        /// <summary>
        /// Обновляет состояние препятствия на тике с индексом <paramref name="tickIndex"/>.
        /// Должно быть детерминированным относительно tickIndex и параметров препятствия.
        /// </summary>
        void Update(int tickIndex);
    }
}

