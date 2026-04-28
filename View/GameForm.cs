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
using CodeYourself.Levels;
using CodeYourself.Models;
using CodeYourself.Parsing;
using CodeYourself.Models.Obstacles;

namespace CodeYourself
{
    public partial class GameForm : Form
    {
        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private GameController _controller;
        private GameModel _model;
        private Panel _gamePanel;     // правое игровое поле
        private TextBox _codeEditor;  // левое поле (пока просто заглушка)
        private SplitContainer _splitContainer;
        private bool _splitterTouchedByUser;
        private readonly CommandParser _parser = new CommandParser();

        // Оставляем отрисовку простой и предсказуемой (как в предыдущей реализации),
        // но возвращаем render tick (~60Hz) для плавной перерисовки.
        private readonly Timer _renderTimer = new Timer();
        private long _renderTickCount;
        private readonly IGameLevel _level = new Week3Level();

        public GameForm(GameModel model, GameController controller)
        {
            _controller = controller;
            _model = model; 
            SetupUI();
            ApplyLevel();
            _controller.GameUpdated += Controller_GameUpdated;
            _controller.CurrentLineIndexChanged += Controller_CurrentLineIndexChanged;

            _renderTimer.Interval = 16; // ~60Hz
            _renderTimer.Tick += RenderTimer_Tick;
            _renderTimer.Start();

            FormClosed += GameForm_FormClosed;
        }

        private void ApplyLevel()
        {
            _level.Apply(_model);
            Text = $"Code Yourself - {_level.Name}";
        }

        private void Controller_CurrentLineIndexChanged(int lineIndex)
        {
            if (IsDisposed || _codeEditor == null || _codeEditor.IsDisposed)
                return;

            HighlightLine(lineIndex);
        }

        private void Controller_GameUpdated()
        {
            if (!IsDisposed && _gamePanel != null && !_gamePanel.IsDisposed)
                _gamePanel.Invalidate();

            // Если выполнение остановилось (win/lose/конец команд) — возвращаем редактор в редактируемое состояние.
            if (!IsDisposed && _codeEditor != null && !_codeEditor.IsDisposed && !_controller.IsRunning)
            {
                _codeEditor.ReadOnly = false;
            }
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || _gamePanel == null || _gamePanel.IsDisposed)
                return;

            _renderTickCount++;
            _gamePanel.Invalidate();
        }

