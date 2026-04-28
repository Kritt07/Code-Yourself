using CodeYourself.Commands.Base;
using CodeYourself.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CodeYourself.Controllers
{
    public sealed class GameController : IDisposable
    {
        private const int SimulationTicksPerCommand = 30;
        private const double CommandTickDurationMs = 500.0; // 0.5s
        private const double SimTickIntervalMs = CommandTickDurationMs / SimulationTicksPerCommand; // fixed-step

        private readonly Queue<GameCommand> _commandQueue = new Queue<GameCommand>();

        private readonly GameModel _model;
        private readonly System.Windows.Forms.Timer _commandTimer;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _accumulatorMs;
        private long _lastElapsedMs;

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
            // Таймер может джиттерить, поэтому реальный темп держим через accumulator + fixed-step.
            _commandTimer.Interval = 1;
            _commandTimer.Tick += CommandTimer_Tick;
        }

        public void Start()
        {
            if (_commandQueue.Count == 0)
                return;

            _remainingSimulationTicksForCommand = 0;
            _commandTickCount = 0;
            _accumulatorMs = 0;
            _lastElapsedMs = 0;
            _stopwatch.Restart();

            _commandTimer.Start();
        }

        public void Stop()
        {
            _commandTimer.Stop();

            _remainingSimulationTicksForCommand = 0;
            SetCurrentLineIndex(-1);
            _stopwatch.Stop();
            _accumulatorMs = 0;
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
            if (!_stopwatch.IsRunning)
                return;

            // 0) Аккумулируем реальное время.
            var elapsedMs = _stopwatch.ElapsedMilliseconds;
            var deltaMs = Math.Max(0, elapsedMs - _lastElapsedMs);
            _lastElapsedMs = elapsedMs;
            _accumulatorMs += deltaMs;

            // Избегаем «спирали смерти», если окно было заморожено.
            if (_accumulatorMs > 1000.0)
                _accumulatorMs = 1000.0;

            // 1) Fixed-step симуляция: выполняем столько sim-tick'ов, сколько накопили.
            while (_accumulatorMs + 0.0001 >= SimTickIntervalMs)
            {
                if (!StepOneSimulationTick())
                    return;

                _accumulatorMs -= SimTickIntervalMs;
            }
        }

        private bool StepOneSimulationTick()
        {
            if (_model.IsGameOver)
            {
                Stop();
                GameUpdated?.Invoke(); // финальная перерисовка
                return false;
            }

            // Если командный тик закончился — берём следующую команду и сразу начинаем её симуляцию.
            if (_remainingSimulationTicksForCommand <= 0)
            {
                if (_commandQueue.Count == 0)
                {
                    Stop();
                    GameUpdated?.Invoke();
                    return false;
                }

                var command = _commandQueue.Dequeue();
                _commandTickCount++;
                SetCurrentLineIndex(command.LineIndex);
                command.Execute(_model);
                _remainingSimulationTicksForCommand = SimulationTicksPerCommand;
            }

            _model.StepSimulationTick();
            _remainingSimulationTicksForCommand--;
            LogicFrameCommitted?.Invoke();
            return true;
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