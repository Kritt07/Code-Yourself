using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Models
{
    public enum MoveDirection
    {
        Left,
        Right
    }

    public class GameModel
    {
        public const int CanvasWidth = 800;
        public const int CanvasHeight = 400;

        public const int GroundHeight = 50;
        public const int GroundY = CanvasHeight - GroundHeight;
        public const int PlayerSize = 50;

        public Player Player { get; set; }
        // Симуляционные тики (30 Гц).
        public int SimTickCount { get; private set; } = 0;

        public const int DefaultCommandDurationSimTicks = 30;

        public sealed class RenderSnapshot
        {
            public int SimTickCount { get; }
            public Rectangle PlayerBounds { get; }
            public ObstacleSnapshot[] Obstacles { get; }
            public bool IsGameOver { get; }
            public string GameOverReason { get; }

            public RenderSnapshot(int simTickCount, Rectangle playerBounds, ObstacleSnapshot[] obstacles, bool isGameOver, string gameOverReason)
            {
                SimTickCount = simTickCount;
                PlayerBounds = playerBounds;
                Obstacles = obstacles ?? Array.Empty<ObstacleSnapshot>();
                IsGameOver = isGameOver;
                GameOverReason = gameOverReason;
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

        private readonly Point _playerStartPosition = new Point(50, GroundY - PlayerSize);

        private readonly List<IObstacle> _obstacles = new List<IObstacle>();
        public IReadOnlyList<IObstacle> Obstacles => _obstacles;

        public bool IsGameOver { get; private set; }
        public string GameOverReason { get; private set; }

        private Rectangle? _playerPrevBounds;
        private readonly Dictionary<IObstacle, Rectangle> _obstaclePrevBounds = new Dictionary<IObstacle, Rectangle>();

        // Намерения игрока на текущий командный тик.
        private int _pendingMoveDx;
        private int _pendingMoveSimTicksLeft;

        private JumpState _jump;

        private sealed class JumpState
        {
            public bool IsActive { get; set; }
            public int SubTicksTotal { get; set; }
            public int SubTickIndex { get; set; }
            public Point Start { get; set; }
            public Point End { get; set; }
            public int PeakHeight { get; set; }
        }

        public GameModel()
        {
            Player = new Player(_playerStartPosition.X, _playerStartPosition.Y, PlayerSize);
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
                isGameOver: IsGameOver,
                gameOverReason: GameOverReason);
        }

        public void StepSimulationTick()
        {
            if (IsGameOver)
            {
                return;
            }

            // 1) Обновляем препятствия детерминированно по текущему тиковому индексу.
            foreach (var obstacle in _obstacles)
            {
                _obstaclePrevBounds[obstacle] = obstacle.Bounds;
                obstacle.Update(SimTickCount);
            }

            // 1.5) Двигаем игрока на один сим-так согласно намерениям.
            ApplyPlayerMovementForSimulationTick();

            // 2) Платформы — не "смертельные", а твёрдые: разрешаем приземляться и ехать вместе с ними.
            //    Если предыдущие границы ещё не зафиксированы (например, в тестах/ручных сценариях) — fallback к текущему положению.
            var prevPlayer = _playerPrevBounds ?? GetPlayerBounds();
            var currPlayer = GetPlayerBounds();

            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Kind != ObstacleKind.MovingPlatform && obstacle.Kind != ObstacleKind.StaticPlatform)
                    continue;

                var prevObs = _obstaclePrevBounds.TryGetValue(obstacle, out var p) ? p : obstacle.Bounds;
                var currObs = obstacle.Bounds;

                // Если игрок стоял на платформе в прошлом тике — едем вместе с ней (только для движущейся).
                if (obstacle.Kind == ObstacleKind.MovingPlatform)
                {
                    var wasStandingOnTop =
                        prevPlayer.Bottom == prevObs.Top &&
                        prevPlayer.Right > prevObs.Left &&
                        prevPlayer.Left < prevObs.Right;

                    if (wasStandingOnTop)
                    {
                        var platformDx = currObs.X - prevObs.X;
                        if (platformDx != 0)
                        {
                            var newX = Player.Position.X + platformDx;
                            newX = Math.Max(0, Math.Min(CanvasWidth - Player.Size, newX));
                            Player.SetPosition(newX, Player.Position.Y);
                            currPlayer = GetPlayerBounds();
                        }
                    }
                }

                // Приземление сверху: в текущем тике пересеклись с платформой, а раньше были выше её верхней границы.
                if (currPlayer.IntersectsWith(currObs) && prevPlayer.Bottom <= prevObs.Top)
                {
                    Player.SetPosition(Player.Position.X, currObs.Top - Player.Size);
                    currPlayer = GetPlayerBounds();
                }
            }

            // 3) Пилы — смертельные. Проверяем swept collision (учёт пути за тик) только для них.
            var sweptPlayer = Union(prevPlayer, currPlayer);

            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Kind != ObstacleKind.Saw && obstacle.Kind != ObstacleKind.Spikes)
                    continue;

                var prevObs = _obstaclePrevBounds.TryGetValue(obstacle, out var p) ? p : obstacle.Bounds;
                var currObs = obstacle.Bounds;
                var sweptObs = Union(prevObs, currObs);

                if (sweptPlayer.IntersectsWith(sweptObs))
                {
                    IsGameOver = true;
                    GameOverReason = $"Collision with {obstacle.Kind}";
                    break;
                }
            }

            _playerPrevBounds = GetPlayerBounds();
            SimTickCount++;
        }

        private void ApplyPlayerMovementForSimulationTick()
        {
            // Прыжок имеет приоритет и сам включает движение по X/Y.
            if (_jump != null && _jump.IsActive)
            {
                var total = Math.Max(1, _jump.SubTicksTotal);
                var i = Math.Max(0, Math.Min(total, _jump.SubTickIndex + 1));
                var t = i / (double)total;

                var x = Lerp(_jump.Start.X, _jump.End.X, t);
                var yLine = Lerp(_jump.Start.Y, _jump.End.Y, t);
                var arc = 4.0 * _jump.PeakHeight * t * (1.0 - t);
                var y = (int)Math.Round(yLine - arc);

                SetPlayerPosition(x, y);

                _jump.SubTickIndex++;
                if (_jump.SubTickIndex >= total)
                    _jump.IsActive = false;

                return;
            }

            if (_pendingMoveSimTicksLeft <= 0 || _pendingMoveDx == 0)
                return;

            var remaining = Math.Max(1, _pendingMoveSimTicksLeft);
            var step = (int)Math.Round(_pendingMoveDx / (double)remaining);
            if (step == 0)
                step = _pendingMoveDx > 0 ? 1 : -1;

            var newX = Player.Position.X + step;
            newX = Math.Max(0, Math.Min(CanvasWidth - Player.Size, newX));
            Player.SetPosition(newX, Player.Position.Y);

            _pendingMoveDx -= step;
            _pendingMoveSimTicksLeft--;
        }

        private static int Lerp(int a, int b, double t)
        {
            return (int)Math.Round(a + (b - a) * t);
        }

        public void Reset()
        {
            SimTickCount = 0;
            Player.SetPosition(_playerStartPosition.X, _playerStartPosition.Y);
            IsGameOver = false;
            GameOverReason = null;
            _playerPrevBounds = null;
            _pendingMoveDx = 0;
            _pendingMoveSimTicksLeft = 0;
            _jump = null;

            foreach (var obstacle in _obstacles)
            {
                obstacle.Update(0);
                _obstaclePrevBounds[obstacle] = obstacle.Bounds;
            }
        }

        public void MovePlayer(MoveDirection direction, int durationSimTicks = DefaultCommandDurationSimTicks)
        {
            // Одна команда MOVE = движение на Player.DefaultStep,
            // растянутое по сим-такту на длительность команды.
            var dx = direction == MoveDirection.Left ? -Player.DefaultStep : Player.DefaultStep;
            _pendingMoveDx = dx;
            _pendingMoveSimTicksLeft = Math.Max(1, durationSimTicks);
            _jump = null;
        }

        public void JumpPlayer(MoveDirection direction, int durationSimTicks = DefaultCommandDurationSimTicks)
        {
            const int jumpHeight = 120;

            var dx = direction == MoveDirection.Left ? -Player.DefaultStep : Player.DefaultStep;
            var newX = Player.Position.X + dx;
            newX = Math.Max(0, Math.Min(CanvasWidth - Player.Size, newX));

            // Базовая высота — текущая (мы без гравитации), но не ниже земли.
            var groundY = GroundY - Player.Size;
            var baseY = Math.Min(Player.Position.Y, groundY);

            // Пытаемся "приземлиться" на платформу, если она достижима по высоте прыжка.
            // Выбираем самую высокую (минимальный Y) из достижимых.
            var bestY = baseY;
            var playerRectAtX = new Rectangle(newX, 0, Player.Size, Player.Size);

            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Kind != ObstacleKind.MovingPlatform && obstacle.Kind != ObstacleKind.StaticPlatform)
                    continue;

                var r = obstacle.Bounds;
                playerRectAtX.Y = r.Top - Player.Size; // позиция стояния на платформе

                // Должны перекрываться по X, и платформа должна быть выше/на уровне baseY,
                // но не выше, чем позволяет прыжок.
                var standY = r.Top - Player.Size;
                var reachable = standY <= baseY && (baseY - standY) <= jumpHeight;
                var overlapsX = playerRectAtX.Right > r.Left && playerRectAtX.Left < r.Right;

                if (reachable && overlapsX && standY < bestY)
                    bestY = standY;
            }

            // Итоговая Y: либо "на платформе", либо остаёмся на прежней высоте, но в пределах канваса.
            var newY = Math.Max(0, Math.Min(groundY, bestY));

            _pendingMoveDx = 0;
            _pendingMoveSimTicksLeft = 0;

            _jump = new JumpState
            {
                IsActive = true,
                SubTicksTotal = Math.Max(1, durationSimTicks),
                SubTickIndex = 0,
                Start = new Point(Player.Position.X, Player.Position.Y),
                End = new Point(newX, newY),
                PeakHeight = jumpHeight
            };
        }

        public void SetPlayerPosition(int x, int y)
        {
            x = Math.Max(0, Math.Min(CanvasWidth - Player.Size, x));
            var groundY = GroundY - Player.Size;
            y = Math.Max(0, Math.Min(groundY, y));
            Player.SetPosition(x, y);
        }

        public Rectangle GetPlayerBounds()
        {
            return new Rectangle(Player.Position.X, Player.Position.Y, Player.Size, Player.Size);
        }

        private static Rectangle Union(Rectangle a, Rectangle b)
        {
            var left = Math.Min(a.Left, b.Left);
            var top = Math.Min(a.Top, b.Top);
            var right = Math.Max(a.Right, b.Right);
            var bottom = Math.Max(a.Bottom, b.Bottom);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }
}