using System;
using System.Drawing;
using System.Windows.Forms;
using CodeYourself.Levels;
using CodeYourself.Controllers;
using CodeYourself.Models;
using CodeYourself.View.Rendering;
using CodeYourself.View.Screens;

namespace CodeYourself.View
{
    public sealed class AppForm : Form
    {
        private readonly Panel _screenHost;
        private readonly NeonTheme _theme;

        private readonly MainMenuScreen _mainMenu;
        private readonly LevelSelectScreen _levelSelect;

        private Control _currentScreen;
        private GameScreen _gameScreen;

        public AppForm()
        {
            _theme = new NeonTheme();

            Text = "Code Yourself";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1400, 650);
            MinimumSize = new Size(1100, 600);
            BackColor = _theme.PanelBackground;
            DoubleBuffered = true;

            _screenHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.PanelBackground
            };
            Controls.Add(_screenHost);

            _mainMenu = new MainMenuScreen(_theme);
            _mainMenu.PlayRequested += (_, __) => ShowLevelSelect();
            _mainMenu.ExitRequested += (_, __) => Close();

            _levelSelect = new LevelSelectScreen(_theme);
            _levelSelect.BackRequested += (_, __) => ShowMainMenu();
            _levelSelect.LevelSelected += (_, level) => StartLevel(level);

            ShowMainMenu();

            FormClosed += (_, __) =>
            {
                DisposeCurrentScreen();
                _theme.Dispose();
            };
        }

        public void ShowMainMenu()
        {
            Text = "Code Yourself";
            SetScreen(_mainMenu);
        }

        public void ShowLevelSelect()
        {
            Text = "Code Yourself - Выбор уровня";
            SetScreen(_levelSelect);
        }

        public void StartLevel(IGameLevel level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));

            DisposeGameScreen();

            var model = new GameModel();
            var controller = new GameController(model);
            _gameScreen = new GameScreen(_theme, model, controller, level, setWindowTitle: t => Text = t);

            SetScreen(_gameScreen);
        }

        private void SetScreen(Control screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            if (ReferenceEquals(_currentScreen, screen))
                return;

            DisposeCurrentScreenIfTransient();

            _screenHost.Controls.Clear();
            _currentScreen = screen;
            screen.Dock = DockStyle.Fill;
            _screenHost.Controls.Add(screen);
            screen.BringToFront();
        }

        private void DisposeCurrentScreenIfTransient()
        {
            if (_currentScreen == null)
                return;

            // Меню и выбор уровня переиспользуются. Игровой экран пересоздаётся на старт уровня.
            if (_currentScreen is GameScreen)
            {
                DisposeGameScreen();
            }
        }

        private void DisposeCurrentScreen()
        {
            DisposeGameScreen();
            _currentScreen = null;
            _screenHost.Controls.Clear();
        }

        private void DisposeGameScreen()
        {
            if (_gameScreen == null)
                return;

            if (_screenHost.Controls.Contains(_gameScreen))
                _screenHost.Controls.Remove(_gameScreen);

            _gameScreen.Dispose();
            _gameScreen = null;
        }
    }
}

