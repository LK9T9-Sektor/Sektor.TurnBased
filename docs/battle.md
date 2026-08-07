# Модель боя (Battle)

Первая игра на ядре. Проверяет, что ядро (`GamePipeline`, `GameEventBus`, `VisualQueue`, `ContentRegistry`) достаточно для реальной игры. Пошаговая схватка двух команд с героями и призываемыми существами.

## Данные-ресурсы вместо enum-статов

- **`StatDefinition`** — строковое описание стата: `Id`, `DisplayName`, `MinValue`, `MaxValue`, `BaseValue`. Стат — данные, а не enum.
- **`ResourceContainer`** — контейнер текущих значений по Id статов с проверкой границ и событиями изменения.
- **`BattleActor`** — актор (герой или существо): runtime-Id, TeamId, TemplateId, `ResourceContainer`, активные статусы. Своё состояние меняет только через валидирующие методы.
- **`StatusDefinition`/`BattleStatus`** — статусы (например, «Ярость», «Блок»): модификаторы статов (`StatModifier`), длительность, `BlocksTurn` (пропуск хода), тик в начале раунда.

## Эффекты и прекондиции (цепочка обязанностей)

- **`ICombatEffect`** — единица действия: `Apply(context)` меняет состояние и пишет события; `EstimateDamage(context, targetId)` — оценка для AI без побочных эффектов.
  - `DamageEffect` — урон с флэт-митигацией (см. `decisions.md`).
  - `HealEffect` — лечение.
  - `ModifyStatEffect` — модификация стата (срок: немедленно/до конца хода/раунда/боя).
  - `ApplyStatusEffect` — наложение статуса.
  - `SummonEffect` — призыв существа.
- **`ICombatPrecondition`** — условие применимости: `CanApply(context)`.
  - `HasResourcePrecondition`, `SourceAlivePrecondition`, `TargetsAlivePrecondition`.
- **`ActionDefinition`** — действие: `TargetMode`, список `Effects` (по Id), список `Preconditions` (по Id). Эффекты применяются по цепочке, все прекондиции должны пройти.

## Правила (полиморфные стратегии)

- **`IOrderRule`** — порядок ходов: `FixedOrderRule` (фиксированный), `SpeedInitiativeRule` (по инициативе), `TeamAlternationRule` (чередование команд). Выбирается по строковому Id в `BattleConfig`.
- **`IWinCondition`** — условие победы: `ExterminationCondition` (уничтожение команды противника).
- `BattleConfig` — настройки боя: порядок ходов, условие победы, лимит раундов.
- `BattleRules` — сборка правил по конфигу.

## Фазы боя (`BattlePhaseIds`)

Собираются в пайплайн ядра фасадом `BattleEngine`:

- `Setup` — создание акторов из шаблонов.
- `RoundStart` — начало раунда: тик статусов, ротация/порядок ходов.
- `ActorTurn` — ход актора: игрок шлёт команду `UseActionCommand`, AI выбирает действие. Фаза подвешивается (`Suspend`) до команды.
- `BattleEnd` — проверка условий победы / лимита раундов → ничья.

## Исполнитель (`BattleExecutor`)

Применяет действие актора к целям: проверяет прекондиции, исполняет эффекты по цепочке через `ContentRegistry`-резолв. Пишет доменные события (`ActorDied`, `ActorStatChanged`, `RoundStarted`, `BattleEnded`) в шину и визуальные снимки в `VisualQueue`.

## AI (`BattleAi`)

Для вражеских акторов: оценивает урон каждого доступного действия через `EstimateDamage` (пре-коммит, без побочных эффектов), выбирает максимальный по слабейшей цели; при равном уроне — более слабая цель. Возвращает тот же `UseActionCommand`, что и игрок. Детерминированно.

## Фасад (`BattleEngine`)

`BattleEngine.Create(...)` валидирует контент (`ContentValidator`), собирает правила и регистрирует фазы; дальше — обычное управление пайплайном (`Start`/`Advance`/`ProcessCommand`). DI через конструктор/фабрику.

## Детерминизм и валидация

- Весь бой детерминирован при одном seed `DeterministicRng` — тесты, симуляции и replay воспроизводимы.
- `ContentValidator` на этапе `Create` проверяет ссылки по Id, наличие статов, корректность границ — ошибки контента ловятся на старте, а не в бою.
