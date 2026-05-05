using System;
using System.Drawing;
using System.Windows.Forms;
using CodeYourself.Levels;
using CodeYourself.View.Controls;
using CodeYourself.View.Rendering;

namespace CodeYourself.View.Screens
{
    public sealed class LevelSelectScreen : UserControl
    {
        private readonly NeonTheme _theme;

        public event EventHandler BackRequested;
        public event EventHandler<IGameLevel> LevelSelected;

        public LevelSelectScreen(NeonTheme theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            BackColor = _theme.PanelBackground;
            DoubleBuffered = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.PanelBackground,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(24),
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            Controls.Add(root);

            var title = new Label
            {
                Text = "ВЫБОР УРОВНЯ",
                ForeColor = _theme.TextPrimary,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 24f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10)
            };
            root.Controls.Add(title, 0, 0);

            var grid = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Anchor = AnchorStyles.Top,
                BackColor = _theme.PanelBackground,
                Margin = new Padding(0, 10, 0, 10),
                Padding = new Padding(0),
            };
            root.Controls.Add(grid, 0, 2);

            // Пока показываем только 1 уровень.
            var level1 = new NeonButton(_theme)
            {
                Text = "УРОВЕНЬ 1",
                Width = 220,
                Height = 54,
                Margin = new Padding(0, 0, 14, 14)
            };
            level1.Click += (_, __) => LevelSelected?.Invoke(this, new Week3Level());
            grid.Controls.Add(level1);

            var back = new NeonButton(_theme)
            {
                Text = "НАЗАД",
                Width = 160,
                Height = 48,
                Margin = new Padding(0, 12, 0, 0)
            };
            back.Click += (_, __) => BackRequested?.Invoke(this, EventArgs.Empty);
            root.Controls.Add(back, 0, 4);
        }
    }
}

