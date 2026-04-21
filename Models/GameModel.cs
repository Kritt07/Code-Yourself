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

        public void JumpPlayerStep(MoveDirection direction, int stepIndex, int totalSteps)
        {
            if (totalSteps <= 0)
                return;

            // Дискретная дуга без физики: шаг по X каждый тик + изменение Y по "параболе".
            var dx = direction == MoveDirection.Left ? -Player.DefaultStep : Player.DefaultStep;

            // Нормализованная позиция t ∈ [0..1]
            var t = totalSteps == 1 ? 1.0 : (double)stepIndex / (totalSteps - 1);

            // Парабола p(t) = 4 t (1-t) ∈ [0..1], максимум в t=0.5
            var p = 4.0 * t * (1.0 - t);

            // Высота прыжка в пикселях (подогнано под сетку шагов)
            var jumpHeight = Player.DefaultStep; // 50px

            // На каждом шаге задаём абсолютную Y относительно земли
            var newY = (int)Math.Round((GroundY - Player.Size) - (jumpHeight * p));

            var newX = Player.Position.X + dx;
            newX = Math.Max(0, Math.Min(CanvasWidth - Player.Size, newX));

            // Ограничиваем Y верхней границей канваса и нижней (земля)
            var groundY = GroundY - Player.Size;
            newY = Math.Max(0, Math.Min(groundY, newY));

            Player.SetPosition(newX, newY);
        }
    }
}