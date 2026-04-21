using System;

namespace CodeYourself.Models.Obstacles
{
    internal static class OscillatingMotion
    {
        public const int DefaultSubTicksPerCommandTick = 30;

        /// <summary>
        /// Детерминированное движение туда-обратно по X (командные тики):
        /// minX -> maxX -> minX ... с шагом stepPerCommandTick.
        /// </summary>
        public static int GetX(int tickIndex, int minX, int maxX, int stepPerCommandTick)
        {
            if (stepPerCommandTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepPerCommandTick), "stepPerCommandTick must be positive.");

            if (minX > maxX)
            {
                var tmp = minX;
                minX = maxX;
                maxX = tmp;
            }

            var range = maxX - minX;
            if (range == 0)
                return minX;

            // Двигаемся по дискретным позициям с фиксированным шагом.
            // Если диапазон не кратен шагу — последний шаг "упирается" в maxX.
            var stepsForward = Math.Max(1, (int)Math.Ceiling(range / (double)stepPerCommandTick));
            var period = stepsForward * 2;

            var t = tickIndex % period;
            if (t < 0) t += period;

            var forwardIndex = t <= stepsForward ? t : (period - t);
            var x = minX + forwardIndex * stepPerCommandTick;
            return Math.Min(maxX, x);
        }

        /// <summary>
        /// Детерминированное движение туда-обратно по X на под-тиках.
        /// Интерпретирует <paramref name="stepPerCommandTick"/> как дистанцию за 1 командный тик,
        /// а <paramref name="simTickIndex"/> — как индекс под-тика.
        /// </summary>
        public static int GetXForSubTick(int simTickIndex, int minX, int maxX, int stepPerCommandTick, int subTicksPerCommandTick = DefaultSubTicksPerCommandTick)
        {
            if (subTicksPerCommandTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(subTicksPerCommandTick), "subTicksPerCommandTick must be positive.");

            if (stepPerCommandTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepPerCommandTick), "stepPerCommandTick must be positive.");

            if (minX > maxX)
            {
                var tmp = minX;
                minX = maxX;
                maxX = tmp;
            }

            var range = maxX - minX;
            if (range == 0)
                return minX;

            var stepPerSubTick = stepPerCommandTick / (double)subTicksPerCommandTick;
            var distance = simTickIndex * stepPerSubTick;

            var periodDistance = 2.0 * range;
            var m = distance % periodDistance;
            if (m < 0) m += periodDistance;

            var pos = m <= range ? m : (periodDistance - m);
            var x = minX + pos;

            var xi = (int)Math.Round(x);
            if (xi < minX) xi = minX;
            if (xi > maxX) xi = maxX;
            return xi;
        }
    }
}

