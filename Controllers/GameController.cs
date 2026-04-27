using CodeYourself.Commands.Base;
using CodeYourself.Models;
using System;
using System.Collections.Generic;

namespace CodeYourself.Controllers
{
    public sealed class GameController : IDisposable
    {
        private const int SimulationTicksPerCommand = 30;
        private readonly Queue<GameCommand> _commandQueue = new Queue<GameCommand>();

        private readonly GameModel _model;
        private readonly System.Windows.Forms.Timer _commandTimer;

        private int _remainingSimulationTicksForCommand;
        private int _commandTickCount;

        public event Action GameUpdated; // событие для перерисовки
        public event Action LogicFrameCommitted; // логический кадр готов (для 60Hz рендера)
        public event Action<int> CurrentLineIndexChanged;

        public int CurrentLineIndex { get; private set; } = -1;
        public bool IsRunning => _commandTimer.Enabled;
        public int CommandTickCount => _commandTickCount;

        public GameController(GameModel model)
        {
            _model = model;

            _commandTimer = new System.Windows.Forms.Timer();
            // Чем меньше интервал, тем быстрее прогоняются сим-такты и команды.
            // WinForms Timer всё равно квантуется ОС, но это убирает секундную паузу.
            _commandTimer.Interval = 1;
            _commandTimer.Tick += CommandTimer_Tick;
        }

        public void Start()
        {
            if (_commandQueue.Count == 0)
                return;

            _remainingSimulationTicksForCommand = 0;
            _commandTickCount = 0;

            _commandTimer.Start();
        }

        public void Stop()
        {
            _commandTimer.Stop();

            _remainingSimulationTicksForCommand = 0;
            SetCurrentLineIndex(-1);
        }

        public void EnqueueCommand(GameCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _commandQueue.Enqueue(command);
        }

        public void ClearCommands()
        {
            _commandQueue.Clear();
        }

        private void CommandTimer_Tick(object sender, EventArgs e)
        {
            if (_model.IsGameOver)
            {
                Stop();
                GameUpdated?.Invoke(); // финальная перерисовка
                return;
            }

            // 1) Если идёт симуляция текущей команды — делаем один сим-так (без real-time ожидания 33ms).
            if (_remainingSimulationTicksForCommand > 0)
            {
                _model.StepSimulationTick();
                _remainingSimulationTicksForCommand--;
                LogicFrameCommitted?.Invoke();
                return;
            }

            // 2) Команда закончилась — берем следующую.
            if (_commandQueue.Count == 0)
            {
                Stop();
                GameUpdated?.Invoke(); // финальная перерисовка
                return;
            }

            var command = _commandQueue.Dequeue();
            _commandTickCount++;

            SetCurrentLineIndex(command.LineIndex);
            command.Execute(_model);
            _remainingSimulationTicksForCommand = SimulationTicksPerCommand;
        }

        public GameModel Model => _model;

        private void SetCurrentLineIndex(int lineIndex)
        {
            if (CurrentLineIndex == lineIndex)
                return;

            CurrentLineIndex = lineIndex;
            CurrentLineIndexChanged?.Invoke(lineIndex);
        }

        public void Dispose()
        {
            Stop();
            _commandTimer.Tick -= CommandTimer_Tick;
            _commandTimer.Dispose();
        }
    }
}