using System.Drawing;

namespace CodeYourself.Models
{
    public class GameModel
    {
        public Point PlayerPosition { get; private set; }
        public int TickCount { get; private set; } = 0;

        private const int PlayerSize = 50;
        private const int GroundY = 300; // высота платформы
        private const int CanvasWidth = 600;

        public GameModel()
        {
            PlayerPosition = new Point(100, GroundY - PlayerSize);
        }

        /// <summary>
        /// Простое движение по тику (для недели 1)
        /// </summary>
        public void Update()
        {
            TickCount++;
            // Двигаемся вправо на 20 пикселей за тик
            int newX = PlayerPosition.X + 20;

            // Если дошли до края — возвращаем в начало (цикл для теста)
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