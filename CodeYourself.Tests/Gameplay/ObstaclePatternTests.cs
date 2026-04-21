using CodeYourself.Models;
using CodeYourself.Models.Obstacles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Gameplay
{
    [TestClass]
    public sealed class ObstaclePatternTests
    {
        [TestMethod]
        public void Saw_IsDeterministic_AndOscillatesWithinRange()
        {
            var y = GameModel.GroundY - 50;
            var saw = new SawObstacle(minX: 100, maxX: 300, y: y, size: 50, stepPerTick: 50);

            // На границах командных тиков (каждые 30 под-тиков) позиция должна совпадать со старым паттерном.
            var expected = new[] { 100, 150, 200, 250, 300, 250, 200, 150, 100 };

            for (int tick = 0; tick < expected.Length; tick++)
            {
                var simTick = tick * GameModel.DefaultSubTicksPerCommandTick;
                saw.Update(simTick);
                Assert.AreEqual(expected[tick], saw.Bounds.X, $"tick={tick}");
                Assert.AreEqual(y, saw.Bounds.Y, $"tick={tick}");
            }

            // Determinism: same tick => same X
            saw.Update(3 * GameModel.DefaultSubTicksPerCommandTick);
            var x1 = saw.Bounds.X;
            saw.Update(3 * GameModel.DefaultSubTicksPerCommandTick);
            var x2 = saw.Bounds.X;
            Assert.AreEqual(x1, x2);
        }

        [TestMethod]
        public void MovingPlatform_IsDeterministic_AndOscillatesWithinRange()
        {
            var platform = new MovingPlatformObstacle(
                minX: 0,
                maxX: 200,
                y: 100,
                width: 150,
                height: 20,
                stepPerTick: 50);

            // На границах командных тиков (каждые 30 под-тиков) паттерн должен совпадать со старым.
            var expected = new[] { 0, 50, 100, 150, 200, 150, 100, 50, 0 };
            for (int tick = 0; tick < expected.Length; tick++)
            {
                var simTick = tick * GameModel.DefaultSubTicksPerCommandTick;
                platform.Update(simTick);
                Assert.AreEqual(expected[tick], platform.Bounds.X, $"tick={tick}");
            }
        }
    }
}

