using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeYourself.Models;

namespace CodeYourself.Commands.Base
{
    public abstract class GameCommand
    {
        /// <summary>
        /// Индекс строки в текстовом редакторе, из которой была создана команда.
        /// Нужен для подсветки активной команды во время выполнения.
        /// </summary>
        public int LineIndex { get; }

        protected GameCommand(int lineIndex)
        {
            LineIndex = lineIndex;
        }

        /// <summary>
        /// Метод выполнения логики команды.
        /// Передаем модель, чтобы команда знала, над чем совершать действия.
        /// </summary>
        public abstract void Execute(GameModel model);
    }
}
