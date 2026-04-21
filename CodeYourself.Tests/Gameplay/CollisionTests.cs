using CodeYourself.Models;
using CodeYourself.Models.Obstacles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Gameplay
{
    [TestClass]
    public sealed class CollisionTests
    {
        [TestMethod]
        public void SweptCollision_Triggers_WhenPlayerCrossesObstacleInSingleTick()
        {
            var model = new GameModel();
            model.ClearObstacles();

            // Статичная пила в центре
            var saw = new SawObstacle(minX: 300, maxX: 300, y: GameModel.GroundY - 50, size: 50, stepPerTick: 50);
            model.AddObstacle(saw);

            // Игрок стартует слева, "телепортом" пересекает пилу за один тик.
            model.SetPlayerPosition(0, GameModel.GroundY - GameModel.PlayerSize);

            model.BeginCommandTick(subTicksPerCommandTick: 1); // фиксируем prev
            model.SetPlayerPosition(600, GameModel.GroundY - GameModel.PlayerSize); // curr справа

            model.StepSubTick();
            model.EndCommandTick();

            Assert.IsTrue(model.IsGameOver);
            StringAssert.Contains(model.GameOverReason, "Collision");
        }

        [TestMethod]
        public void SweptCollision_DoesNotTrigger_WhenNoOverlapAlongPath()
        {
            var model = new GameModel();
            model.ClearObstacles();

            var saw = new SawObstacle(minX: 700, maxX: 700, y: GameModel.GroundY - 50, size: 50, stepPerTick: 50);
            model.AddObstacle(saw);

            model.SetPlayerPosition(0, GameModel.GroundY - GameModel.PlayerSize);

            model.BeginCommandTick(subTicksPerCommandTick: 1);
            model.SetPlayerPosition(600, GameModel.GroundY - GameModel.PlayerSize);

            model.StepSubTick();
            model.EndCommandTick();

            Assert.IsFalse(model.IsGameOver);
        }
    }
}

