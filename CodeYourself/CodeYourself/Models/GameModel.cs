using System;
using System.Drawing;
using System.Xml.Serialization;

namespace CodeYourself.Models
{
    public enum MoveDirection
    {
        Left,
        Right
    }

    public class GameModel
    {
        public const int CanvasWidth = 800;
        public const int CanvasHeight = 400;

        private const int GroundY = 300;
        private const int PlayerSize = 20;

        public Player Player { get; set; }
        public int TickCount { get; private set; } = 0;

        public GameModel()
        {
            Player = new Player(50, GroundY - PlayerSize);
        }

        public void Update()
        {
            TickCount++;
        }
    }

    public class Player
    {
        public Point Position { get; private set; }
        public int Size { get; private set; }
        public Player(int x, int y)
        {
            Position = new Point(x, y);
            Size = 20;
        }

        public void MovePlayer(MoveDirection direction)
        {
            if (direction == MoveDirection.Left)
                Position = new Point(Position.X - 10, Position.Y);
            else if (direction == MoveDirection.Right)
                Position = new Point(Position.X + 10, Position.Y);
        }
    }
}