using System;
using System.Drawing;

namespace CodeYourself.Models
{
    public readonly struct InsetsPx
    {
        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }

        public InsetsPx(int left, int top, int right, int bottom)
        {
            if (left < 0) throw new ArgumentOutOfRangeException(nameof(left));
            if (top < 0) throw new ArgumentOutOfRangeException(nameof(top));
            if (right < 0) throw new ArgumentOutOfRangeException(nameof(right));
            if (bottom < 0) throw new ArgumentOutOfRangeException(nameof(bottom));
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    /// <summary>
    /// Сетка уровня: 1 клетка = 50px. Ось X вправо, ось Y — вверх от линии земли.
    /// </summary>
    public static class Grid
    {
        public const int CellSizePx = 50;

        public static int XCellsToPx(int xCells) => xCells * CellSizePx;

        /// <summary>
        /// Возвращает top (в px) для прямоугольника высоты <paramref name="heightPx"/>,
        /// если его НИЗ находится на высоте <paramref name="yCells"/> клеток над землёй.
        /// yCells=0 => низ на линии земли.
        /// </summary>
        public static int TopPxFromBottomOnGroundCells(int yCells, int heightPx)
        {
            return GameModel.GroundY - (yCells * CellSizePx) - heightPx;
        }

        public static Rectangle RectFromCellsBottomAnchored(
            int xCells,
            int yCells,
            int widthCells,
            int heightCells,
            InsetsPx insets)
        {
            if (widthCells <= 0) throw new ArgumentOutOfRangeException(nameof(widthCells));
            if (heightCells <= 0) throw new ArgumentOutOfRangeException(nameof(heightCells));

            var fullWidthPx = widthCells * CellSizePx;
            var fullHeightPx = heightCells * CellSizePx;

            var widthPx = Math.Max(0, fullWidthPx - insets.Left - insets.Right);
            var heightPx = Math.Max(0, fullHeightPx - insets.Top - insets.Bottom);

            var xPx = XCellsToPx(xCells) + insets.Left;
            var topPx = TopPxFromBottomOnGroundCells(yCells, fullHeightPx) + insets.Top;

            return new Rectangle(xPx, topPx, widthPx, heightPx);
        }
    }
}

