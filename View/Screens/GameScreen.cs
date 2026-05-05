using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CodeYourself.Controllers;
using CodeYourself.Levels;
using CodeYourself.Models;
using CodeYourself.Parsing;
using CodeYourself.View.Controls;
using CodeYourself.View.Rendering;

namespace CodeYourself.View.Screens
{
    public sealed class GameScreen : UserControl
    {
        private enum GameOverlayMode
        {
            None,
            Pause,
            Defeat,
            Victory
        }

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
        private Button _pauseButton;

        private Panel _overlayPanel;
        private Label _overlayTitle;
        private Label _overlaySubtitle;
        private NeonButton _btnContinue;
        private NeonButton _btnMainMenu;
        private NeonButton _btnRestart;
        private NeonButton _btnNextLevel;
        private GameOverlayMode _overlayMode = GameOverlayMode.None;

        private readonly Timer _renderTimer = new Timer();
        private long _renderTickCount;

        private readonly NeonTheme _theme;
        private readonly GameRenderer _renderer;

        public event EventHandler MainMenuRequested;
        public event EventHandler<IGameLevel> NextLevelRequested;

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

            if (!IsDisposed && _codeEditor != null && !_codeEditor.IsDisposed)
            {
                var lockEditor = _controller.IsRunning || _controller.IsSimulationPaused;
                if (!lockEditor)
                    _codeEditor.ReadOnly = false;
            }

            SyncEndStateOverlay();
        }

        private void SyncEndStateOverlay()
        {
            if (_overlayPanel == null || _overlayPanel.IsDisposed)
                return;

            if (_model.EndState == GameEndState.Won)
            {
                _overlayMode = GameOverlayMode.Victory;
                _overlayPanel.Visible = true;
                SetRenderTimerEnabled(false);
                ApplyOverlayContent();
                UpdateCornerButtonsEnabled();
                BringGamePanelControlsToFront();
                return;
            }

            if (_model.EndState == GameEndState.Lost)
            {
                _overlayMode = GameOverlayMode.Defeat;
                _overlayPanel.Visible = true;
                SetRenderTimerEnabled(false);
                ApplyOverlayContent();
                UpdateCornerButtonsEnabled();
                BringGamePanelControlsToFront();
            }
        }

        private void SetRenderTimerEnabled(bool enabled)
        {
            if (IsDisposed)
                return;

            if (enabled)
            {
                if (!_renderTimer.Enabled)
                    _renderTimer.Start();
            }
            else
            {
                if (_renderTimer.Enabled)
                    _renderTimer.Stop();
            }
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || _gamePanel == null || _gamePanel.IsDisposed)
                return;

            // Во время меню (пауза/победа/поражение) не делаем 60Hz Invalidate:
            // полупрозрачный оверлей в WinForms начинает мерцать при постоянной перерисовке фона.
            if (_overlayPanel != null && !_overlayPanel.IsDisposed && _overlayPanel.Visible)
                return;

