using System.Drawing;

namespace CodeYourself.Models
{
    public class GameModel
    {
        // ФИКСИРОВАННЫЕ размеры игрового поля (логические координаты)
        public const int CanvasWidth = 800;   // ← увеличил до 800, чтобы было комфортнее
        public const int CanvasHeight = 400;

        public Point PlayerPosition { get; private set; }
        public int TickCount { get; private set; } = 0;

        private const int PlayerSize = 50;
        private const int GroundY = 300; // относительно CanvasHeight

        public GameModel()
        {
            PlayerPosition = new Point(100, GroundY - PlayerSize);
        }

        public void Update()
        {
            TickCount++;
            int newX = PlayerPosition.X + 20;

            // Цикл по фиксированному полю
            if (newX > CanvasWidth - PlayerSize)
                newX = 50;

            PlayerPosition = new Point(newX, PlayerPosition.Y);
        }

        public void Reset()
        {
            PlayerPosition = new Point(50, GroundY - PlayerSize);
            TickCount = 0;
        }
    }
}