using CodeYourself.Commands.Base;
using CodeYourself.Models;

namespace CodeYourself.Commands
{
    internal sealed class JumpCommand : GameCommand
    {
        private readonly MoveDirection _direction;
        private readonly int _stepIndex;
        private readonly int _totalSteps;

        public JumpCommand(int lineIndex, MoveDirection direction, int stepIndex, int totalSteps) : base(lineIndex)
        {
            _direction = direction;
            _stepIndex = stepIndex;
            _totalSteps = totalSteps;
        }

        public override void Execute(GameModel model)
        {
            model.JumpPlayerStep(_direction, _stepIndex, _totalSteps);
        }
    }
}

