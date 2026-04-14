using CodeYourself.Commands.Base;
using CodeYourself.Models;

namespace CodeYourself.Commands
{
    internal class WaitCommand : GameCommand
    {
        public WaitCommand(int lineIndex) : base(lineIndex)
        {
        }

        public override void Execute(GameModel model)
        {
            // намеренно ничего не делаем: "wait" = пропуск тика
        }
    }
}