        private void GameForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_controller != null)
            {
                _controller.GameUpdated -= Controller_GameUpdated;
                _controller.CurrentLineIndexChanged -= Controller_CurrentLineIndexChanged;
                _controller.Dispose();
                _controller = null;
            }

            _renderTimer.Tick -= RenderTimer_Tick;
            _renderTimer.Stop();
            _renderTimer.Dispose();
        }

        private void SetupUI()
        {
            this.Text = "Code Yourself - Неделя 3";
            this.Size = new Size(1400, 650);           // комфортный стартовый размер
            this.MinimumSize = new Size(1350, 600);    // теперь канвас 800px точно помещается в правую панель (60%)
            this.DoubleBuffered = true;

            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 8,
                IsSplitterFixed = false
            };
            _splitContainer.SplitterMoved += (s, e) => _splitterTouchedByUser = true;
            this.Controls.Add(_splitContainer);

            // ЛЕВАЯ ЧАСТЬ 
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            _splitContainer.Panel1.Controls.Add(leftPanel);

            _codeEditor = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 11),
                Text = "REPEAT 2\r\n  MOVE RIGHT 2\r\n  JUMP RIGHT\r\n  WAIT 1\r\nEND\r\n",
                ReadOnly = false,
                AcceptsTab = true,
                ScrollBars = ScrollBars.Vertical
            };
            leftPanel.Controls.Add(_codeEditor);

            // ПРАВАЯ ЧАСТЬ 
            _gamePanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 55)
            };
            _gamePanel.Paint += GamePanel_Paint;

            // при любом изменении размера панели сразу перерисовываем
            _gamePanel.Resize += (s, e) => _gamePanel.Invalidate();

            _splitContainer.Panel2.Controls.Add(_gamePanel);

            //  Кнопки 
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(40, 40, 40),
                FlowDirection = FlowDirection.LeftToRight,
                ForeColor = Color.White
            };
            _splitContainer.Panel2.Controls.Add(btnPanel);

            var btnRun = new Button { Text = "▶ Run", Width = 100, Height = 35, Margin = new Padding(10) };
            btnRun.Click += (s, e) => RunProgram();
            btnPanel.Controls.Add(btnRun);

            Load += (s, e) =>
            {
                if (!_splitterTouchedByUser)
                {
                    int totalWidth = ClientSize.Width;
                    _splitContainer.SplitterDistance = (int)(totalWidth * 0.4);
                }
                _gamePanel.Invalidate(); // сразу центрируем при запуске
            };
        }

        private void RunProgram()
        {
            _controller.Stop();
            _controller.ClearCommands();

            // Run всегда стартует "с начала": сбрасываем модель и заново применяем уровень.
            _model.Reset();
            ApplyLevel();
            _gamePanel.Invalidate();

            var result = _parser.Parse(_codeEditor.Text);
            if (!result.IsSuccess)
            {
                var msg = string.Join("\r\n", result.Errors.Select(e => $"Line {e.LineIndex + 1}: {e.Message}"));
                MessageBox.Show(this, msg, "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var cmd in result.Commands)
                _controller.EnqueueCommand(cmd);

            _codeEditor.ReadOnly = true;
            _controller.Start();
        }

        private void HighlightLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _codeEditor.Lines.Length)
            {
                _codeEditor.SelectionLength = 0;
                _codeEditor.ReadOnly = false;
                return;
            }

            var start = _codeEditor.GetFirstCharIndexFromLine(lineIndex);
            if (start < 0)
                return;

            var length = _codeEditor.Lines[lineIndex].Length;

            _codeEditor.SelectionStart = start;
            _codeEditor.SelectionLength = length;
            _codeEditor.ScrollToCaret();
            _codeEditor.Focus();
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var player = _model.Player;

            // === Центрируем виртуальное поле внутри _gamePanel ===
            int offsetX = (_gamePanel.Width - GameModel.CanvasWidth) / 2;
            int offsetY = (_gamePanel.Height - GameModel.CanvasHeight) / 2;

            // Смещаем систему координат
            g.TranslateTransform(offsetX, offsetY);

            // Рисуем землю (только в пределах виртуального поля)
            g.FillRectangle(Brushes.DarkSlateGray, 
                            0, GameModel.GroundY, 
                            GameModel.CanvasWidth, GameModel.GroundHeight);

            DrawObstacles(g);
            DrawFinish(g);

            // Персонаж
            DrawPlayer(g, player);

            // Подпись Player
            using (var font = new Font("Arial", 12, FontStyle.Bold))
                g.DrawString("Player", font, Brushes.White,
                             player.Position.X + 8, player.Position.Y - 25);

            // Отладка (явно разделяем командные и симуляционные тики)
            using (var font = new Font("Consolas", 10))
                g.DrawString($"CommandTick: {_controller.CommandTickCount} | SimTick: {_model.SimTickCount} | RenderTick: {_renderTickCount} | Canvas: {GameModel.CanvasWidth}x{GameModel.CanvasHeight}",
                             font, Brushes.Yellow, 20, 20);

            DrawEndOverlay(g);

            // Сбрасываем трансформацию (чтобы кнопки не сдвинулись)
            g.ResetTransform();

            // Командные тики (1 тик = 1 команда), в экранных координатах.
            using (var font = new Font("Consolas", 10))
                g.DrawString($"CommandTick: {_controller.CommandTickCount} | RenderTick: {_renderTickCount}", font, Brushes.Yellow, 10, 10);
        }

        private void DrawObstacles(Graphics g)
        {
            foreach (var obstacle in _model.Obstacles)
            {
                Brush brush = Brushes.OrangeRed;
                if (obstacle.Kind == ObstacleKind.MovingPlatform || obstacle.Kind == ObstacleKind.StaticPlatform)
                    brush = Brushes.SteelBlue;

                var r = obstacle.Bounds;
                g.FillRectangle(brush, r.X, r.Y, r.Width, r.Height);
            }
        }

        private void DrawFinish(Graphics g)
        {
            if (!_model.FinishZone.HasValue)
                return;

            var r = _model.FinishZone.Value;
            using (var fill = new SolidBrush(Color.FromArgb(90, 120, 200, 255)))
                g.FillRectangle(fill, r);
            using (var pen = new Pen(Color.FromArgb(220, 120, 200, 255), 3))
                g.DrawRectangle(pen, r);
            using (var font = new Font("Arial", 10, FontStyle.Bold))
                g.DrawString("EXIT", font, Brushes.White, r.X - 8, r.Y - 18);
        }

        private static void DrawPlayer(Graphics g, Player player)
        {
            g.FillRectangle(Brushes.LimeGreen,
                player.Position.X,
                player.Position.Y,
                player.Size,
                player.Size);
        }

        private void DrawEndOverlay(Graphics g)
        {
            if (_model.EndState == GameEndState.Running)
                return;

            using (var font = new Font("Arial", 18, FontStyle.Bold))
            {
                var text = _model.EndState == GameEndState.Won ? "YOU WIN" : "GAME OVER";
                var brush = _model.EndState == GameEndState.Won ? Brushes.LawnGreen : Brushes.Red;
                g.DrawString(text, font, brush, 20, 50);
            }

            if (!string.IsNullOrWhiteSpace(_model.EndReason))
            {
                using (var font = new Font("Consolas", 10))
                    g.DrawString(_model.EndReason, font, Brushes.White, 20, 80);
            }
        }
    }
}
