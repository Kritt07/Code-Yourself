# Tick system (Simulation + Render)

В проекте есть два независимых "ритма":

- **Simulation ticks (логика)** — дискретные шаги симуляции (условно **30Hz**), где обновляются препятствия, движение игрока и коллизии.
- **Render ticks (отрисовка)** — перерисовка UI (примерно **60Hz**) со сглаживанием между двумя последними логическими снапшотами.

Ниже — как это связано и где реализовано.

---

## 1) Simulation ticks: `GameController` → `GameModel.StepSimulationTick()`

### Основная идея

- Программа пользователя парсится в список `GameCommand` и ставится в очередь контроллера.
- **Одна команда исполняется мгновенно** (задаёт намерение: move/jump/wait и т.п.), а затем **проигрывается 30 симуляционных тиков**.
- Каждый сим-тик — это один вызов `GameModel.StepSimulationTick()`.

### Где это сделано

Файл: `Controllers/GameController.cs`

- `SimulationTicksPerCommand = 30` — сколько логических шагов выделяется на одну команду.
- `_commandTimer` — WinForms `Timer`, который "подтягивает" симуляцию.
- `CommandTimer_Tick(...)` — один "тик" таймера, который либо делает один сим-тик, либо переключает команду.

Ключевой алгоритм `CommandTimer_Tick`:

1. Если `model.IsGameOver` — остановить таймер и дать UI финально перерисоваться.
2. Если идёт проигрывание команды (`_remainingSimulationTicksForCommand > 0`) — выполнить **ровно один** `StepSimulationTick()` и сгенерировать событие `LogicFrameCommitted`.
3. Иначе — взять следующую `GameCommand`, выполнить `command.Execute(model)` и выставить `_remainingSimulationTicksForCommand = 30`.

---

## 2) Что делает один сим-тик: `GameModel.StepSimulationTick()`

Файл: `Models/GameModel.cs`

`StepSimulationTick()` — это атомарный логический шаг. Порядок операций важен:

1. **Обновление препятствий**: каждый `IObstacle.Update(SimTickCount)` вызывается с текущим индексом тика.
2. **Движение игрока** на один сим-тик согласно "намерениям" (pending move / active jump).
3. **Платформы (MovingPlatform)**:
   - если игрок стоял на платформе в прошлом тике — он "едет" вместе с ней (компенсация `platformDx`);
   - если игрок пересёкся с платформой и раньше был над её верхом — "приземление" сверху.
4. **Смертельные столкновения (Saw)**:
   - проверяется swept-collision: берётся объединение прямоугольников игрока за тик (`prev ∪ curr`) и препятствия (`prev ∪ curr`);
   - если пересеклись — `IsGameOver = true`.
5. Сохранение `prev`-границ и `SimTickCount++`.

### Как команда превращается в движение по тикам

- `MovePlayer(...)` не двигает игрока моментально, а задаёт:
  - `_pendingMoveDx` — остаток смещения по X,
  - `_pendingMoveSimTicksLeft` — сколько тиков нужно "растянуть" движение.
- `ApplyPlayerMovementForSimulationTick()` каждый сим-тик выбирает шаг и уменьшает остаток.

Прыжок (`JumpPlayer`) работает как отдельная state-machine (`JumpState`): каждый сим-тик вычисляет точку по дуге и продвигает `SubTickIndex`.

---

## 3) Render ticks: `GameForm` (60Hz) + снапшоты + интерполяция

Файл: `View/GameForm.cs`

### Render timer

`_renderTimer.Interval = 16` — таймер UI, который просто делает `Invalidate()` панели, чтобы WinForms вызвал `Paint`.

### Logic snapshot commit

`GameController` после каждого сим-тика вызывает событие `LogicFrameCommitted`.

`GameForm` в обработчике `Controller_LogicFrameCommitted()`:

- сохраняет пару снапшотов: `_prevSnapshot` и `_currSnapshot` (оба — `GameModel.RenderSnapshot`),
- измеряет интервал между commit'ами (для стабильного расчёта alpha).

### Интерполяция (сглаживание)

В `GamePanel_Paint`:

- вычисляется `alpha` в диапазоне `[0..1]` как отношение времени после последнего коммита к ожидаемому интервалу коммита,
- прямоугольники игрока и препятствий рисуются как `LerpRect(prev, curr, alpha)`.

Итог: даже если логика идёт дискретно, визуально движения выглядят более плавными.

---

## 4) Полезные точки входа

- **Запуск приложения**: `Program.cs` создаёт `GameModel`, `GameController`, `GameForm`.
- **Запуск программы игрока**: `GameForm.RunProgram()` → парсинг → `EnqueueCommand(...)` → `controller.Start()`.

