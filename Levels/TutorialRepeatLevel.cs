using System.Drawing;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Levels
{
    public sealed class TutorialRepeatLevel : IGameLevel
    {
        public string Name => "Tutorial 4: REPEAT";

        public void Apply(GameModel model)
        {
            model.ClearObstacles();

            int PxX(int xCells) => Grid.XCellsToPx(xCells);

            // Идея: повторяющийся узор "пройти клетку + перепрыгнуть шипы".
            // Удобное решение через:
            // REPEAT 3
            //   MOVE RIGHT 1
            //   JUMP RIGHT 1
            // END

            const int spikesInset = 5;
            const int spikesHeightPx = 18;
            int spikesY = GameModel.GroundY - spikesHeightPx;

            // Три одинаковых "барьера" в клетках 4, 6, 8 (между ними есть по клетке для разгона).
            int[] spikeCells = { 4, 6, 8 };
            foreach (var xc in spikeCells)
            {
                int x = PxX(xc) + spikesInset;
                int w = (1 * Grid.CellSizePx) - (spikesInset * 2);
                model.AddObstacle(new SpikesObstacle(x: x, y: spikesY, width: w, height: spikesHeightPx));
            }

            model.SetFinishZone(new Rectangle(
                x: PxX(12),
                y: GameModel.GroundY - 120,
                width: 60,
                height: 120));
        }
    }
}

