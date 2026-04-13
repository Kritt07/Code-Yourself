using CodeYourself.Models;
using System;

namespace CodeYourself.Controllers
{
    public class GameController
    {
        private readonly GameModel _model;
        private System.Windows.Forms.Timer _tickTimer;

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

        private void TickTimer_Tick(object sender, EventArgs e)
        {
            _model.Update();
            _model.Player.MovePlayer(MoveDirection.Right); // для теста двигаем персонажа вправо каждый тик
            GameUpdated?.Invoke(); // говорим View, что нужно перерисоваться
        }

        public GameModel Model => _model;
    }
}