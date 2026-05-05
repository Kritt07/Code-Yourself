using System.Drawing;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Levels
{
    public sealed class TutorialJumpLevel : IGameLevel
    {
        public string Name => "Tutorial 2: JUMP";

        public void Apply(GameModel model)
        {
            model.ClearObstacles();

            int PxX(int xCells) => Grid.XCellsToPx(xCells);

            // Идея: на земле стоят шипы шириной 1 клетка — нужно перепрыгнуть JUMP RIGHT 1.
            const int spikesInset = 5;
            const int spikesHeightPx = 18;
            int spikesY = GameModel.GroundY - spikesHeightPx;

            int spikesX = PxX(7) + spikesInset;
            int spikesW = (1 * Grid.CellSizePx) - (spikesInset * 2);
            model.AddObstacle(new SpikesObstacle(
                x: spikesX,
                y: spikesY,
                width: spikesW,
                height: spikesHeightPx));

            // Финиш сразу после шипов.
            model.SetFinishZone(new Rectangle(
                x: PxX(10),
                y: GameModel.GroundY - 120,
                width: 60,
                height: 120));
        }
    }
}

