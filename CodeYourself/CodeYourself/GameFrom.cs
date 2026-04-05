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
            this.Size = new Size(300, 550);
            this.MinimumSize = new Size(950, 550);

            // SplitContainer — делит форму на левую и правую часть
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 10,
                SplitterDistance = 350,
                IsSplitterFixed = false
            };
            this.Controls.Add(splitContainer);

            // === ЛЕВАЯ ЧАСТЬ (редактор кода) ===
            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };
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
                BackColor = Color.FromArgb(50, 50, 60)
            };
            _gamePanel.Paint += GamePanel_Paint;
            splitContainer.Panel2.Controls.Add(_gamePanel);

            // Кнопки управления
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(40, 40, 40),
                FlowDirection = FlowDirection.LeftToRight
            };
            splitContainer.Panel2.Controls.Add(btnPanel);

            var btnRun = new Button { Text = "▶ Run", Width = 100, Height = 35, Margin = new Padding(10) };
            var btnReset = new Button { Text = "⟳ Reset", Width = 100, Height = 35, Margin = new Padding(10) };

            btnRun.Click += (s, e) => _controller.Start();
            btnReset.Click += (s, e) => _controller.Reset();

            btnPanel.Controls.Add(btnRun);
            btnPanel.Controls.Add(btnReset);
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

            // Платформа (земля)
            g.FillRectangle(Brushes.DarkSlateGray, 0, 350, _gamePanel.Width, _gamePanel.Height - 350);

            // Персонаж — зелёный квадрат
            g.FillRectangle(Brushes.LimeGreen,
                model.PlayerPosition.X,
                model.PlayerPosition.Y,
                50, 50);

            // Подпись Player (как в макете)
            using (var font = new Font("Arial", 12, FontStyle.Bold))
            {
                g.DrawString("Player", font, Brushes.White,
                    model.PlayerPosition.X + 8, model.PlayerPosition.Y - 25);
            }

            // Информация о тиках (для отладки)
            using (var font = new Font("Consolas", 10))
            {
                g.DrawString($"Tick: {model.TickCount}", font, Brushes.Yellow, 20, 20);
            }
        }
    }
}