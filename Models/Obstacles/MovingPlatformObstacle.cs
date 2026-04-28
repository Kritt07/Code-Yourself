using System;
using System.Drawing;
using CodeYourself.Models;

namespace CodeYourself.Models.Obstacles
{
    public sealed class MovingPlatformObstacle : IObstacle
    {
        private readonly int _minX;
        private readonly int _maxX;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stepPerTick;

        public MovingPlatformObstacle(int minX, int maxX, int y, int width, int height, int stepPerTick)
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

        public ObstacleKind Kind => ObstacleKind.MovingPlatform;

        public Rectangle Bounds { get; private set; }

        public void Update(int tickIndex)
        {
            var x = OscillatingMotion.GetXForSimTick(tickIndex, _minX, _maxX, _stepPerTick);
            Bounds = new Rectangle(x, _y, _width, _height);
        }

        /// <summary>
        /// Возвращает X левого края игрока, выровненного по сетке платформы (клетки <see cref="Grid.CellSizePx"/>,
        /// начало сетки — <see cref="Bounds.Left"/>), с учётом <paramref name="playerCellCenterOffsetX"/>.
        /// </summary>
        public int SnapPlayerXToOwnGrid(int playerX, int playerCellCenterOffsetX)
        {
            var cell = Grid.CellSizePx;
            var platformLeft = Bounds.Left;
            var widthCells = Math.Max(1, Bounds.Width / cell);
            var maxK = widthCells - 1;

            var anchor = playerX - playerCellCenterOffsetX;
            var rel = anchor - platformLeft;
            int k = rel >= 0
                ? (rel + (cell / 2)) / cell
                : -(((-rel) + (cell / 2)) / cell);
            if (k < 0) k = 0;
            if (k > maxK) k = maxK;

            return platformLeft + (k * cell) + playerCellCenterOffsetX;
        }
    }
}