            if (!_controller.IsSimulationPaused)
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
                Text = "",
                ReadOnly = false,
                AcceptsTab = true,
                ScrollBars = ScrollBars.Vertical
            };
            leftPanel.Controls.Add(_codeEditor);

            _gamePanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.PanelBackground
            };
            _gamePanel.Paint += GamePanel_Paint;
            _gamePanel.Resize += (s, e) =>
            {
                UpdateCanvasCornerButtonsLayout();
                _gamePanel.Invalidate();
            };
            _splitContainer.Panel2.Controls.Add(_gamePanel);

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
            _restartButton.Click += (_, __) => RestartLevel();
            _gamePanel.Controls.Add(_restartButton);

            _pauseButton = new Button
            {
                Text = "⏸",
                Width = 36,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.ButtonBackground,
                ForeColor = _theme.TextPrimary
            };
            _pauseButton.FlatAppearance.BorderColor = _theme.ButtonBorder;
            _pauseButton.FlatAppearance.BorderSize = 1;
            _pauseButton.Click += (_, __) => OnPauseButtonClick();
            _gamePanel.Controls.Add(_pauseButton);

            BuildOverlay();
            UpdateCanvasCornerButtonsLayout();

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
            btnRun.Click += (_, __) => RunProgram();
            btnPanel.Controls.Add(btnRun);

            HandleCreated += (_, __) =>
            {
                if (_splitContainer == null) return;

                if (!_splitterTouchedByUser)
                {
                    int totalWidth = ClientSize.Width;
                    _splitContainer.SplitterDistance = (int)(totalWidth * 0.4);
                }
                UpdateCanvasCornerButtonsLayout();
                _gamePanel?.Invalidate();
            };
        }

        private void BuildOverlay()
        {
            _overlayPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                // В WinForms полупрозрачные панели поверх активного рендера часто мерцают.
                // Для стабильности делаем фон оверлея непрозрачным.
                BackColor = Color.FromArgb(18, 16, 26)
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            _overlayPanel.Controls.Add(root);

            var center = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(16),
                Margin = new Padding(0)
            };

            _overlayTitle = new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
                ForeColor = _theme.TextPrimary,
                Margin = new Padding(0, 0, 0, 8),
                MinimumSize = new Size(280, 0)
            };

            _overlaySubtitle = new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Consolas", 10f),
                ForeColor = _theme.TextAccent,
                Margin = new Padding(0, 0, 0, 16),
                MaximumSize = new Size(420, 0),
                Visible = false
            };

            _btnContinue = CreateNeonButton("Продолжить");
            _btnContinue.Click += (_, __) => OnContinuePause();

            _btnRestart = CreateNeonButton("Рестарт");
            _btnRestart.Click += (_, __) => RestartLevel();

            _btnMainMenu = CreateNeonButton("В меню");
            _btnMainMenu.Click += (_, __) => GoToMainMenu();

            _btnNextLevel = CreateNeonButton("Следующий уровень");
            _btnNextLevel.Click += (_, __) => OnNextLevel();

            center.Controls.Add(_overlayTitle);
            center.Controls.Add(_overlaySubtitle);
            center.Controls.Add(_btnContinue);
            center.Controls.Add(_btnRestart);
            center.Controls.Add(_btnMainMenu);
            center.Controls.Add(_btnNextLevel);

            root.Controls.Add(center, 1, 1);

            _gamePanel.Controls.Add(_overlayPanel);
            BringGamePanelControlsToFront();
        }

        private NeonButton CreateNeonButton(string text)
        {
            var b = new NeonButton(_theme)
            {
                Text = text,
                Width = 240,
                Height = 48,
                Margin = new Padding(0, 0, 0, 10)
            };
            return b;
        }

        private void ApplyOverlayContent()
        {
            _btnContinue.Visible = _overlayMode == GameOverlayMode.Pause;
            _btnRestart.Visible = _overlayMode == GameOverlayMode.Defeat || _overlayMode == GameOverlayMode.Victory;
            _btnMainMenu.Visible = true;
            _btnMainMenu.Text = _overlayMode == GameOverlayMode.Pause ? "В главное меню" : "В меню";
            _btnNextLevel.Visible = _overlayMode == GameOverlayMode.Victory && LevelCatalog.TryGetNext(_level, out _);

            switch (_overlayMode)
            {
                case GameOverlayMode.Pause:
                    _overlayTitle.Text = "ПАУЗА";
                    _overlaySubtitle.Visible = false;
                    break;
                case GameOverlayMode.Defeat:
                    _overlayTitle.Text = "ПОРАЖЕНИЕ";
                    if (!string.IsNullOrWhiteSpace(_model.EndReason))
                    {
                        _overlaySubtitle.Text = _model.EndReason;
                        _overlaySubtitle.Visible = true;
                    }
                    else
                    {
                        _overlaySubtitle.Visible = false;
                    }
                    break;
                case GameOverlayMode.Victory:
                    _overlayTitle.Text = "ПОБЕДА";
                    _overlaySubtitle.Visible = false;
                    break;
            }
        }

        private void OnPauseButtonClick()
        {
            if (_model.EndState != GameEndState.Running)
                return;

            _controller.PauseSimulation();
            _overlayMode = GameOverlayMode.Pause;
            _overlayPanel.Visible = true;
            SetRenderTimerEnabled(false);
            ApplyOverlayContent();
            UpdateCornerButtonsEnabled();
            BringGamePanelControlsToFront();
            _gamePanel.Invalidate();
        }

        private void OnContinuePause()
        {
            if (_overlayMode != GameOverlayMode.Pause)
                return;

            _overlayMode = GameOverlayMode.None;
            _overlayPanel.Visible = false;
            _controller.ResumeSimulation();
            SetRenderTimerEnabled(true);
            UpdateCornerButtonsEnabled();
            BringGamePanelControlsToFront();
            _gamePanel.Invalidate();
            _codeEditor?.Focus();
        }

        private void GoToMainMenu()
        {
            _controller.Stop();
            MainMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnNextLevel()
        {
            if (!LevelCatalog.TryGetNext(_level, out var next))
                return;

            NextLevelRequested?.Invoke(this, next);
        }

        private void HideOverlay()
        {
            _overlayMode = GameOverlayMode.None;
            if (_overlayPanel != null && !_overlayPanel.IsDisposed)
                _overlayPanel.Visible = false;
            SetRenderTimerEnabled(true);
            UpdateCornerButtonsEnabled();
            BringGamePanelControlsToFront();
        }

        private void UpdateCornerButtonsEnabled()
        {
            if (_pauseButton == null || _pauseButton.IsDisposed)
                return;

            _pauseButton.Enabled = _model.EndState == GameEndState.Running;
        }

        private void RestartLevel()
        {
            _controller.Stop();
            _controller.ClearCommands();

            _model.Reset();
            ApplyLevel();

            HideOverlay();

            if (_codeEditor != null && !_codeEditor.IsDisposed)
                _codeEditor.ReadOnly = false;

            HighlightLine(-1);
            UpdateCanvasCornerButtonsLayout();
            _gamePanel.Invalidate();
        }

        private void BringGamePanelControlsToFront()
        {
            if (_gamePanel == null || _gamePanel.IsDisposed)
                return;

            if (_overlayPanel != null && !_overlayPanel.IsDisposed && _overlayPanel.Visible)
                _overlayPanel.BringToFront();
            else
            {
                _restartButton?.BringToFront();
                _pauseButton?.BringToFront();
            }
        }

        private void UpdateCanvasCornerButtonsLayout()
        {
            if (_restartButton == null || _gamePanel == null || _gamePanel.IsDisposed)
                return;

            int offsetX = (_gamePanel.Width - GameModel.CanvasWidth) / 2;
            int offsetY = (_gamePanel.Height - GameModel.CanvasHeight) / 2;

            const int margin = 10;
            const int gap = 8;

            int ry = offsetY + margin;
            int restartRight = offsetX + GameModel.CanvasWidth - margin;
            int rx = restartRight - _restartButton.Width;
            int px = rx - gap - _pauseButton.Width;

            _restartButton.Location = new Point(Math.Max(0, rx), Math.Max(0, ry));
            _pauseButton.Location = new Point(Math.Max(0, px), Math.Max(0, ry));
            BringGamePanelControlsToFront();
        }

        private void RunProgram()
        {
            _controller.Stop();
            _controller.ClearCommands();

            HideOverlay();

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
            UpdateCornerButtonsEnabled();
            _controller.Start();
        }

        private void HighlightLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _codeEditor.Lines.Length)
            {
                _codeEditor.SelectionLength = 0;
                if (!_controller.IsRunning && !_controller.IsSimulationPaused)
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
