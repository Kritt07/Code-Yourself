using CodeYourself.Commands.Base;
using CodeYourself.Models;
using System;
using System.Collections.Generic;

namespace CodeYourself.Controllers
{
    public sealed class GameController : IDisposable
    {
        private readonly Queue<GameCommand> _commandQueue = new Queue<GameCommand>();

        private readonly GameModel _model;
        private readonly System.Windows.Forms.Timer _tickTimer;

        public event Action GameUpdated; // событие для перерисовки

        public GameController(GameModel model)
        {
            _model = model;

            _tickTimer = new System.Windows.Forms.Timer();
            _tickTimer.Interval = 500; // 500 мс = 2 тика в секунду (можно потом поставить 1000)
            _tickTimer.Tick += TickTimer_Tick;
        }

        public void Start()
        {
            _tickTimer.Start();
        }

        public void Stop()
        {
            _tickTimer.Stop();
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
            if (_commandQueue.Count > 0)
            {
                var command = _commandQueue.Dequeue();
                command.Execute(_model);
            }
              
            _model.Update();
            GameUpdated?.Invoke(); // говорим View, что нужно перерисоваться
        }

        public GameModel Model => _model;

        public void Dispose()
        {
            Stop();
            _tickTimer.Tick -= TickTimer_Tick;
            _tickTimer.Dispose();
        }
    }
}