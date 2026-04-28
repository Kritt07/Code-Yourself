# Tick system (Simulation + Render)

В проекте есть два независимых "ритма":

- **Command ticks (команды)** — дискретные шаги выполнения программы пользователя. Одна команда исполняется мгновенно и задаёт намерение движения, после чего проигрывается фиксированное число sim-tick'ов.
- **Simulation ticks (логика)** — дискретные шаги симуляции физики/коллизий. В текущей модели: **30 sim ticks на 1 command tick**, при этом один command tick длится **0.5s** (значит sim tick ≈ 16.67ms).
- **Render ticks (отрисовка)** — перерисовка UI (примерно **60Hz**), независимо от логики.

Ниже — как это связано и где реализовано.

---

## 1) Simulation ticks: `GameController` → `GameModel.StepSimulationTick()`

### Основная идея

- Программа пользователя парсится в список `GameCommand` и ставится в очередь контроллера.
- **Одна команда исполняется мгновенно** (задаёт намерение: move/jump/wait и т.п.), а затем **проигрывается 30 simulation ticks** (это и есть один command tick).
- Каждый сим-тик — это один вызов `GameModel.StepSimulationTick()`.

### Где это сделано

Файл: `Controllers/GameController.cs`

- `SimulationTicksPerCommand = 30` — сколько логических шагов выделяется на одну команду (command tick).
- `CommandTickDurationMs = 500` и `SimTickIntervalMs = 500/30` — fixed-step симуляции.
- `_commandTimer` — WinForms `Timer`, который тикает часто (джиттер допустим), а реальный темп логики держится через accumulator + fixed-step.

Ключевой алгоритм `CommandTimer_Tick`:

1. Аккумулировать прошедшее время (Stopwatch) в `accumulatorMs`.
2. Пока `accumulatorMs >= SimTickIntervalMs` — выполнить **ровно один** sim-tick (`StepOneSimulationTick()`), уменьшить `accumulatorMs`.
3. Внутри `StepOneSimulationTick()`:\n+   - если закончились sim-tick'и текущей команды — взять следующую команду, выполнить `Execute(model)`, выставить `_remainingSimulationTicksForCommand = 30`.\n+   - выполнить `model.StepSimulationTick()`, уменьшить `_remainingSimulationTicksForCommand`.\n+   - сгенерировать `LogicFrameCommitted`.

---

## 2) Что делает один сим-тик: `GameModel.StepSimulationTick()`

Файл: `Models/GameModel.cs`

`StepSimulationTick()` — это атомарный логический шаг. Порядок операций важен:

1. **Обновление препятствий**: каждый `IObstacle.Update(SimTickCount)` вызывается с текущим индексом тика.
2. **Физика игрока** (vx/vy + gravity) и коллизии с платформами (раздельно по X затем Y).
3. **Moving-platform ride**: если игрок grounded на moving-platform — он дополнительно смещается на `platformDx` за тик.
4. **Смертельные столкновения (Saw)**:
   - проверяется swept-collision: берётся объединение прямоугольников игрока за тик (`prev ∪ curr`) и препятствия (`prev ∪ curr`);
   - если пересеклись — `IsGameOver = true`.
5. Сохранение `prev`-границ и `SimTickCount++`.

### Как команда превращается в движение по тикам

- `MovePlayer(...)` задаёт горизонтальное намерение так, чтобы за **30 sim ticks** игрок стремился пройти **50px** (с учётом коллизий).
- `JumpPlayer(...)` даёт вертикальный импульс вверх **только если** игрок grounded, дальше движение идёт по физике.

---

## 3) Render ticks: `GameForm` (60Hz) + снапшоты + интерполяция

Файл: `View/GameForm.cs`

### Render timer

`_renderTimer.Interval = 16` — таймер UI, который просто делает `Invalidate()` панели, чтобы WinForms вызвал `Paint`.

### Logic frame commit

`GameController` после каждого sim-tick вызывает событие `LogicFrameCommitted`, а `GameForm` может инициировать перерисовку.

---

## 4) Полезные точки входа

- **Запуск приложения**: `Program.cs` создаёт `GameModel`, `GameController`, `GameForm`.
- **Запуск программы игрока**: `GameForm.RunProgram()` → парсинг → `EnqueueCommand(...)` → `controller.Start()`.

