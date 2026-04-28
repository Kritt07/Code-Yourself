using CodeYourself.Models;
using CodeYourself.Models.Obstacles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Gameplay
{
    [TestClass]
    public sealed class MovingPlatformRideTests
    {
        [TestMethod]
        public void Jump_DoesNotApply_WhenNotGrounded()
        {
            var model = new GameModel();
            model.ClearObstacles();

            // Игрок в воздухе.
            model.SetPlayerPosition(10, 50);

            model.JumpPlayer(MoveDirection.Right);

            var y0 = model.Player.Position.Y;
            model.StepSimulationTick();
            var y1 = model.Player.Position.Y;

            // Должен падать (y растёт), а не лететь вверх.
            Assert.IsTrue(y1 >= y0);
        }

        [TestMethod]
        public void Jump_Applies_WhenStandingOnPlatform()
        {
            var model = new GameModel();
            model.ClearObstacles();

            var platform = new StaticPlatformObstacle(
                x: 0,
                y: 200,
                width: 400,
                height: 20);
            model.AddObstacle(platform);

            model.SetPlayerPosition(10, platform.Bounds.Top - GameModel.PlayerHeightPx);
            model.StepSimulationTick(); // зафиксировать grounded

            model.JumpPlayer(MoveDirection.Right);

            var y0 = model.Player.Position.Y;
            model.StepSimulationTick();
            var y1 = model.Player.Position.Y;

            // После прыжка игрок должен начать подниматься (y уменьшается).
            Assert.IsTrue(y1 < y0);
        }

        [TestMethod]
        public void Player_Rides_MovingPlatform_ByPlatformDx()
        {
            var model = new GameModel();
            model.ClearObstacles();

            var platform = new MovingPlatformObstacle(
                minX: 0,
                maxX: 100,
                y: 200,
                width: 200,
                height: 20,
                // 30 px / command-tick => 1 px / sim-tick (30Hz)
                stepPerTick: 30);

            model.AddObstacle(platform);

            // Ставим игрока на платформу.
            model.SetPlayerPosition(10, platform.Bounds.Top - GameModel.PlayerHeightPx);

            // Первый сим-тик: dx платформы = 0 (tick=0), просто фиксируем состояние.
            model.StepSimulationTick();
            var x0 = model.Player.Position.X;

            // Второй сим-тик: платформа сместится на +1px, игрок должен «поехать» вместе с ней.
            model.StepSimulationTick();
            var x1 = model.Player.Position.X;

            Assert.AreEqual(x0 + 1, x1);
        }
    }
}

