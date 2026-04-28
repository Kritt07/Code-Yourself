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

            // Пила: по земле, ходит туда-обратно.
            model.AddObstacle(new SawObstacle(
                minX: 250,
                maxX: 550,
                y: GameModel.GroundY - 35,
                size: 35,
                stepPerTick: 50));

            // Платформа (слева): чуть выше земли, ходит туда-обратно.
            const int movingPlatformMinX = 0;
            const int movingPlatformMaxX = 280;
            const int movingPlatformY = GameModel.GroundY - 120;
            const int movingPlatformWidth = 150;
            const int movingPlatformHeight = 20;
            const int movingPlatformStepPerTick = 50;

            model.AddObstacle(new MovingPlatformObstacle(
                minX: movingPlatformMinX,
                maxX: movingPlatformMaxX,
                y: movingPlatformY,
                width: movingPlatformWidth,
                height: movingPlatformHeight,
                stepPerTick: movingPlatformStepPerTick));

            // Статическая платформа (слева над игроком): твёрдая сверху.
            model.AddObstacle(new StaticPlatformObstacle(
                x: 0,
                y: GameModel.GroundY - 230,
                width: 220,
                height: 20));

            // Шипы на земле: смертельны при любом пересечении.
            model.AddObstacle(new SpikesObstacle(
                x: 620,
                y: GameModel.GroundY - 18,
                width: 120,
                height: 18));

            // Доп платформа перед выходом: позволяет перепрыгнуть последние шипы и выйти в FinishZone.
            model.AddObstacle(new StaticPlatformObstacle(
                x: 540,
                y: GameModel.GroundY - 95,
                width: 190,
                height: 20));

            // Финиш-зона (победа при пересечении) — ставим её НА платформу перед выходом.
            // Низ зоны = верх платформы, чтобы игрок мог «выйти» с неё.
            const int finishWidth = 40;
            const int finishHeight = 80;
            var finishPlatformTop = GameModel.GroundY - 95;
            model.SetFinishZone(new Rectangle(
                x: GameModel.CanvasWidth - 60,
                y: finishPlatformTop - finishHeight,
                width: finishWidth,
                height: finishHeight));

            // Шипы на движущейся платформе (слева): "приклеены" к ней по X.
            model.AddObstacle(new MovingSpikesObstacle(
                minX: movingPlatformMinX,
                maxX: movingPlatformMaxX,
                y: movingPlatformY - 18,
                width: 80,
                height: 18,
                stepPerTick: movingPlatformStepPerTick,
                xOffset: 60));
        }
    }
}

