using System;
using CodeYourself.Models.Obstacles;

namespace CodeYourself.Models
{
    public partial class GameModel
    {
        // Fixed-point физика: значения в "px * FixedScale".
        private const int FixedScale = 1000;

        // MOVE должен стремиться пройти 50px за 1 command tick (=30 sim ticks).
        private const int MoveDistancePerCommandTickPx = 50;

        // Вертикальная физика (в px/tick, умноженных на 1000 для fixed-point).
        private const int GravityFixed = 1400;        // 1.4 px/tick^2
        private const int JumpImpulseFixed = 25000;   // 20 px/tick вверх
        private const int MaxFallSpeedFixed = 30000;  // 30 px/tick вниз

        // Физика игрока.
        private long _posXFixed;
        private long _posYFixed;
        private long _vyFixed;

        // Горизонтальная команда: распределяем 50px равномерно и детерминированно по N sim ticks.
        private int _moveTicksLeft;
        private int _moveStepBaseFixed; // fixed units per tick (absolute)
        private int _moveStepRemainder; // number of ticks that get +1 fixed unit
        private int _moveStepSign;      // -1 / +1 / 0

        private bool _grounded;
        private IObstacle _groundedPlatform;

        private void ResetPhysics()
        {
            _moveTicksLeft = 0;
            _moveStepBaseFixed = 0;
            _moveStepRemainder = 0;
            _moveStepSign = 0;
            _vyFixed = 0;
            _grounded = false;
            _groundedPlatform = null;
        }

        private void StartMove(MoveDirection direction, int durationSimTicks)
        {
            var ticks = Math.Max(1, durationSimTicks);
            _moveTicksLeft = ticks;
            _moveStepSign = direction == MoveDirection.Left ? -1 : 1;

            var total = MoveDistancePerCommandTickPx * FixedScale;
            _moveStepBaseFixed = total / ticks;
            _moveStepRemainder = total % ticks;
        }

        private void StartJump(MoveDirection direction, int durationSimTicks)
        {
            // Прыгать можно только стоя на земле/платформе.
            if (!_grounded)
            {
                StartMove(direction, durationSimTicks);
                return;
            }

            _vyFixed = -JumpImpulseFixed;
            _grounded = false;
            _groundedPlatform = null;

            StartMove(direction, durationSimTicks);
        }

        private void IntegrateAndResolvePlayer()
        {
            // 1) Горизонтальный шаг от команды (MOVE/JUMP): 50px за command tick распределяются по N sim ticks.
            var dxFixed = GetHorizontalStepFixedAndAdvance();
            _posXFixed += dxFixed;
            ClampFixedToCanvasX();
            var xBefore = FixedToInt(_posXFixed);
            Player.SetPosition(xBefore, Player.Position.Y);
            ResolveSolidCollisionsX(dxFixed);
            if (Player.Position.X != xBefore)
                SyncFixedFromPlayerX();

            // 2) Гравитация (fixed-point).
            _vyFixed += GravityFixed;
            if (_vyFixed > MaxFallSpeedFixed)
                _vyFixed = MaxFallSpeedFixed;

            // 3) Вертикальный шаг + коллизии.
            _grounded = false;
            _groundedPlatform = null;

            _posYFixed += _vyFixed;
            ClampFixedToCanvasY();
            var yBefore = FixedToInt(_posYFixed);
            Player.SetPosition(Player.Position.X, yBefore);
            ResolveSolidCollisionsY();
            if (Player.Position.Y != yBefore)
                SyncFixedFromPlayerY();

            // 4) Земля.
            var groundY = GroundY - Player.Size;
            if (Player.Position.Y >= groundY)
            {
                Player.SetPosition(Player.Position.X, groundY);
                _vyFixed = 0;
                _grounded = true;
                _groundedPlatform = null;
                SyncFixedFromPlayerY();
            }
        }

