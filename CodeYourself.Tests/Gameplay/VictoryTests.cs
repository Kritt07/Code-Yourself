using System.Reflection;
using CodeYourself.Controllers;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;
using CodeYourself.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Gameplay
{
    [TestClass]
    public sealed class VictoryTests
    {
        [TestMethod]
        public void Victory_WhenPlayerEntersFinishZone()
        {
            var model = new GameModel();
            model.ClearObstacles();

            model.SetPlayerPosition(10, GameModel.GroundY - GameModel.PlayerHeightPx);
            model.SetFinishZone(model.GetPlayerBounds());

            model.StepSimulationTick();

            Assert.AreEqual(GameEndState.Won, model.EndState);
        }

        [TestMethod]
        public void LosePriority_IfFinishAndHazardSameTick()
        {
            var model = new GameModel();
            model.ClearObstacles();

            var spikes = new SpikesObstacle(
                x: 300,
                y: GameModel.GroundY - 18,
                width: 120,
                height: 18);
            model.AddObstacle(spikes);

            // Финиш прямо на шипах.
            model.SetFinishZone(spikes.Bounds);

            // 1) Фиксируем prev
            model.SetPlayerPosition(0, GameModel.GroundY - GameModel.PlayerHeightPx);
            model.StepSimulationTick();

            // 2) «Телепорт» на шипы за один тик, чтобы гарантированно сработал swept и одновременно пересечение с FinishZone.
            model.SetPlayerPosition(310, GameModel.GroundY - GameModel.PlayerHeightPx);
            model.StepSimulationTick();

            Assert.AreEqual(GameEndState.Lost, model.EndState);
        }

        [TestMethod]
        public void ControllerStopsOnWin()
        {
            var model = new GameModel();
            model.ClearObstacles();

            // Победа должна произойти на первом же сим-тыке.
            model.SetPlayerPosition(10, GameModel.GroundY - GameModel.PlayerHeightPx);
            model.SetFinishZone(model.GetPlayerBounds());

            var controller = new GameController(model);
            var parser = new CommandParser();
            var parse = parser.Parse("WAIT 1");
            Assert.IsTrue(parse.IsSuccess);
            foreach (var cmd in parse.Commands)
                controller.EnqueueCommand(cmd);

            controller.Start();

            var method = typeof(GameController).GetMethod("StepOneSimulationTick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "StepOneSimulationTick method not found via reflection.");

            var ok = (bool)method.Invoke(controller, null);

            Assert.IsFalse(ok);
            Assert.IsFalse(controller.IsRunning);
            Assert.AreEqual(GameEndState.Won, model.EndState);
        }
    }
}

