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

        public const int GroundHeight = 50;
        public const int GroundY = CanvasHeight - GroundHeight;
        public const int PlayerSize = 50;

        public Player Player { get; set; }
        public int TickCount { get; private set; } = 0;

        private readonly Point _playerStartPosition = new Point(50, GroundY - PlayerSize);

        public GameModel()
        {
            Player = new Player(_playerStartPosition.X, _playerStartPosition.Y, PlayerSize);
        }

        public void Update()
        {
            TickCount++;
        }

        public void Reset()
        {
            TickCount = 0;
            Player.SetPosition(_playerStartPosition.X, _playerStartPosition.Y);
        }

        public void MovePlayer(MoveDirection direction)
        {
            var dx = direction == MoveDirection.Left ? -Player.DefaultStep : Player.DefaultStep;
            var newX = Player.Position.X + dx;
            newX = Math.Max(0, Math.Min(CanvasWidth - Player.Size, newX));
            Player.SetPosition(newX, Player.Position.Y);
        }
    }
}