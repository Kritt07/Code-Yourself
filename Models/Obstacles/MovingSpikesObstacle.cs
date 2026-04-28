using System;
using System.Drawing;

namespace CodeYourself.Models.Obstacles
{
    /// <summary>
    /// Шипы, привязанные к движущейся платформе с теми же min/max/step, что у <see cref="MovingPlatformObstacle"/>.
    /// Параметр <c>xOffsetFromPlatformLeftPx</c> — смещение левого края шипов вправо от левого края платформы (в мировых px), а не от мировой сетки.
    /// </summary>
    public sealed class MovingSpikesObstacle : IObstacle
    {
        private readonly int _minX;
        private readonly int _maxX;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stepPerTick;
        private readonly int _xOffsetFromPlatformLeftPx;

        public MovingSpikesObstacle(int minX, int maxX, int y, int width, int height, int stepPerTick, int xOffsetFromPlatformLeftPx)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _minX = minX;
            _maxX = maxX;
            _y = y;
            _width = width;
            _height = height;
            _stepPerTick = stepPerTick;
            _xOffsetFromPlatformLeftPx = xOffsetFromPlatformLeftPx;

            Update(0);
        }

        public ObstacleKind Kind => ObstacleKind.Spikes;

        public Rectangle Bounds { get; private set; }

        /// <summary>
        /// Мировой X левого края шипов на тике <paramref name="simTickIndex"/>, если платформа движется с теми же параметрами.
        /// </summary>
        public static int GetWorldLeftXForSimTick(int simTickIndex, int minX, int maxX, int stepPerTick, int xOffsetFromPlatformLeftPx)
        {
            var platformLeft = OscillatingMotion.GetXForSimTick(simTickIndex, minX, maxX, stepPerTick);
            return platformLeft + xOffsetFromPlatformLeftPx;
        }

        public void Update(int tickIndex)
        {
            var x = GetWorldLeftXForSimTick(tickIndex, _minX, _maxX, _stepPerTick, _xOffsetFromPlatformLeftPx);
            Bounds = new Rectangle(x, _y, _width, _height);
        }
    }
}

