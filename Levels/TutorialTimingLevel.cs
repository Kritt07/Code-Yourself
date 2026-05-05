using System.Drawing;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Levels
{
    public sealed class TutorialTimingLevel : IGameLevel
    {
        public string Name => "Tutorial 5: Timing (WAIT → JUMP)";

        public void Apply(GameModel model)
        {
            model.ClearObstacles();

            int PxX(int xCells) => Grid.XCellsToPx(xCells);
            int CellBottomY(int yCells) => GameModel.GroundY - (yCells * Grid.CellSizePx);
            int PlatformTopYFromCellBottom(int yCells, int heightPx) => CellBottomY(yCells) - heightPx;

            // Идея: пила ходит по земле и "караулит" проход; нужно подождать (WAIT), когда безопасно,
            // затем перепрыгнуть шипы (JUMP) и выйти на платформу к финишу.

            // Пила: 1 клетка, но уменьшенная, ездит по короткому отрезку.
            const int sawInset = 10;
            int sawSize = Grid.CellSizePx - (sawInset * 2); // 30
            int sawCenterOffsetX = (Grid.CellSizePx - sawSize) / 2; // 10
            model.AddObstacle(new SawObstacle(
                minX: PxX(3) + sawCenterOffsetX,
                maxX: PxX(6) + sawCenterOffsetX,
                y: GameModel.GroundY - sawSize,
                width: sawSize,
                height: sawSize,
                stepPerTick: Grid.CellSizePx));

            // Шипы на земле, которые нужно перепрыгнуть на дистанцию 1 клетку.
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

            // Платформа перед финишем: безопасная "точка" после прыжка.
            const int platformHeightPx = 20;
            model.AddObstacle(new StaticPlatformObstacle(
                x: PxX(11),
                y: PlatformTopYFromCellBottom(yCells: 1, heightPx: platformHeightPx),
                width: 5 * Grid.CellSizePx,
                height: platformHeightPx));

            // Финиш над платформой, чтобы нельзя было выиграть "по земле" раньше.
            int platformTop = PlatformTopYFromCellBottom(yCells: 1, heightPx: platformHeightPx);
            model.SetFinishZone(new Rectangle(
                x: PxX(15) + 10,
                y: platformTop - 90,
                width: 50,
                height: 90));
        }
    }
}

