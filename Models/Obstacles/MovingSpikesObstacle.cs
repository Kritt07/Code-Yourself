using System;
using System.Drawing;

namespace CodeYourself.Models.Obstacles
{
    public sealed class MovingSpikesObstacle : IObstacle
    {
        private readonly int _minX;
        private readonly int _maxX;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stepPerTick;
        private readonly int _xOffset;

        public MovingSpikesObstacle(int minX, int maxX, int y, int width, int height, int stepPerTick, int xOffset)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _minX = minX;
            _maxX = maxX;
            _y = y;
            _width = width;
            _height = height;
            _stepPerTick = stepPerTick;
            _xOffset = xOffset;

            Update(0);
        }

        public ObstacleKind Kind => ObstacleKind.Spikes;

        public Rectangle Bounds { get; private set; }

        public void Update(int tickIndex)
        {
            var platformX = OscillatingMotion.GetXForSimTick(tickIndex, _minX, _maxX, _stepPerTick);
            Bounds = new Rectangle(platformX + _xOffset, _y, _width, _height);
        }
    }
}

