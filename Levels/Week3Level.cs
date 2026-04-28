using System.Drawing;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Levels
{
    public sealed class Week3Level : IGameLevel
    {
        public string Name => "Week 3 (moving platforms + hazards)";

        public void Apply(GameModel model)
        {
            // MVP препятствия для недели 3 + win-zone из недели 4.
            model.ClearObstacles();

            // Helpers: привязка к сетке (x вправо, y вверх от линии земли).
            int PxX(int xCells) => Grid.XCellsToPx(xCells);
            int CellBottomY(int yCells) => GameModel.GroundY - (yCells * Grid.CellSizePx);
            int PlatformTopYFromCellBottom(int yCells, int heightPx) => CellBottomY(yCells) - heightPx;

            // Пила: 1 клетка, но с inset'ами (меньше клетки). Низ без отступа => прижата к земле.
            const int sawInset = 10;
            var sawSize = Grid.CellSizePx - sawInset - sawInset;  // 30 (квадрат)
            var sawCenterOffsetX = (Grid.CellSizePx - sawSize) / 2; // 10
            model.AddObstacle(new SawObstacle(
                minX: PxX(5) + sawCenterOffsetX,
                maxX: PxX(11) + sawCenterOffsetX,
                y: GameModel.GroundY - sawSize,
                width: sawSize,
                height: sawSize,
                stepPerTick: Grid.CellSizePx));

            // Платформа (слева): чуть выше земли, ходит туда-обратно.
            const int movingPlatformMinX = 0;               // cells: 0
            const int movingPlatformMaxX = 6;               // 300px
            const int movingPlatformCellY = 2;              // bottom at 250px (2 клетки над землёй)
            const int movingPlatformWidthCells = 3;         // 150px
            const int movingPlatformHeightPx = 20;
            const int movingPlatformStepPerTick = Grid.CellSizePx;

            model.AddObstacle(new MovingPlatformObstacle(
                minX: PxX(movingPlatformMinX),
                maxX: PxX(movingPlatformMaxX),
                y: PlatformTopYFromCellBottom(movingPlatformCellY, movingPlatformHeightPx),
                width: movingPlatformWidthCells * Grid.CellSizePx,
                height: movingPlatformHeightPx,
                stepPerTick: movingPlatformStepPerTick));

            // Статическая платформа (слева над игроком): твёрдая сверху.
            model.AddObstacle(new StaticPlatformObstacle(
                x: PxX(0),
                y: PlatformTopYFromCellBottom(yCells: 5, heightPx: 20),
                width: 4 * Grid.CellSizePx,
                height: 20));

            // Шипы на земле: растягиваются на N клеток, но inset только по краям.
            const int spikesInset = 5;
            var spikesX = PxX(12) + spikesInset;
            var spikesWidth = (2 * Grid.CellSizePx) - spikesInset - spikesInset;
            model.AddObstacle(new SpikesObstacle(
                x: spikesX,
                y: GameModel.GroundY - 18,
                width: spikesWidth,
                height: 18));

            // Доп платформа перед выходом: позволяет перепрыгнуть последние шипы и выйти в FinishZone.
            const int exitPlatformCellY = 3;
            const int exitPlatformXCells = 10;
            const int exitPlatformWidthCells = 4;
            model.AddObstacle(new StaticPlatformObstacle(
                x: PxX(exitPlatformXCells),
                y: PlatformTopYFromCellBottom(exitPlatformCellY, 20),
                width: exitPlatformWidthCells * Grid.CellSizePx,
                height: 20));

            // Финиш-зона (победа при пересечении) — ставим её НА платформу перед выходом.
            // Низ зоны = верх платформы, чтобы игрок мог «выйти» с неё.
            const int finishWidth = 40;  // можно тоже привязать к клетке, но оставляем компактной
            const int finishHeight = 80; // ~1.6 клетки
            var finishPlatformTop = PlatformTopYFromCellBottom(exitPlatformCellY, 20);
            model.SetFinishZone(new Rectangle(
                x: GameModel.CanvasWidth - Grid.CellSizePx,
                y: finishPlatformTop - finishHeight,
                width: finishWidth,
                height: finishHeight));
        }
    }
}

