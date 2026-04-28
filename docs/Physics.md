# Physics (player movement) — how it works

Этот документ описывает **физику игрока** в проекте: как из команд получается движение, как работает гравитация/прыжок, коллизии и moving-platform.

Файлы реализации:
- `Models/GameModel.cs` — “оркестратор” тика: обновление препятствий → физика → опасности.
- `Models/GameModel.Physics.cs` — вся внутренняя физика (fixed-point интеграция, коллизии, ride).
- `Controllers/GameController.cs` — command tick (команда) и fixed-step simulation ticks.

---

## 1) Тики и где исполняется физика

### Command tick
- **Command tick = 0.5 секунды**.
- На каждом command tick берётся **одна команда**, она исполняется мгновенно (задаёт намерение), затем проигрывается фиксированное число simulation ticks.

### Simulation tick
- **30 simulation ticks на 1 command tick**.
- Один simulation tick — это один вызов `GameModel.StepSimulationTick()`.
- Внутри него вызывается `IntegrateAndResolvePlayer()` (физика игрока) и `ApplyMovingPlatformRide()` (ехать вместе с платформой).

### Render tick
- Отрисовка идёт отдельно (примерно **60Hz**) и не влияет на физику.

---

## 2) Почему используется fixed-point (дробные пиксели)

Требование для MOVE: игрок должен стремиться пройти **50px за 1 command tick**.

Так как на command tick приходится 30 sim ticks, средний шаг:
\[
50 / 30 \approx 1.666\ldots \text{ px за sim-tick}
\]

Если хранить позицию/скорости как `int`, придётся округлять каждый тик, что даёт дрейф (то 48px, то 58px и т.п.).

Поэтому внутри физики используется fixed-point:
- `FixedScale = 1000`
- “1 пиксель” хранится как “1000 единиц”
- позиции `_posXFixed/_posYFixed` и скорость `_vyFixed` хранятся в этих единицах.

Преимущества:
- движение с дробным шагом без потери точности;
- детерминированность: одна и та же последовательность тиков даёт один и тот же результат.

---

## 3) Что делает команда MOVE (50px на command tick)

Команда не задаёт “скорость px/tick” напрямую. Вместо этого она задаёт **план распределения 50px** на `N` sim ticks (обычно `N=30`).

Алгоритм:
- берём `total = 50px * FixedScale`
- `base = total / N` — гарантированный шаг каждого тика
- `remainder = total % N` — сколько тиков получат `+1` fixed-unit

На каждом sim tick:
- шаг = `base`
- если `remainder > 0`, то шаг увеличивается на 1 и `remainder--`
- `ticksLeft--`

Так сумма за N тиков **ровно** равна `total` (если нет коллизий/ограничений по краю).

---

## 4) Прыжок и гравитация (Y)

### Гравитация
Каждый sim tick:
- `_vyFixed += GravityFixed`
- `_vyFixed` ограничивается `MaxFallSpeedFixed` (чтобы не разгоняться бесконечно).

### Прыжок
Прыжок — это **один импульс** вверх:
- разрешён только если игрок стоит на земле/платформе (`_grounded == true`)
- задаёт `_vyFixed = -JumpImpulseFixed`

Дальше движение продолжает идти по гравитации и коллизиям.

---

## 5) Коллизии (платформы) — почему “по осям”

Коллизии решаются раздельно:
1) Двигаемся по X → выталкиваемся из платформ по X.
2) Двигаемся по Y → выталкиваемся из платформ по Y:
   - если падали (`vy > 0`) — приземляемся сверху, `vy = 0`, `_grounded = true`
   - если летели вверх (`vy < 0`) — удар головой, `vy = 0`

Такой подход проще и стабильнее для платформера, чем “одновременное” разрешение.

---

## 6) Moving-platform ride (ехать вместе)

После основной интеграции на тик:
- если игрок стоит на moving-platform (`_groundedPlatform.Kind == MovingPlatform`),
- вычисляется `platformDx = currPlatformX - prevPlatformX`,
- игроку добавляется это смещение по X.

После смещения повторно проверяются X-коллизии (чтобы платформа не “ввезла” игрока в стену/другую платформу).

---

## 7) Опасности (пила/шипы) — swept collision

Опасности проверяются не просто `Intersects`, а с учётом пути за тик:
- берётся `Union(prevPlayerRect, currPlayerRect)`
- и `Union(prevObstacleRect, currObstacleRect)`
- если они пересеклись — game over.

Это защищает от “проскока” опасности на большом шаге.

---

## 8) Где смотреть в коде (точки входа)

- `Controllers/GameController.cs`:
  - `CommandTickDurationMs = 500`
  - `SimulationTicksPerCommand = 30`
  - `StepOneSimulationTick()`
- `Models/GameModel.cs`:
  - `StepSimulationTick()` (порядок: obstacles → physics → ride → hazards)
- `Models/GameModel.Physics.cs`:
  - `StartMove(...)`, `StartJump(...)`
  - `IntegrateAndResolvePlayer()`
  - `ResolveSolidCollisionsX/Y()`
  - `ApplyMovingPlatformRide()`

