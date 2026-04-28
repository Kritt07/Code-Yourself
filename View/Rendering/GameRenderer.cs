using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using CodeYourself.Controllers;
using CodeYourself.Models;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.View.Rendering
{
    public sealed class GameRenderer
    {
        public NeonTheme Theme { get; }

        public GameRenderer()
        {
            Theme = new NeonTheme();
        }

        public void Render(Graphics g, GameModel model, GameController controller, long renderTickCount, Size panelSize)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            var player = model.Player;

            // === Центрируем виртуальное поле внутри панели ===
            int offsetX = (panelSize.Width - GameModel.CanvasWidth) / 2;
            int offsetY = (panelSize.Height - GameModel.CanvasHeight) / 2;

            // Сохраняем состояние Graphics (трансформации/режимы) и восстанавливаем в конце.
            var state = g.Save();
            try
            {
                // Default (will be tuned further in neon-canvas-ui + polish-perf)
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                g.TranslateTransform(offsetX, offsetY);

                // Canvas background (inside 800x400)
                g.FillRectangle(Theme.CanvasBackgroundBrush, 0, 0, GameModel.CanvasWidth, GameModel.CanvasHeight);

                DrawCanvasFrame(g);
                DrawGrid(g);

                // Земля (только в пределах виртуального поля)
                g.FillRectangle(Brushes.DarkSlateGray,
                    0, GameModel.GroundY,
                    GameModel.CanvasWidth, GameModel.GroundHeight);

                DrawObstacles(g, model, renderTickCount);
                DrawFinish(g, model);

                // Персонаж
                DrawPlayer(g, player);

                // Подпись Player
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                    g.DrawString("Player", font, Brushes.White,
                        player.Position.X + 8, player.Position.Y - 25);

                // Отладка (явно разделяем command/sim/render ticks)
                using (var font = new Font("Consolas", 10))
                    g.DrawString(
                        $"CommandTick: {controller.CommandTickCount} | SimTick: {model.SimTickCount} | RenderTick: {renderTickCount} | Canvas: {GameModel.CanvasWidth}x{GameModel.CanvasHeight}",
                        font,
                        Theme.TextAccentBrush,
                        20,
                        20);

                DrawEndOverlay(g, model);
            }
            finally
            {
                g.Restore(state);
            }
        }

        private void DrawCanvasFrame(Graphics g)
        {
            // Simple neon frame: a few translucent strokes + a bright inner stroke
            var baseColor = Theme.PlatformNeon;

            for (int i = Theme.GlowLayers; i >= 1; i--)
            {
                int spread = i * 2;
                int alpha = Theme.GlowAlphaEnd + (Theme.GlowAlphaStart - Theme.GlowAlphaEnd) * i / Theme.GlowLayers;
                using (var pen = new Pen(Theme.WithAlpha(baseColor, alpha), 2 + spread))
                {
                    pen.Alignment = PenAlignment.Center;
                    g.DrawRectangle(pen, -1, -1, GameModel.CanvasWidth + 2, GameModel.CanvasHeight + 2);
                }
            }

            using (var pen = new Pen(Theme.WithAlpha(baseColor, 220), 2))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(pen, 0, 0, GameModel.CanvasWidth, GameModel.CanvasHeight);
            }
        }

        private void DrawGrid(Graphics g)
        {
            using (var pen = new Pen(Theme.GridLine, 1))
            using (var axisPen = new Pen(Theme.GridAxis, 2))
            {
                // Vertical lines
                for (int x = 0; x <= GameModel.CanvasWidth; x += Grid.CellSizePx)
                {
                    g.DrawLine(pen, x, 0, x, GameModel.CanvasHeight);
                }

                // Horizontal lines
                for (int y = 0; y <= GameModel.CanvasHeight; y += Grid.CellSizePx)
                {
                    g.DrawLine(pen, 0, y, GameModel.CanvasWidth, y);
                }

                // Ground line highlight
                g.DrawLine(axisPen, 0, GameModel.GroundY, GameModel.CanvasWidth, GameModel.GroundY);
            }
        }

        private void DrawObstacles(Graphics g, GameModel model, long renderTickCount = 0)
        {
            foreach (var obstacle in model.Obstacles)
            {
                var r = obstacle.Bounds;
                switch (obstacle.Kind)
                {
                    case ObstacleKind.MovingPlatform:
                    case ObstacleKind.StaticPlatform:
                        DrawNeonPlatform(g, r);
                        break;
                    case ObstacleKind.Spikes:
                        DrawNeonSpikes(g, r);
                        break;
                    case ObstacleKind.Saw:
                        DrawNeonSaw(g, r, renderTickCount);
                        break;
                    default:
                        using (var fallback = new SolidBrush(Theme.WithAlpha(Theme.PlatformNeon, 80)))
                            g.FillRectangle(fallback, r);
                        break;
                }
            }
        }

        private void DrawNeonPlatform(Graphics g, Rectangle r)
        {
            using (var body = new SolidBrush(Theme.WithAlpha(Theme.PlatformNeon, 50)))
                g.FillRectangle(body, r);
            using (var pen = new Pen(Theme.WithAlpha(Theme.PlatformNeon, 230), 2))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(pen, r);
            }
        }

        private void DrawNeonSpikes(Graphics g, Rectangle r)
        {
            var c = Theme.HazardNeon;

            // Triangles
            int toothW = Math.Max(8, r.Height); // roughly square teeth
            int count = Math.Max(1, r.Width / toothW);
            float w = (float)r.Width / count;

            using (var fill = new SolidBrush(Theme.WithAlpha(c, 80)))
            using (var pen = new Pen(Theme.WithAlpha(c, 240), 2))
            {
                for (int i = 0; i < count; i++)
                {
                    float x0 = r.Left + i * w;
                    float x1 = r.Left + (i + 1) * w;
                    float xm = (x0 + x1) / 2f;

                    var pts = new[]
                    {
                        new PointF(x0, r.Bottom),
                        new PointF(xm, r.Top),
                        new PointF(x1, r.Bottom),
                    };

                    g.FillPolygon(fill, pts);
                    g.DrawPolygon(pen, pts);
                }
            }
        }

        private void DrawNeonSaw(Graphics g, Rectangle r, long renderTickCount)
        {
            var c = Theme.HazardNeon;

            // Teeth polygon (visual only)
            var cx = r.Left + r.Width / 2f;
            var cy = r.Top + r.Height / 2f;
            var outer = Math.Min(r.Width, r.Height) / 2f;
            var inner = outer * 0.72f;

            int teeth = 12;
            double rot = (renderTickCount % 360) * (Math.PI / 180.0) * 2.0; // faster than 1deg/tick

            var pts = new PointF[teeth * 2];
            for (int i = 0; i < pts.Length; i++)
            {
                bool isOuter = (i % 2) == 0;
                double a = rot + (i * Math.PI / teeth);
                float rad = isOuter ? outer : inner;
                pts[i] = new PointF(
                    x: cx + (float)(Math.Cos(a) * rad),
                    y: cy + (float)(Math.Sin(a) * rad));
            }

            using (var body = new SolidBrush(Theme.WithAlpha(c, 60)))
                g.FillPolygon(body, pts);
            using (var pen = new Pen(Theme.WithAlpha(c, 240), 2))
                g.DrawPolygon(pen, pts);

            // Hub
            float hub = inner * 0.45f;
            var hubRect = new RectangleF(cx - hub, cy - hub, hub * 2, hub * 2);
            using (var hubFill = new SolidBrush(Theme.WithAlpha(Color.Black, 160)))
                g.FillEllipse(hubFill, hubRect);
            using (var hubPen = new Pen(Theme.WithAlpha(c, 160), 2))
                g.DrawEllipse(hubPen, hubRect);
        }

        private static void DrawFinish(Graphics g, GameModel model)
        {
            if (!model.FinishZone.HasValue)
                return;

            var r = model.FinishZone.Value;
            using (var fill = new SolidBrush(Color.FromArgb(90, 120, 200, 255)))
                g.FillRectangle(fill, r);
            using (var pen = new Pen(Color.FromArgb(220, 120, 200, 255), 3))
                g.DrawRectangle(pen, r);
            using (var font = new Font("Arial", 10, FontStyle.Bold))
                g.DrawString("EXIT", font, Brushes.White, r.X - 8, r.Y - 18);
        }

        private void DrawPlayer(Graphics g, Player player)
        {
            var r = new Rectangle(player.Position.X, player.Position.Y, player.Width, player.Height);

            // Body fill (slightly darker core)
            using (var fill = new SolidBrush(Theme.WithAlpha(Color.FromArgb(0, 0, 0), 0)))
            {
                // keep as solid for now; gradient can be added later without touching call sites
            }
            using (var body = new SolidBrush(Theme.WithAlpha(Theme.PlayerNeon, 55)))
                g.FillRectangle(body, r);

            // Bright outline
            using (var pen = new Pen(Theme.WithAlpha(Theme.PlayerNeon, 230), 2))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(pen, r);
            }

            // Specular highlight (top-left edge)
            using (var pen = new Pen(Theme.WithAlpha(Color.White, 90), 1))
            {
                g.DrawLine(pen, r.Left + 2, r.Top + 2, r.Right - 3, r.Top + 2);
                g.DrawLine(pen, r.Left + 2, r.Top + 2, r.Left + 2, r.Bottom - 3);
            }
        }

        private static void DrawEndOverlay(Graphics g, GameModel model)
        {
            if (model.EndState == GameEndState.Running)
                return;

            using (var font = new Font("Arial", 18, FontStyle.Bold))
            {
                var text = model.EndState == GameEndState.Won ? "YOU WIN" : "GAME OVER";
                var brush = model.EndState == GameEndState.Won ? Brushes.LawnGreen : Brushes.Red;
                g.DrawString(text, font, brush, 20, 50);
            }

            if (!string.IsNullOrWhiteSpace(model.EndReason))
            {
                using (var font = new Font("Consolas", 10))
                    g.DrawString(model.EndReason, font, Brushes.White, 20, 80);
            }
        }
    }
}

