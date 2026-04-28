using CodeYourself.Commands.Base;
using CodeYourself.Models;

namespace CodeYourself.Commands
{
    internal sealed class JumpCommand : GameCommand
    {
        private readonly MoveDirection _direction;
        private readonly int _cells;

        public JumpCommand(int lineIndex, MoveDirection direction, int cells) : base(lineIndex)
        {
            _direction = direction;
            _cells = cells;
        }

        public override void Execute(GameModel model)
        {
            model.JumpPlayer(_direction, cells: _cells);
        }
    }
}

