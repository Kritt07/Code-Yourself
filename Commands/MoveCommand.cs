using CodeYourself.Commands.Base;
using CodeYourself.Models;

namespace CodeYourself.Commands
{
    internal class MoveCommand : GameCommand
    {
        private readonly MoveDirection _direction;

        public MoveCommand(int lineIndex, MoveDirection direction) : base(lineIndex)
        {
            _direction = direction;
        }

        public override void Execute(GameModel model)
        {
            model.MovePlayer(_direction);
        }
    }
}
