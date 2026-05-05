using System;
using System.Drawing;
using System.Windows.Forms;
using CodeYourself.View.Rendering;

namespace CodeYourself.View.Controls
{
    public sealed class NeonButton : Button
    {
        private readonly NeonTheme _theme;

        private bool _hovered;
        private bool _pressed;

        public NeonButton(NeonTheme theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            FlatAppearance.BorderColor = _theme.ButtonBorder;
            FlatAppearance.MouseDownBackColor = _theme.ButtonBackground;
            FlatAppearance.MouseOverBackColor = _theme.ButtonBackground;

            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            ForeColor = _theme.TextPrimary;
            BackColor = _theme.ButtonBackground;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;

            TabStop = false;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            MouseEnter += (_, __) => { _hovered = true; UpdateVisualState(); };
            MouseLeave += (_, __) => { _hovered = false; _pressed = false; UpdateVisualState(); };
            MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _pressed = true;
                    UpdateVisualState();
                }
            };
            MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _pressed = false;
                    UpdateVisualState();
                }
            };
            EnabledChanged += (_, __) => UpdateVisualState();

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (!Enabled)
            {
                BackColor = Darken(_theme.ButtonBackground, 0.20f);
                ForeColor = _theme.WithAlpha(_theme.TextPrimary, 140);
                FlatAppearance.BorderColor = _theme.WithAlpha(_theme.ButtonBorder, 90);
                Cursor = Cursors.Default;
            }
            else if (_pressed)
            {
                BackColor = Darken(_theme.ButtonBackground, 0.10f);
                ForeColor = _theme.TextPrimary;
                FlatAppearance.BorderColor = _theme.WithAlpha(_theme.ButtonBorder, 255);
                Cursor = Cursors.Hand;
            }
            else if (_hovered)
            {
                BackColor = Lighten(_theme.ButtonBackground, 0.08f);
                ForeColor = _theme.TextPrimary;
                FlatAppearance.BorderColor = _theme.WithAlpha(_theme.ButtonBorder, 255);
                Cursor = Cursors.Hand;
            }
            else
            {
                BackColor = _theme.ButtonBackground;
                ForeColor = _theme.TextPrimary;
                FlatAppearance.BorderColor = _theme.ButtonBorder;
                Cursor = Cursors.Hand;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var borderColor = FlatAppearance.BorderColor;

            var glowColor = _pressed ? _theme.WithAlpha(borderColor, 120)
                : _hovered ? _theme.WithAlpha(borderColor, 160)
                : _theme.WithAlpha(borderColor, 90);

            DrawGlow(pevent.Graphics, rect, glowColor);

            using (var bg = new SolidBrush(BackColor))
                pevent.Graphics.FillRectangle(bg, rect);

            using (var pen = new Pen(borderColor, 1))
                pevent.Graphics.DrawRectangle(pen, rect);

            TextRenderer.DrawText(
                pevent.Graphics,
                Text,
                Font,
                rect,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawGlow(Graphics g, Rectangle rect, Color c)
        {
            int layers = Math.Max(1, _theme.GlowLayers);
            int spread = Math.Max(1, _theme.GlowSpreadPx);

            for (int i = 0; i < layers; i++)
            {
                float t = layers <= 1 ? 1f : (float)i / (layers - 1);
                int a = (int)(_theme.GlowAlphaStart + (_theme.GlowAlphaEnd - _theme.GlowAlphaStart) * t);
                var col = _theme.WithAlpha(c, a);

                int s = 1 + (int)(spread * (1f - t));
                var r = Rectangle.Inflate(rect, s, s);
                using (var pen = new Pen(col, 2))
                    g.DrawRectangle(pen, r);
            }
        }

        private static Color Lighten(Color c, float amount)
        {
            amount = Clamp01(amount);
            return Color.FromArgb(
                c.A,
                (int)(c.R + (255 - c.R) * amount),
                (int)(c.G + (255 - c.G) * amount),
                (int)(c.B + (255 - c.B) * amount));
        }

        private static Color Darken(Color c, float amount)
        {
            amount = Clamp01(amount);
            return Color.FromArgb(
                c.A,
                (int)(c.R * (1f - amount)),
                (int)(c.G * (1f - amount)),
                (int)(c.B * (1f - amount)));
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}

