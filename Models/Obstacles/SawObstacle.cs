using System;
using System.Drawing;

namespace CodeYourself.Models.Obstacles
{
    public sealed class SawObstacle : IObstacle
    {
        private readonly int _minX;
        private readonly int _maxX;
        private readonly int _y;
        private readonly int _size;
        private readonly int _stepPerTick;

        public SawObstacle(int minX, int maxX, int y, int size, int stepPerTick)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            _minX = minX;
            _maxX = maxX;
            _y = y;
            _size = size;
            _stepPerTick = stepPerTick;

            Update(0);
        }

        public ObstacleKind Kind => ObstacleKind.Saw;

        public Rectangle Bounds { get; private set; }

        public void Update(int tickIndex)
        {
            var x = OscillatingMotion.GetXForSimTick(tickIndex, _minX, _maxX, _stepPerTick);
            Bounds = new Rectangle(x, _y, _size, _size);
        }
    }
}

