using CodeYourself.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Gameplay
{
    [TestClass]
    public sealed class MoveDistanceTests
    {
        [TestMethod]
        public void Move_Strives_For50PxPerCommandTick_Over30SimTicks()
        {
            var model = new GameModel();
            model.ClearObstacles();

            model.SetPlayerPosition(0, GameModel.GroundY - GameModel.PlayerHeightPx);
            var x0 = model.Player.Position.X;

            model.MovePlayer(MoveDirection.Right, GameModel.DefaultCommandDurationSimTicks);
            for (int i = 0; i < GameModel.DefaultCommandDurationSimTicks; i++)
                model.StepSimulationTick();

            var x1 = model.Player.Position.X;
            Assert.AreEqual(x0 + 50, x1);
        }
    }
}

