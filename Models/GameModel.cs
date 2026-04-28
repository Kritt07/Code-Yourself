using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Models
{
    public enum MoveDirection
    {
        Left,
        Right
    }

    public enum GameEndState
    {
        Running,
        Won,
        Lost
    }

    public partial class GameModel
    {
        // Для hazards (пила/шипы) используем swept-collision через Union(prev,curr),
        // чтобы не пропускать столкновения при быстром движении.

        public const int CanvasWidth = 800;
        public const int CanvasHeight = 400;

        public const int GroundHeight = 50;
        public const int GroundY = CanvasHeight - GroundHeight;
        public const int PlayerInsetLeftPx = 5;
        public const int PlayerInsetRightPx = 5;
        // Делаем player квадратным (40x40) при bottomInset=0:
        // size = 50 - 5 - 5 = 40 => topInset = 50 - 40 - 0 = 10.
        public const int PlayerInsetTopPx = 10;
        public const int PlayerInsetBottomPx = 0;
        public const int PlayerWidthPx = Grid.CellSizePx - PlayerInsetLeftPx - PlayerInsetRightPx;   // 40
        public const int PlayerHeightPx = Grid.CellSizePx - PlayerInsetTopPx - PlayerInsetBottomPx; // 45

        public const int PlayerCellCenterOffsetXPx = (Grid.CellSizePx - PlayerWidthPx) / 2; // 5

        public Player Player { get; set; }
        // Симуляционные тики (фиксированный шаг; 30 sim ticks на command tick).
        public int SimTickCount { get; private set; } = 0;

        public const int DefaultCommandDurationSimTicks = 30;

        public sealed class RenderSnapshot
        {
            public int SimTickCount { get; }
            public Rectangle PlayerBounds { get; }
            public ObstacleSnapshot[] Obstacles { get; }
            public GameEndState EndState { get; }
            public string EndReason { get; }

            public RenderSnapshot(int simTickCount, Rectangle playerBounds, ObstacleSnapshot[] obstacles, GameEndState endState, string endReason)
            {
                SimTickCount = simTickCount;
                PlayerBounds = playerBounds;
                Obstacles = obstacles ?? Array.Empty<ObstacleSnapshot>();
                EndState = endState;
                EndReason = endReason;
            }
        }

        public struct ObstacleSnapshot
        {
            public ObstacleKind Kind { get; }
            public Rectangle Bounds { get; }

            public ObstacleSnapshot(ObstacleKind kind, Rectangle bounds)
            {
                Kind = kind;
                Bounds = bounds;
            }
        }

        private readonly Point _playerStartPosition = new Point(
            x: Grid.XCellsToPx(1) + PlayerCellCenterOffsetXPx,
            y: Grid.TopPxFromBottomOnGroundCells(0, PlayerHeightPx));

        private readonly List<IObstacle> _obstacles = new List<IObstacle>();
        public IReadOnlyList<IObstacle> Obstacles => _obstacles;

        public GameEndState EndState { get; private set; } = GameEndState.Running;
        public string EndReason { get; private set; }

        // Week 3 совместимость: старый UI/код мог читать IsGameOver/GameOverReason.
        public bool IsGameOver => EndState == GameEndState.Lost;
        public string GameOverReason => EndState == GameEndState.Lost ? EndReason : null;

        // Финиш-зона уровня (часть контента уровня, не сбрасывается в Reset()).
        public Rectangle? FinishZone { get; private set; }

        public void SetFinishZone(Rectangle zone)
        {
            FinishZone = zone;
        }

        private Rectangle? _playerPrevBounds;
        private readonly Dictionary<IObstacle, Rectangle> _obstaclePrevBounds = new Dictionary<IObstacle, Rectangle>();

        public GameModel()
        {
            Player = new Player(_playerStartPosition.X, _playerStartPosition.Y, PlayerWidthPx, PlayerHeightPx);
            SyncFixedFromPlayer();
        }

        public void AddObstacle(IObstacle obstacle)
        {
            if (obstacle == null) throw new ArgumentNullException(nameof(obstacle));
            _obstacles.Add(obstacle);
            _obstaclePrevBounds[obstacle] = obstacle.Bounds;
        }

        public void ClearObstacles()
        {
            _obstacles.Clear();
            _obstaclePrevBounds.Clear();
        }

        public RenderSnapshot CreateRenderSnapshot()
        {
            var obstacles = new ObstacleSnapshot[_obstacles.Count];
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                obstacles[i] = new ObstacleSnapshot(o.Kind, o.Bounds);
            }

            return new RenderSnapshot(
                simTickCount: SimTickCount,
                playerBounds: GetPlayerBounds(),
                obstacles: obstacles,
                endState: EndState,
                endReason: EndReason);
        }

        public void StepSimulationTick()
        {
            if (EndState != GameEndState.Running)
            {
                return;
            }

            // 0) Предыдущее положение игрока для swept-collision по hazards.
            // Используем зафиксированное прошлым тиком значение, чтобы тесты/сценарии с "телепортом"
            // между тиками тоже детектировались.
            var prevPlayer = _playerPrevBounds ?? GetPlayerBounds();

            // 1) Обновляем препятствия детерминированно по текущему тиковому индексу.
            foreach (var obstacle in _obstacles)
            {
                _obstaclePrevBounds[obstacle] = obstacle.Bounds;
                obstacle.Update(SimTickCount);
            }

            // 2) Двигаем игрока (vx/vy + гравитация) и решаем коллизии с твёрдыми платформами.
            IntegrateAndResolvePlayer();

            // 2.1) Если стоим на moving-platform — едем вместе с ней на её dx за тик.
            ApplyMovingPlatformRide();

            var currPlayer = GetPlayerBounds();

            // 3) Пилы/шипы — смертельные. Проверяем движение внутри тика подшагами (детерминированно),
            // чтобы не было «проскока» через hazard за один sim-tick.
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Kind != ObstacleKind.Saw && obstacle.Kind != ObstacleKind.Spikes)
                    continue;

                var prevObs = _obstaclePrevBounds.TryGetValue(obstacle, out var p) ? p : obstacle.Bounds;
                var currObs = obstacle.Bounds;

                Rectangle ShrinkSpikesHitbox(Rectangle r)
                {
                    // Визуально шипы "острые" и занимают не всю ширину/высоту Bounds.
                    // Сжимаем хитбокс: уже по X и ниже по Y (только верхнюю часть), чтобы прыжки через 1 клетку были возможны.
                    const int insetX = 4;
                    const int insetTop = 6;

                    int left = r.Left + insetX;
                    int right = r.Right - insetX;
                    if (right <= left)
                    {
                        left = r.Left;
                        right = r.Right;
                    }

                    int top = r.Top + insetTop;
                    int bottom = r.Bottom;
                    if (bottom <= top)
                    {
                        top = r.Top;
                        bottom = r.Bottom;
                    }

                    return Rectangle.FromLTRB(left, top, right, bottom);
                }

                // Swept collision: объединяем прямоугольники prev/curr.
                // Для Spikes сначала уменьшаем хитбокс и только потом делаем Union.
                if (obstacle.Kind == ObstacleKind.Spikes)
                {
                    prevObs = ShrinkSpikesHitbox(prevObs);
                    currObs = ShrinkSpikesHitbox(currObs);
                }

                var sweptPlayer = Union(prevPlayer, currPlayer);
                var sweptObs = Union(prevObs, currObs);

                if (sweptPlayer.IntersectsWith(sweptObs))
                {
                    EndState = GameEndState.Lost;
                    EndReason = $"Collision with {obstacle.Kind}";
                }

                if (EndState != GameEndState.Running)
                    break;
            }

            // 4) Победа: если попали в финиш-зону. Приоритет у поражения (если в этот тик умерли — победа не засчитывается).
            if (EndState == GameEndState.Running && FinishZone.HasValue)
            {
                if (currPlayer.IntersectsWith(FinishZone.Value))
                {
                    EndState = GameEndState.Won;
                    EndReason = "Reached finish";
                }
            }

            _playerPrevBounds = GetPlayerBounds();
            SimTickCount++;
        }

        public void Reset()
        {
            SimTickCount = 0;
            Player.SetPosition(_playerStartPosition.X, _playerStartPosition.Y);
            EndState = GameEndState.Running;
            EndReason = null;
            _playerPrevBounds = null;
            SyncFixedFromPlayer();
            ResetPhysics();

            foreach (var obstacle in _obstacles)
            {
                obstacle.Update(0);
                _obstaclePrevBounds[obstacle] = obstacle.Bounds;
            }
        }

        public void MovePlayer(MoveDirection direction, int durationSimTicks = DefaultCommandDurationSimTicks)
        {
            StartMove(direction, durationSimTicks);
        }

        public void JumpPlayer(MoveDirection direction, int durationSimTicks = DefaultCommandDurationSimTicks)
        {
            JumpPlayer(direction, cells: 2, durationSimTicks: durationSimTicks);
        }

        public void JumpPlayer(MoveDirection direction, int cells, int durationSimTicks = DefaultCommandDurationSimTicks)
        {
            if (cells < 1 || cells > 3)
                throw new ArgumentOutOfRangeException(nameof(cells), "Jump cells must be in range 1..3.");

            StartJump(direction, durationSimTicks, distancePerCommandTickPx: cells * Grid.CellSizePx);
        }

        public void SetPlayerPosition(int x, int y)
        {
            x = Math.Max(0, Math.Min(CanvasWidth - Player.Width, x));
            var groundY = GroundY - Player.Height;
            y = Math.Max(0, Math.Min(groundY, y));
            Player.SetPosition(x, y);
            SyncFixedFromPlayer();
        }

        public Rectangle GetPlayerBounds()
        {
            return new Rectangle(Player.Position.X, Player.Position.Y, Player.Width, Player.Height);
        }

        public void SnapPlayerToCellCenter()
        {
            if (_grounded && _groundedPlatform is MovingPlatformObstacle mp)
                SnapPlayerXToPlatformGrid(mp);
            else
                SnapPlayerXToCellCenter();
        }

        private void SnapPlayerXToPlatformGrid(MovingPlatformObstacle platform)
        {
            var targetX = platform.SnapPlayerXToOwnGrid(Player.Position.X, PlayerCellCenterOffsetXPx);
            targetX = Math.Max(0, Math.Min(CanvasWidth - Player.Width, targetX));

            if (targetX == Player.Position.X)
                return;

            var prevX = Player.Position.X;
            Player.SetPosition(targetX, Player.Position.Y);

            var dxFixed = (long)(targetX - prevX) * FixedScale;
            ResolveSolidCollisionsX(dxFixed);
            SyncFixedFromPlayerX();
        }

        private void SnapPlayerXToCellCenter()
        {
            var cell = Grid.CellSizePx;
            var offset = PlayerCellCenterOffsetXPx;

            // Позицию привязываем так, чтобы игрок оставался "по центру" клетки относительно сетки:
            // x = cellIndex * CellSize + offset.
            var rel = Player.Position.X - offset;
            int cellIndex = rel >= 0
                ? (rel + (cell / 2)) / cell
                : -(((-rel) + (cell / 2)) / cell);

            var targetX = (cellIndex * cell) + offset;
            targetX = Math.Max(0, Math.Min(CanvasWidth - Player.Width, targetX));

            if (targetX == Player.Position.X)
                return;

            var prevX = Player.Position.X;
            Player.SetPosition(targetX, Player.Position.Y);

            // После "подтягивания" можем слегка въехать в платформы/стены — корректируем коллизии.
            var dxFixed = (long)(targetX - prevX) * FixedScale;
            ResolveSolidCollisionsX(dxFixed);
            SyncFixedFromPlayerX();
        }

        private static Rectangle Union(Rectangle a, Rectangle b)
        {
            var left = Math.Min(a.Left, b.Left);
            var top = Math.Min(a.Top, b.Top);
            var right = Math.Max(a.Right, b.Right);
            var bottom = Math.Max(a.Bottom, b.Bottom);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        private static Rectangle LerpRect(Rectangle a, Rectangle b, int step, int steps)
        {
            if (steps <= 0) return a;
            if (step <= 0) return a;
            if (step >= steps) return b;

            // Детерминированная линейная интерполяция по целым (без float).
            int Lerp(int av, int bv) => av + (int)(((long)(bv - av) * step) / steps);

            return new Rectangle(
                x: Lerp(a.X, b.X),
                y: Lerp(a.Y, b.Y),
                width: Lerp(a.Width, b.Width),
                height: Lerp(a.Height, b.Height));
        }
    }
}