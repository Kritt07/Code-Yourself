using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeYourself.Controllers;
using CodeYourself.Models;

namespace CodeYourself
{
    public partial class GameFrom : Form
    {
        private GameController _controller;
        private Panel _gamePanel;     // правое игровое поле
        private TextBox _codeEditor;  // левое поле (пока просто заглушка)

        public GameFrom()
        {
            InitializeComponent();
            SetupUI();
            InitializeMVC();
        }

        private void SetupUI()
        {
            this.Text = "Code Yourself - Неделя 1";
            this.Size = new Size(1400, 650);           // комфортный стартовый размер
            this.MinimumSize = new Size(1350, 600);    // теперь канвас 800px точно помещается в правую панель (60%)

            // === SplitContainer ===
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 8,
                IsSplitterFixed = false
            };
            this.Controls.Add(splitContainer);

            // === ЛЕВАЯ ЧАСТЬ (редактор кода) ===
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            splitContainer.Panel1.Controls.Add(leftPanel);

            _codeEditor = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 11),
                Text = "write your code here\r\n(пока не используется — неделя 1)",
                ReadOnly = true
            };
            leftPanel.Controls.Add(_codeEditor);

            // === ПРАВАЯ ЧАСТЬ (игровое поле) ===
            _gamePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 55)
            };
            _gamePanel.Paint += GamePanel_Paint;

            // ← НОВОЕ: при любом изменении размера панели сразу перерисовываем
            _gamePanel.Resize += (s, e) => _gamePanel.Invalidate();

            splitContainer.Panel2.Controls.Add(_gamePanel);

            // === Кнопки ===
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(40, 40, 40),
                FlowDirection = FlowDirection.LeftToRight,
                ForeColor = Color.White
            };
            splitContainer.Panel2.Controls.Add(btnPanel);

            var btnRun = new Button { Text = "▶ Run", Width = 100, Height = 35, Margin = new Padding(10) };
            var btnReset = new Button { Text = "⟳ Reset", Width = 100, Height = 35, Margin = new Padding(10) };

            btnRun.Click += (s, e) => _controller.Start();
            btnReset.Click += (s, e) => _controller.Reset();

            btnPanel.Controls.Add(btnRun);
            btnPanel.Controls.Add(btnReset);

            // === ДИНАМИЧЕСКОЕ СОотношение 4:6 + принудительная перерисовка ===
            this.Load += (s, e) =>
            {
                int totalWidth = this.ClientSize.Width;
                splitContainer.SplitterDistance = (int)(totalWidth * 0.4);
                _gamePanel.Invalidate(); // сразу центрируем при запуске
            };

            this.Resize += (s, e) =>
            {
                if (splitContainer != null)
                {
                    int totalWidth = this.ClientSize.Width;
                    splitContainer.SplitterDistance = (int)(totalWidth * 0.4);
                    _gamePanel.Invalidate(); // ВАЖНО: перерисовка при изменении высоты/ширины
                }
            };
        }

        private void InitializeMVC()
        {
            var model = new GameModel();
            _controller = new GameController(model);
            _controller.GameUpdated += () => _gamePanel.Invalidate(); // перерисовка
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var model = _controller.Model;

            // === Центрируем виртуальное поле внутри _gamePanel ===
            int offsetX = (_gamePanel.Width - GameModel.CanvasWidth) / 2;
            int offsetY = (_gamePanel.Height - GameModel.CanvasHeight) / 2;

            // Смещаем систему координат
            g.TranslateTransform(offsetX, offsetY);

            // Рисуем землю (только в пределах виртуального поля)
            g.FillRectangle(Brushes.DarkSlateGray, 
                            0, GameModel.CanvasHeight - 50, 
                            GameModel.CanvasWidth, 50);

            // Персонаж
            g.FillRectangle(Brushes.LimeGreen, 
                            model.PlayerPosition.X, 
                            model.PlayerPosition.Y, 
                            50, 50);

            // Подпись Player
            using (var font = new Font("Arial", 12, FontStyle.Bold))
                g.DrawString("Player", font, Brushes.White, 
                             model.PlayerPosition.X + 8, model.PlayerPosition.Y - 25);

            // Отладка
            using (var font = new Font("Consolas", 10))
                g.DrawString($"Tick: {model.TickCount} | Canvas: {GameModel.CanvasWidth}x{GameModel.CanvasHeight}", 
                             font, Brushes.Yellow, 20, 20);

            // Сбрасываем трансформацию (чтобы кнопки не сдвинулись)
            g.ResetTransform();
        }
    }
}