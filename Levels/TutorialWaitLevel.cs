using System.Drawing;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Levels
{
    public sealed class TutorialWaitLevel : IGameLevel
    {
        public string Name => "Tutorial 3: WAIT";

        public void Apply(GameModel model)
        {
            model.ClearObstacles();

            int PxX(int xCells) => Grid.XCellsToPx(xCells);

            // Идея: движущаяся платформа перевозит игрока над шипами.
            // Если продолжать MOVE — игрок сойдёт на шипы и проиграет.
            // Решение: зайти на платформу и WAIT, пока она довезёт до безопасной зоны.

            const int platformHeightPx = 20;
            int platformTopY = GameModel.GroundY - platformHeightPx;

            // Платформа: ширина 3 клетки, ездит туда-обратно над полем шипов.
            // На tick=0 стоит слева, чтобы можно было на неё зайти сразу.
            model.AddObstacle(new MovingPlatformObstacle(
                minX: PxX(3),
                maxX: PxX(9),
                y: platformTopY,
                width: 3 * Grid.CellSizePx,
                height: platformHeightPx,
                stepPerTick: Grid.CellSizePx));

            // Шипы на земле: перекрывают путь с x=4..10 клеток.
            const int spikesInset = 5;
            const int spikesHeightPx = 18;
            int spikesY = GameModel.GroundY - spikesHeightPx;
            int spikesX = PxX(4) + spikesInset;
            int spikesW = (6 * Grid.CellSizePx) - (spikesInset * 2); // клетки 4..9 включительно
            model.AddObstacle(new SpikesObstacle(
                x: spikesX,
                y: spikesY,
                width: spikesW,
                height: spikesHeightPx));

            // Финиш справа, после шипов.
            model.SetFinishZone(new Rectangle(
                x: PxX(12),
                y: GameModel.GroundY - 140,
                width: 60,
                height: 140));
        }
    }
}

