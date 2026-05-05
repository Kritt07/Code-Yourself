using System;
using System.Collections.Generic;

namespace CodeYourself.Levels
{
    /// <summary>
    /// Порядок уровней кампании для экрана выбора и кнопки «Следующий уровень».
    /// </summary>
    public static class LevelCatalog
    {
        private static readonly IGameLevel[] Ordered =
        {
            new Week3Level(),
        };

        public static IReadOnlyList<IGameLevel> All => Ordered;

        public static bool TryGetNext(IGameLevel current, out IGameLevel next)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

            int index = -1;
            for (int i = 0; i < Ordered.Length; i++)
            {
                if (Ordered[i].GetType() == current.GetType())
                {
                    index = i;
                    break;
                }
            }

            if (index < 0 || index + 1 >= Ordered.Length)
            {
                next = null;
                return false;
            }

            next = Ordered[index + 1];
            return true;
        }
    }
}
