using System;
using System.Drawing;

namespace CodeYourself.View.Rendering
{
    public sealed class NeonTheme : IDisposable
    {
        // Core palette
        public Color PanelBackground { get; } = Color.FromArgb(20, 18, 28);
        public Color CanvasBackground { get; } = Color.FromArgb(10, 10, 14);

        public Color GridLine { get; } = Color.FromArgb(18, 120, 255, 255);
        public Color GridAxis { get; } = Color.FromArgb(70, 160, 255, 255);

        public Color TextPrimary { get; } = Color.FromArgb(235, 240, 255);
        public Color TextAccent { get; } = Color.FromArgb(160, 255, 225);

        // UI colors
        public Color EditorBackground { get; } = Color.FromArgb(14, 12, 18);
        public Color EditorForeground { get; } = Color.FromArgb(170, 255, 210);
        public Color UiPanelBackground { get; } = Color.FromArgb(16, 14, 22);
        public Color ButtonBackground { get; } = Color.FromArgb(26, 22, 34);
        public Color ButtonBorder { get; } = Color.FromArgb(90, 220, 255);

        public Color PlayerNeon { get; } = Color.FromArgb(80, 255, 120);
        public Color PlatformNeon { get; } = Color.FromArgb(90, 220, 255);
        public Color HazardNeon { get; } = Color.FromArgb(255, 60, 90);
        public Color FinishNeon { get; } = Color.FromArgb(120, 200, 255);

        // Glow tuning
        public int GlowLayers { get; } = 3;
        public int GlowSpreadPx { get; } = 6;
        public int GlowAlphaStart { get; } = 90; // outermost
        public int GlowAlphaEnd { get; } = 30;   // innermost

        // Cached pens/brushes that are reused often (dispose on form close).
        public SolidBrush CanvasBackgroundBrush { get; }
        public SolidBrush PanelBackgroundBrush { get; }
        public SolidBrush TextPrimaryBrush { get; }
        public SolidBrush TextAccentBrush { get; }

        public NeonTheme()
        {
            CanvasBackgroundBrush = new SolidBrush(CanvasBackground);
            PanelBackgroundBrush = new SolidBrush(PanelBackground);
            TextPrimaryBrush = new SolidBrush(TextPrimary);
            TextAccentBrush = new SolidBrush(TextAccent);
        }

        public Color WithAlpha(Color c, int a)
        {
            a = Math.Max(0, Math.Min(255, a));
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        public void Dispose()
        {
            CanvasBackgroundBrush?.Dispose();
            PanelBackgroundBrush?.Dispose();
            TextPrimaryBrush?.Dispose();
            TextAccentBrush?.Dispose();
        }
    }
}

