using CodeYourself.Controllers;
using CodeYourself.Levels;
using CodeYourself.Models;
using CodeYourself.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Controllers
{
    [TestClass]
    public sealed class GameControllerPauseTests
    {
        [TestMethod]
        public void PauseSimulation_ThenResume_RestoresRunning_WhenWasExecuting()
        {
            var model = new GameModel();
            new Week3Level().Apply(model);

            var controller = new GameController(model);
            var parser = new CommandParser();
            var result = parser.Parse("WAIT 50");
            Assert.IsTrue(result.IsSuccess);

            foreach (var cmd in result.Commands)
                controller.EnqueueCommand(cmd);

            controller.Start();
            Assert.IsTrue(controller.IsRunning);

            controller.PauseSimulation();
            Assert.IsTrue(controller.IsSimulationPaused);
            Assert.IsFalse(controller.IsRunning);

            controller.ResumeSimulation();
            Assert.IsFalse(controller.IsSimulationPaused);
            Assert.IsTrue(controller.IsRunning);

            controller.Stop();
        }

        [TestMethod]
        public void Stop_ClearsSimulationPaused()
        {
            var model = new GameModel();
            var controller = new GameController(model);

            controller.PauseSimulation();
            Assert.IsTrue(controller.IsSimulationPaused);

            controller.Stop();
            Assert.IsFalse(controller.IsSimulationPaused);
        }
    }
}
