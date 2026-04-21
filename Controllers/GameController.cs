using CodeYourself.Commands.Base;
using CodeYourself.Models;
using System;
using System.Collections.Generic;

namespace CodeYourself.Controllers
{
    public sealed class GameController : IDisposable
    {
        private const int SubTicksPerCommandTick = 30;
        private readonly Queue<GameCommand> _commandQueue = new Queue<GameCommand>();

        private readonly GameModel _model;
        private readonly System.Windows.Forms.Timer _tickTimer;

        public event Action GameUpdated; // событие для перерисовки
        public event Action<int> CurrentLineIndexChanged;

        public int CurrentLineIndex { get; private set; } = -1;
        public bool IsRunning => _tickTimer.Enabled;

        public GameController(GameModel model)
        {
            _model = model;

            _tickTimer = new System.Windows.Forms.Timer();
            _tickTimer.Interval = 1000; // 1 тик/сек: 1 команда/сек, но симуляция будет в под-тиках
            _tickTimer.Tick += TickTimer_Tick;
        }

        public void Start()
        {
            if (_commandQueue.Count == 0)
                return;

            _tickTimer.Start();
        }

        public void Stop()
        {
            _tickTimer.Stop();
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

        private void TickTimer_Tick(object sender, EventArgs e)
        {
            _model.BeginCommandTick(SubTicksPerCommandTick);

            if (_commandQueue.Count == 0)
            {
                Stop();
                GameUpdated?.Invoke();
                return;
            }

            if (_commandQueue.Count > 0)
            {
                var command = _commandQueue.Dequeue();
                SetCurrentLineIndex(command.LineIndex);
                command.Execute(_model);
            }

            for (int i = 0; i < SubTicksPerCommandTick; i++)
                _model.StepSubTick();
            _model.EndCommandTick();

            if (_model.IsGameOver)
            {
                Stop();
                GameUpdated?.Invoke();
                return;
            }

            GameUpdated?.Invoke(); // говорим View, что нужно перерисоваться
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
            _tickTimer.Tick -= TickTimer_Tick;
            _tickTimer.Dispose();
        }
    }
}