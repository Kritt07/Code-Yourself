using System.Drawing;
using CodeYourself.Models;

namespace CodeYourself.Levels
{
    public sealed class TutorialMoveLevel : IGameLevel
    {
        public string Name => "Tutorial 1: MOVE";

        public void Apply(GameModel model)
        {
            model.ClearObstacles();

            // Простая цель: дойти до финиша серией MOVE.
            model.SetFinishZone(new Rectangle(
                x: GameModel.CanvasWidth - Grid.CellSizePx,
                y: GameModel.GroundY - 120,
                width: 40,
                height: 120));
        }
    }
}

