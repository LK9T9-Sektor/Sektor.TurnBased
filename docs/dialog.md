# Фазовый диалог/квест (Dialog)

Вторая игра на ядре (ADR-008). Цель — доказать, что ядро — фреймворк, а не спец-библиотека Battle: диалог принципиально отличается от боя (нет акторов/статов/урона), но использует тот же `GamePipeline`, `GameEventBus`, `VisualQueue`, `ContentRegistry` и `Result`.

## Модель (данные-ресурсы)

- **`DialogNodeDefinition`** — узел: текст, варианты ответа (`Choices`), флаги для входа/выдачи (`RequiresFlags`/`GrantsFlags`), либо триггер вложенного диалога (`SubDialogId` + `ContinueNodeId`). Узел без вариантов и без `SubDialogId` — концовка (Outcome).
- **`DialogChoiceDefinition`** — вариант ответа: текст, флаги-прекондиции, выдаваемые флаги, следующий узел.
- **`DialogState`** — текущий узел, набор флагов, посещённые узлы, исход. Реализует `IGameState`.

Ветвление — это данные (флаги и ссылки на узлы), а не switch по коду: новый маршрут = новая запись в каталоге.

## Фазы

- `Setup` — ставит стартовый узел (для вложенных диалогов — через `startOverride`).
- `Flow` — ведёт диалог:
  - обычный узел → `NodeShown` + `Suspend("awaiting_choice")`;
  - выбор игрока (`ChooseOptionCommand`) → выдаёт флаги, меняет текущий узел, `Resume()`;
  - концовка → `Finish()` (исход родителя, `DialogEnded`);
  - узел с `SubDialogId` → **запускает дочерний пайплайн** (`CreateChildPipeline`), ждёт его завершения (`Resume()`), затем продолжает на `ContinueNodeId`.

## Вложенный диалог (child pipeline)

Ключевая проверка ядра: Battle не использует вложенные пайплайны вообще. Диалог использует их для «диалога внутри диалога» (загадка сфинкса в демо):

- родительский `Flow` создаёт дочерний пайплайн, регистрирует его фазы и `Start`;
- `GamePipeline.Advance` сам продвигает активного ребёнка, а `ProcessCommand` маршрутизирует команды в подвешенный дочерний диалог;
- когда ребёнок завершён (`Finish`), родительская фаза перевыполняется и продолжает на `ContinueNodeId`;
- режим `isSubDialog` в `Flow`: концовка ребёнка завершает только дочерний пайплайн и не трогает исход родителя.

## События и визуализация

- `NodeShown`, `ChoiceChosen`, `SubDialogEntered`, `SubDialogCompleted`, `DialogEnded` — доменные события через `GameEventBus` (`Before`/`After` + `applyBase`).
- `DialogEventSink` пишет `VisualEvent`-снимки (`NodeText`, `Choice`, `Ending`, `SubDialogEnter`, `SubDialogComplete`) в `VisualQueue` и строки в `GameLog`.
- Хук `Before` может отменить событие (тест: отмена `NodeShown` не ломает прохождение).

## Команды

- `ChooseOptionCommand(NodeId, ChoiceId)` — выбор варианта. Проверяется соответствие текущему узлу, существование варианта и прекондиции по флагам.

## Фасад и контент

- `DialogEngine.Create` валидирует контент (`DialogContentValidator`: дубликаты, все ссылки узлов/sub-dialog/continue, объявленные флаги) и регистрирует фазы.
- `DialogContentCatalog` — демо-квест «Ворота крепости»: ветвления по флагам (`papers_stolen`), вложенная загадка (`riddle_key`), 5 концовок.
