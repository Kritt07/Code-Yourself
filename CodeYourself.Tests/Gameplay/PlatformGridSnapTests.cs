using CodeYourself.Models;
using CodeYourself.Models.Obstacles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Gameplay
{
    [TestClass]
    public sealed class PlatformGridSnapTests
    {
        [TestMethod]
        public void Snap_OnMovingPlatform_AlignsToPlatformCellCenters()
        {
            var model = new GameModel();
            model.ClearObstacles();

            // minX == maxX => платформа не едет по X, проще проверить снап.
            var platform = new MovingPlatformObstacle(
                minX: 0,
                maxX: 0,
                y: 200,
                width: 150,
                height: 20,
                stepPerTick: Grid.CellSizePx);

            model.AddObstacle(platform);

            var topY = platform.Bounds.Top - GameModel.PlayerHeightPx;
            model.SetPlayerPosition(12, topY);
            model.StepSimulationTick();

            model.SnapPlayerToCellCenter();

            var cell = Grid.CellSizePx;
            var offset = GameModel.PlayerCellCenterOffsetXPx;
            var rel = model.Player.Position.X - offset - platform.Bounds.Left;
            Assert.AreEqual(0, rel % cell, "Player should align to platform-local grid.");
            Assert.AreEqual(5, model.Player.Position.X);
        }

        [TestMethod]
        public void Snap_AfterPlatformMoves_StillAlignsToPlatformGrid()
        {
            var model = new GameModel();
            model.ClearObstacles();

            var platform = new MovingPlatformObstacle(
                minX: 0,
                maxX: 200,
                y: 200,
                width: 150,
                height: 20,
                stepPerTick: 30);

            model.AddObstacle(platform);

            var topY = platform.Bounds.Top - GameModel.PlayerHeightPx;
            model.SetPlayerPosition(GameModel.PlayerCellCenterOffsetXPx, topY);
            for (var i = 0; i < 45; i++)
                model.StepSimulationTick();

            model.Player.SetPosition(platform.Bounds.Left + 17, topY);
            model.StepSimulationTick();

            model.SnapPlayerToCellCenter();

            var cell = Grid.CellSizePx;
            var offset = GameModel.PlayerCellCenterOffsetXPx;
            var rel = model.Player.Position.X - offset - platform.Bounds.Left;
            Assert.AreEqual(0, rel % cell);
        }

        [TestMethod]
        public void Snap_OnGround_UsesWorldGrid()
        {
            var model = new GameModel();
            model.ClearObstacles();

            var groundY = GameModel.GroundY - GameModel.PlayerHeightPx;
            model.SetPlayerPosition(12, groundY);
            model.StepSimulationTick();

            model.SnapPlayerToCellCenter();

            var cell = Grid.CellSizePx;
            var offset = GameModel.PlayerCellCenterOffsetXPx;
            var rel = model.Player.Position.X - offset;
            Assert.AreEqual(0, rel % cell);
        }

        [TestMethod]
        public void MovingSpikes_WorldX_EqualsPlatformLeftPlusOffset()
        {
            const int minX = 0;
            const int maxX = 200;
            const int step = 30;
            const int offsetFromPlatform = 55;

            var platform = new MovingPlatformObstacle(minX, maxX, y: 200, width: 150, height: 20, stepPerTick: step);
            var spikes = new MovingSpikesObstacle(minX, maxX, y: 180, width: 40, height: 18, stepPerTick: step, offsetFromPlatform);

            for (var t = 0; t < 120; t += 13)
            {
                platform.Update(t);
                spikes.Update(t);
                var expectedLeft = MovingSpikesObstacle.GetWorldLeftXForSimTick(t, minX, maxX, step, offsetFromPlatform);
                Assert.AreEqual(expectedLeft, spikes.Bounds.Left);
                Assert.AreEqual(platform.Bounds.Left + offsetFromPlatform, spikes.Bounds.Left);
            }
        }
    }
}
