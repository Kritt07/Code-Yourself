using System.Drawing;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Levels
{
    /// <summary>
    /// Визуальный тест уровня: шипы на 1 клетку и на 3 клетки.
    /// </summary>
    public sealed class NeonSpikesTestLevel : IGameLevel
    {
        public string Name => "Neon spikes test (1 cell + 3 cells)";

        public void Apply(GameModel model)
        {
            model.ClearObstacles();

            int PxX(int xCells) => Grid.XCellsToPx(xCells);

            // Шипы на земле: 1 клетка и 3 клетки, inset только по краям.
            const int spikesInset = 5;
            const int spikesHeight = 18;
            int spikesY = GameModel.GroundY - spikesHeight;

            // 1 клетка (xCells = 6)
            int spikes1X = PxX(6) + spikesInset;
            int spikes1W = (1 * Grid.CellSizePx) - (spikesInset * 2);
            model.AddObstacle(new SpikesObstacle(
                x: spikes1X,
                y: spikesY,
                width: spikes1W,
                height: spikesHeight));

            // 3 клетки (xCells = 10)
            int spikes3X = PxX(10) + spikesInset;
            int spikes3W = (3 * Grid.CellSizePx) - (spikesInset * 2);
            model.AddObstacle(new SpikesObstacle(
                x: spikes3X,
                y: spikesY,
                width: spikes3W,
                height: spikesHeight));

            // Финиш-зона чтобы можно было быстро завершать уровень при желании.
            model.SetFinishZone(new Rectangle(
                x: GameModel.CanvasWidth - Grid.CellSizePx,
                y: GameModel.GroundY - 120,
                width: 40,
                height: 120));
        }
    }
}

