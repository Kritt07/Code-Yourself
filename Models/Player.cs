using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeYourself.Models
{
    public class Player
    {
        public const int DefaultStep = 50;
        public Point Position { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Back-compat: старый код мог ожидать квадрат; используем max(Width,Height).
        public int Size => Math.Max(Width, Height);

        public Player(int x, int y, int width, int height)
        {
            Position = new Point(x, y);
            Width = width;
            Height = height;
        }

        internal void MoveBy(int dx, int dy = 0)
        {
            Position = new Point(Position.X + dx, Position.Y + dy);
        }

        internal void SetPosition(int x, int y)
        {
            Position = new Point(x, y);
        }
    }
}
