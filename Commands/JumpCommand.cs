using CodeYourself.Commands.Base;
using CodeYourself.Models;

namespace CodeYourself.Commands
{
    internal sealed class JumpCommand : GameCommand
    {
        private readonly MoveDirection _direction;

        public JumpCommand(int lineIndex, MoveDirection direction) : base(lineIndex)
        {
            _direction = direction;
        }

        public override void Execute(GameModel model)
        {
            model.JumpPlayer(_direction);
        }
    }
}