        private void ResolveSolidCollisionsX(long dxFixed)
        {
            if (dxFixed == 0)
                return;

            var player = GetPlayerBounds();
            foreach (var o in _obstacles)
            {
                if (o.Kind != ObstacleKind.MovingPlatform && o.Kind != ObstacleKind.StaticPlatform)
                    continue;

                var r = o.Bounds;
                if (!player.IntersectsWith(r))
                    continue;

                if (dxFixed > 0)
                    Player.SetPosition(r.Left - Player.Size, Player.Position.Y);
                else
                    Player.SetPosition(r.Right, Player.Position.Y);

                player = GetPlayerBounds();
            }
        }

        private void ResolveSolidCollisionsY()
        {
            var player = GetPlayerBounds();
            foreach (var o in _obstacles)
            {
                if (o.Kind != ObstacleKind.MovingPlatform && o.Kind != ObstacleKind.StaticPlatform)
                    continue;

                var r = o.Bounds;
                if (!player.IntersectsWith(r))
                    continue;

                if (_vyFixed > 0)
                {
                    // Падение: приземляемся сверху.
                    Player.SetPosition(Player.Position.X, r.Top - Player.Size);
                    _vyFixed = 0;
                    _grounded = true;
                    _groundedPlatform = o;
                }
                else if (_vyFixed < 0)
                {
                    // Движение вверх: удар головой.
                    Player.SetPosition(Player.Position.X, r.Bottom);
                    _vyFixed = 0;
                }

                player = GetPlayerBounds();
            }
        }

        private void ApplyMovingPlatformRide()
        {
            if (!_grounded || _groundedPlatform == null || _groundedPlatform.Kind != ObstacleKind.MovingPlatform)
                return;

            var prevObs = _obstaclePrevBounds.TryGetValue(_groundedPlatform, out var p) ? p : _groundedPlatform.Bounds;
            var currObs = _groundedPlatform.Bounds;
            var dx = currObs.X - prevObs.X;
            if (dx == 0)
                return;

            _posXFixed += (long)dx * FixedScale;
            ClampFixedToCanvasX();
            var xBefore = FixedToInt(_posXFixed);
            Player.SetPosition(xBefore, Player.Position.Y);

            // После переноса платформой можем въехать в статическую геометрию.
            ResolveSolidCollisionsX(dx * FixedScale);
            if (Player.Position.X != xBefore)
                SyncFixedFromPlayerX();
        }

        private int GetHorizontalStepFixedAndAdvance()
        {
            if (_moveTicksLeft <= 0 || _moveStepSign == 0)
                return 0;

            var sign = _moveStepSign;
            var step = _moveStepBaseFixed;
            if (_moveStepRemainder > 0)
            {
                step += 1;
                _moveStepRemainder--;
            }

            _moveTicksLeft--;
            if (_moveTicksLeft == 0)
                _moveStepSign = 0;

            return sign * step;
        }

        private void SyncFixedFromPlayer()
        {
            _posXFixed = (long)Player.Position.X * FixedScale;
            _posYFixed = (long)Player.Position.Y * FixedScale;
        }

        private void SyncFixedFromPlayerX()
        {
            _posXFixed = (long)Player.Position.X * FixedScale;
        }

        private void SyncFixedFromPlayerY()
        {
            _posYFixed = (long)Player.Position.Y * FixedScale;
        }

        private void ClampFixedToCanvasX()
        {
            var min = 0L;
            var max = (long)(CanvasWidth - Player.Size) * FixedScale;
            if (_posXFixed < min) _posXFixed = min;
            if (_posXFixed > max) _posXFixed = max;
        }

        private void ClampFixedToCanvasY()
        {
            var min = 0L;
            var max = (long)(GroundY - Player.Size) * FixedScale;
            if (_posYFixed < min) _posYFixed = min;
            if (_posYFixed > max) _posYFixed = max;
        }

        private static int FixedToInt(long valueFixed)
        {
            // Детеминированное преобразование fixed->int без дрейфа: floor для положительных.
            if (valueFixed >= 0)
                return (int)(valueFixed / FixedScale);
            // Для отрицательных — ceil (чтобы не уходить дальше в минус из-за /).
            return (int)-((-valueFixed) / FixedScale);
        }
    }
}

