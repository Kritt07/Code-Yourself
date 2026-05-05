using System;
using System.Drawing;
using System.Windows.Forms;
using CodeYourself.View.Controls;
using CodeYourself.View.Rendering;

namespace CodeYourself.View.Screens
{
    public sealed class MainMenuScreen : UserControl
    {
        private readonly NeonTheme _theme;

        public event EventHandler PlayRequested;
        public event EventHandler ExitRequested;

        public MainMenuScreen(NeonTheme theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            BackColor = _theme.PanelBackground;
            DoubleBuffered = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.PanelBackground,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(24),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            Controls.Add(root);

            var title = new Label
            {
                Text = "CODE YOURSELF",
                ForeColor = _theme.TextPrimary,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 32f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10)
            };
            root.Controls.Add(title, 0, 1);

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Anchor = AnchorStyles.Top,
                BackColor = _theme.PanelBackground,
                Margin = new Padding(0, 10, 0, 0),
                Padding = new Padding(0),
            };
            root.Controls.Add(buttons, 0, 3);

            var play = new NeonButton(_theme)
            {
                Text = "ИГРАТЬ",
                Width = 280,
                Height = 54,
                Margin = new Padding(0, 0, 0, 14)
            };
            play.Click += (_, __) => PlayRequested?.Invoke(this, EventArgs.Empty);
            buttons.Controls.Add(play);

            var commands = new NeonButton(_theme)
            {
                Text = "КОМАНДЫ",
                Width = 280,
                Height = 54,
                Margin = new Padding(0, 0, 0, 14)
            };
            commands.Click += (_, __) =>
            {
                var help =
                    "Доступные команды:\r\n" +
                    "\r\n" +
                    "MOVE LEFT|RIGHT [n]\r\n" +
                    "  - шаг на 1 клетку (если n не задан — 1)\r\n" +
                    "\r\n" +
                    "JUMP LEFT|RIGHT n\r\n" +
                    "  - прыжок на n клеток (n = 1..3)\r\n" +
                    "\r\n" +
                    "WAIT [n]\r\n" +
                    "  - подождать n тиков (если n не задан — 1)\r\n" +
                    "\r\n" +
                    "REPEAT n\r\n" +
                    "  <команды>\r\n" +
                    "END\r\n" +
                    "  - повторить блок n раз\r\n" +
                    "\r\n" +
                    "Пример:\r\n" +
                    "REPEAT 3\r\n" +
                    "  MOVE RIGHT 1\r\n" +
                    "  JUMP RIGHT 1\r\n" +
                    "END\r\n";

                var owner = FindForm();
                if (owner != null)
                    MessageBox.Show(owner, help, "Список команд", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(help, "Список команд", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            buttons.Controls.Add(commands);

            var exit = new NeonButton(_theme)
            {
                Text = "ВЫХОД",
                Width = 280,
                Height = 54,
                Margin = new Padding(0, 0, 0, 0)
            };
            exit.Click += (_, __) => ExitRequested?.Invoke(this, EventArgs.Empty);
            buttons.Controls.Add(exit);
        }
    }
}

