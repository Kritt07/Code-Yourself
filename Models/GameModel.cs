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

    public partial class GameModel
    {
        public const int CanvasWidth = 800;
        public const int CanvasHeight = 400;

        public const int GroundHeight = 50;
        public const int GroundY = CanvasHeight - GroundHeight;
        public const int PlayerSize = 40;

        public Player Player { get; set; }
        // Симуляционные тики (фиксированный шаг; 30 sim ticks на command tick).
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

        public GameModel()
        {
            Player = new Player(_playerStartPosition.X, _playerStartPosition.Y, PlayerSize);
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
                isGameOver: IsGameOver,
                gameOverReason: GameOverReason);
        }

        public void StepSimulationTick()
        {
            if (IsGameOver)
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

        public void Reset()
        {
            SimTickCount = 0;
            Player.SetPosition(_playerStartPosition.X, _playerStartPosition.Y);
            IsGameOver = false;
            GameOverReason = null;
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
            StartJump(direction, durationSimTicks);
        }

        public void SetPlayerPosition(int x, int y)
        {
            x = Math.Max(0, Math.Min(CanvasWidth - Player.Size, x));
            var groundY = GroundY - Player.Size;
            y = Math.Max(0, Math.Min(groundY, y));
            Player.SetPosition(x, y);
            SyncFixedFromPlayer();
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