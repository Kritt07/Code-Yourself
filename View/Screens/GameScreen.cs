using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CodeYourself.Controllers;
using CodeYourself.Levels;
using CodeYourself.Models;
using CodeYourself.Parsing;
using CodeYourself.View.Rendering;

namespace CodeYourself.View.Screens
{
    public sealed class GameScreen : UserControl
    {
        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private readonly Action<string> _setWindowTitle;

        private GameController _controller;
        private GameModel _model;
        private readonly IGameLevel _level;

        private Panel _gamePanel;
        private TextBox _codeEditor;
        private SplitContainer _splitContainer;
        private bool _splitterTouchedByUser;
        private readonly CommandParser _parser = new CommandParser();
        private Button _restartButton;

        private readonly Timer _renderTimer = new Timer();
        private long _renderTickCount;

        private readonly NeonTheme _theme;
        private readonly GameRenderer _renderer;

        public GameScreen(NeonTheme theme, GameModel model, GameController controller, IGameLevel level, Action<string> setWindowTitle)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
            _renderer = new GameRenderer(_theme);

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _setWindowTitle = setWindowTitle ?? (_ => { });

            BackColor = _theme.PanelBackground;
            DoubleBuffered = true;

            SetupUI();
            ApplyLevel();

            _controller.GameUpdated += Controller_GameUpdated;
            _controller.CurrentLineIndexChanged += Controller_CurrentLineIndexChanged;

            _renderTimer.Interval = 16; // ~60Hz
            _renderTimer.Tick += RenderTimer_Tick;
            _renderTimer.Start();
        }

        private void ApplyLevel()
        {
            _level.Apply(_model);
            _setWindowTitle($"Code Yourself - {_level.Name}");
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

            if (!IsDisposed && _codeEditor != null && !_codeEditor.IsDisposed && !_controller.IsRunning)
                _codeEditor.ReadOnly = false;
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || _gamePanel == null || _gamePanel.IsDisposed)
                return;

            _renderTickCount++;
            _gamePanel.Invalidate();
        }

        private void SetupUI()
        {
            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 8,
                IsSplitterFixed = false
            };
            _splitContainer.SplitterMoved += (s, e) => _splitterTouchedByUser = true;
            Controls.Add(_splitContainer);

            // ЛЕВАЯ ЧАСТЬ
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.UiPanelBackground
            };
            _splitContainer.Panel1.Controls.Add(leftPanel);

            _codeEditor = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BackColor = _theme.EditorBackground,
                ForeColor = _theme.EditorForeground,
                Font = new Font("Consolas", 11),
                Text = "WAIT 9\r\nJUMP RIGHT 1\r\nMOVE RIGHT 2\r\nWAIT 6\r\nJUMP RIGHT 2\r\nMOVE RIGHT 3\r\nJUMP RIGHT 2\r\n",
                ReadOnly = false,
                AcceptsTab = true,
                ScrollBars = ScrollBars.Vertical
            };
            leftPanel.Controls.Add(_codeEditor);

            // ПРАВАЯ ЧАСТЬ
            _gamePanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.PanelBackground
            };
            _gamePanel.Paint += GamePanel_Paint;
            _gamePanel.Resize += (s, e) =>
            {
                UpdateRestartButtonLayout();
                _gamePanel.Invalidate();
            };
            _splitContainer.Panel2.Controls.Add(_gamePanel);

            // Кнопка рестарта внутри канваса
            _restartButton = new Button
            {
                Text = "⟲ Restart",
                Width = 90,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.ButtonBackground,
                ForeColor = _theme.TextPrimary
            };
            _restartButton.FlatAppearance.BorderColor = _theme.ButtonBorder;
            _restartButton.FlatAppearance.BorderSize = 1;
            _restartButton.Click += (s, e) => RestartLevel();
            _gamePanel.Controls.Add(_restartButton);
            UpdateRestartButtonLayout();

            // Панель кнопок справа снизу (Run)
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = _theme.UiPanelBackground,
                FlowDirection = FlowDirection.LeftToRight,
                ForeColor = _theme.TextPrimary
            };
            _splitContainer.Panel2.Controls.Add(btnPanel);

            var btnRun = new Button { Text = "▶ Run", Width = 100, Height = 35, Margin = new Padding(10) };
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.BackColor = _theme.ButtonBackground;
            btnRun.ForeColor = _theme.TextPrimary;
            btnRun.FlatAppearance.BorderColor = _theme.ButtonBorder;
            btnRun.FlatAppearance.BorderSize = 1;
            btnRun.Click += (s, e) => RunProgram();
            btnPanel.Controls.Add(btnRun);

            // Layout after handle is created
            HandleCreated += (_, __) =>
            {
                if (_splitContainer == null) return;

                if (!_splitterTouchedByUser)
                {
                    int totalWidth = ClientSize.Width;
                    _splitContainer.SplitterDistance = (int)(totalWidth * 0.4);
                }
                UpdateRestartButtonLayout();
                _gamePanel?.Invalidate();
            };
        }

        private void RestartLevel()
        {
            _controller.Stop();
            _controller.ClearCommands();

            _model.Reset();
            ApplyLevel();

            if (_codeEditor != null && !_codeEditor.IsDisposed)
                _codeEditor.ReadOnly = false;

            HighlightLine(-1);
            UpdateRestartButtonLayout();
            _gamePanel.Invalidate();
        }

        private void UpdateRestartButtonLayout()
        {
            if (_restartButton == null || _gamePanel == null || _gamePanel.IsDisposed)
                return;

            int offsetX = (_gamePanel.Width - GameModel.CanvasWidth) / 2;
            int offsetY = (_gamePanel.Height - GameModel.CanvasHeight) / 2;

            const int margin = 10;
            var x = offsetX + GameModel.CanvasWidth - _restartButton.Width - margin;
            var y = offsetY + margin;

            _restartButton.Location = new Point(Math.Max(0, x), Math.Max(0, y));
            _restartButton.BringToFront();
        }

        private void RunProgram()
        {
            _controller.Stop();
            _controller.ClearCommands();

            _model.Reset();
            ApplyLevel();
            _gamePanel.Invalidate();

            var result = _parser.Parse(_codeEditor.Text);
            if (!result.IsSuccess)
            {
                var msg = string.Join("\r\n", result.Errors.Select(e => $"Line {e.LineIndex + 1}: {e.Message}"));
                var owner = FindForm();
                if (owner != null)
                    MessageBox.Show(owner, msg, "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show(msg, "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            _renderer.Render(
                e.Graphics,
                _model,
                _controller,
                renderTickCount: _renderTickCount,
                panelSize: _gamePanel.ClientSize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
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

                _renderer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

