using System;
using System.Drawing;

namespace CodeYourself.Models.Obstacles
{
    public sealed class SawObstacle : IObstacle
    {
        private readonly int _minX;
        private readonly int _maxX;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stepPerTick;

        public SawObstacle(int minX, int maxX, int y, int size, int stepPerTick)
            : this(minX, maxX, y, width: size, height: size, stepPerTick: stepPerTick)
        {
        }

        public SawObstacle(int minX, int maxX, int y, int width, int height, int stepPerTick)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _minX = minX;
            _maxX = maxX;
            _y = y;
            _width = width;
            _height = height;
            _stepPerTick = stepPerTick;

            Update(0);
        }

        public ObstacleKind Kind => ObstacleKind.Saw;

        public Rectangle Bounds { get; private set; }

        public void Update(int tickIndex)
        {
            var x = OscillatingMotion.GetXForSimTick(tickIndex, _minX, _maxX, _stepPerTick);
            Bounds = new Rectangle(x, _y, _width, _height);
        }
    }
}

