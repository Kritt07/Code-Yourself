using System;
using System.Drawing;

namespace CodeYourself.Models.Obstacles
{
    public sealed class StaticPlatformObstacle : IObstacle
    {
        private readonly int _x;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;

        public StaticPlatformObstacle(int x, int y, int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _x = x;
            _y = y;
            _width = width;
            _height = height;

            Update(0);
        }

        public ObstacleKind Kind => ObstacleKind.StaticPlatform;

        public Rectangle Bounds { get; private set; }

        public void Update(int tickIndex)
        {
            Bounds = new Rectangle(_x, _y, _width, _height);
        }
    }
}

