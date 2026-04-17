# Dev Plan (MVP): Receiver-only backend

Документ адаптирован под текущую структуру проекта и режим разработки **только Receiver** (без задач парсинга NickSell и без parser-модулей).

## Текущая структура, на которую опирается план

- `Api` — вход в систему, middleware, endpoints, DI, конфигурация.
- `Application` — сервисы и бизнес-логика use-case уровня.
- `Infrastructure` — EF Core, `AppDbContext`, сущности, миграции, persistence.
- `Shared` — конфиги, контракты результата, модели, утилиты.
- `SharedServices` — общие сервисы (при необходимости).
- `ConsoleTest` — тестовые/вспомогательные консольные сценарии.

## Правила ведения плана

- [ ] Выполнять эпики последовательно с учётом зависимостей.
- [ ] Перед началом задачи менять чекбокс на `[-]`.
- [ ] После завершения ставить `[x]` и фиксировать артефакт (PR/док/миграция/тест).
- [ ] Если требуется решение по архитектуре — помечать `[!]`.

Статусы:
- `[ ]` не начато
- `[-]` в работе
- `[x]` завершено
- `[!]` блокер

---

## Epic 1. Receiver scope и спецификации (без парсеров)

**Цель:** зафиксировать контракт и поведение receiver-части как самостоятельного backend-сервиса.

### 1.1 Scope Receiver-only MVP
- [x] Утвердить границы: проект принимает observation-сообщения, валидирует, сохраняет, отдаёт read API.
- [x] Зафиксировать, что логика сбора/парсинга источников вне этого репозитория.
- [x] Утвердить минимальные сценарии: valid message, duplicate, out-of-order, terminal status.

### 1.2 Observation contract v1
- [x] Разделить поля контракта на required/optional.
- [x] Утвердить типы, nullable, формат `ObservedAt`.
- [x] Утвердить правила версионирования `SchemaVersion`.
- [x] Добавить примеры valid/invalid payload.

### 1.3 Стратегия идемпотентности
- [x] Утвердить dedupe по `MessageId`.
- [x] Утвердить semantic dedupe (fingerprint) и окно проверки.
- [x] Утвердить правила обработки устаревших сообщений.

### 1.4 Финализация статусов
- [x] Утвердить canonical статусы и terminal precedence.
- [x] Утвердить бизнес-правило фиксации финального статуса/финальной цены.
- [x] Утвердить политику для `disappeared`.

### 1.5 API v1 для receiver/read-model
- [x] Утвердить ingestion endpoint(s) для приёма observation (если приём через HTTP нужен).
- [x] Утвердить read endpoint’ы для доменов/торгов/результатов.
- [x] Утвердить формат ошибок и контракт ответов.

**DoD эпика:** согласованная спецификация Receiver-only v1 готова к реализации.

---

## Epic 2. Подготовка текущего solution под Receiver-only

**Зависимость:** Epic 1.

### 2.1 Выравнивание архитектуры по текущим проектам
- [x] Зафиксировать ответственность слоёв `Api` / `Application` / `Infrastructure` / `Shared`.
- [x] Удалить/изолировать нецелевые зависимости, не относящиеся к receiver-domain.
- [!] Подготовить технический ADR по выбранной структуре (оставляем `Api` как host receiver или создаём `Receiver.Worker`) — требуется отдельное архитектурное решение владельца проекта.

### 2.2 Конфигурации и environment
- [x] Дополнить `appsettings` параметрами receiver-пайплайна и RabbitMQ.
- [x] Описать env vars и секреты.
- [x] Добавить валидацию конфигурации на старте.

### 2.3 Базовые cross-cutting механизмы
- [x] Структурированные логи.
- [x] Корреляция по `MessageId`/`TraceId`.
- [x] Единый формат ошибок уровня API/processing.

**DoD эпика:** solution готов к реализации receiver-пайплайна без parser-задач.

---

## Epic 3. Данные и БД в `Infrastructure`

**Зависимость:** Epic 2.

### 3.1 Модель данных receiver-домена
- [x] Спроектировать и утвердить ER-модель сущностей: `Domain`, `SourcePlatform`, `Auction`, `AuctionObservation`, `AuctionStatusHistory`, `ReceiverProcessingLog`.
- [x] Утвердить опциональность `DomainPriceHistory` для MVP.

### 3.2 Реализация в `AppDbContext`
- [x] Добавить `DbSet` и конфигурации сущностей.
- [x] Настроить snake_case, схемы, фильтры, soft-delete совместимо с текущей базой.
- [x] Настроить связи и ограничения целостности.

### 3.3 Индексы и уникальность
- [x] Уникальность `Domain.NameNormalized`.
- [x] Уникальность `(SourcePlatformId, ExternalAuctionId)` для `Auction`.
- [x] Уникальность `AuctionObservation.MessageId`.
- [x] Индексы для `ObservedAt`, `Status`, `EndAt`, `NamePunycode`.

### 3.4 IDN и нормализация доменов
- [x] Реализовать хранение original/normalized/punycode/tld/sld.
- [ ] Подготовить тест-набор IDN кейсов.

### 3.5 Миграции
- [x] Сформировать миграции EF Core.
- [!] Проверить применение миграций при старте (`Api/Program.cs`) и локально через команды (локальный `dotnet ef database update` упирается в `28P01` на текущих credentials `postgres`; автоприменение на старте оставлено через `db.Database.Migrate()`).
- [x] Обновить `Infrastructure/migration_commands.md`.

**DoD эпика:** receiver-схема БД полностью описана и применима миграциями.

---

## Epic 4. Ingestion транспорт (RabbitMQ и/или HTTP)

**Зависимость:** Epic 2 и Epic 3.

### 4.1 RabbitMQ integration
- [x] Описать топологию exchange/queue/routing key для observation.
- [x] Реализовать consumer с manual ack.
- [x] Реализовать поведение nack/requeue/dlq для разных классов ошибок.

### 4.2 Вариант HTTP ingestion (если нужен в MVP)
- [!] Реализовать endpoint приёма observation в `Api` (отложено: в текущем MVP включён только RabbitMQ ingest, HTTP канал не активирован).
- [!] Сделать идентичную валидацию контракта как в queue flow (валидатор реализован в общем `Application` use-case, endpoint не включён).
- [!] Ограничить endpoint как internal-only (применимо после включения HTTP ingest).

### 4.3 Общий ingestion слой
- [x] Унифицировать входные модели из queue/http в единый `Application` use-case.
- [x] Добавить метрики по входящему потоку.

**DoD эпика:** сообщения стабильно попадают в receiver pipeline и корректно подтверждаются/отклоняются.

---

## Epic 5. Receiver pipeline в `Application`

**Зависимость:** Epic 3 и Epic 4.

### 5.1 Validation
- [x] Schema validation.
- [x] Business validation.

### 5.2 Dedupe / idempotency
- [x] Проверка duplicate по `MessageId`.
- [x] Проверка semantic duplicate по fingerprint.
- [x] Логирование решения dedupe в `ReceiverProcessingLog`.

### 5.3 Normalization
- [x] Domain normalization.
- [x] Status normalization.
- [x] Currency/time normalization.

### 5.4 Matching + persistence
- [x] Match/Create `Domain`.
- [x] Match/Create `Auction`.
- [x] Update current snapshot `Auction`.
- [x] Append `AuctionObservation`.
- [x] Append `AuctionStatusHistory` (и `DomainPriceHistory`, если включено).

### 5.5 Out-of-order и terminal rules
- [x] Приоритет terminal статусов.
- [x] Защита от поздних устаревших сообщений.
- [x] Корректная фиксация финального состояния.

**DoD эпика:** pipeline receiver-а идемпотентен и устойчив к дублям/рассинхрону порядка.

---

## Epic 6. API слой в `Api` (read + ops)

**Зависимость:** Epic 5.

### 6.1 Read endpoints
- [x] Реализовать endpoints списка/карточки доменов.
- [x] Реализовать endpoints списка/карточки торгов.
- [x] Реализовать endpoints истории наблюдений/статусов/цен.
- [x] Реализовать endpoint финальных результатов.

### 6.2 Ops endpoints
- [x] `health/readiness` endpoint.
- [ ] Технический endpoint состояния очереди/ingestion (опционально).

### 6.3 Контракты и pagination
- [x] Единый формат ответа.
- [x] Фильтрация/сортировка/пагинация.
- [x] Единый формат ошибок.

**DoD эпика:** API предоставляет консистентную read-модель receiver данных.

---

## Epic 7. Надёжность и эксплуатация

**Зависимость:** Epic 5 и Epic 6.

### 7.1 Наблюдаемость
- [x] Метрики: throughput, duplicates, processing lag, failed count.
- [x] Логи по шагам pipeline.
- [x] Корреляция ошибок с `MessageId`.

### 7.2 Ошибки и recovery
- [x] Ретраи для transient ошибок.
- [x] Политика обработки poison messages.
- [x] Процедура replay из DLQ/архива.

### 7.3 Data retention и backup
- [x] Утвердить retention для raw/logs/processing log.
- [!] Настроить backup и smoke restore (требуется внешняя инфраструктурная настройка расписания backup/restore-job вне кода приложения; процедура и критерии smoke-restore описаны в `Docs/receiver-runbook.md`).

### 7.4 Эксплуатационные инструкции
- [x] Runbook инцидентов receiver-сервиса.
- [x] Инструкция подключения нового upstream producer без изменений parser-кода в этом репо.

**DoD эпика:** receiver готов к эксплуатации в MVP режиме.

---

## Epic 8. Тестирование и приемка Receiver-only MVP

**Зависимость:** Epic 3-7.

### 8.1 Unit tests
- [ ] Нормализация доменов/статусов.
- [ ] Идемпотентность и dedupe.
- [ ] Финализация статусов.

### 8.2 Integration tests
- [ ] EF Core mappings и constraints.
- [ ] Ingestion + pipeline + БД.

### 8.3 E2E tests
- [ ] `Producer(simulated) -> RabbitMQ/HTTP -> Receiver -> PostgreSQL -> Read API`.
- [ ] Duplicate delivery.
- [ ] Out-of-order delivery.
- [ ] Terminal/disappeared сценарии.

### 8.4 Приемка
- [ ] Receiver принимает observation-сообщения.
- [ ] Идемпотентно обрабатывает дубли и перестановку порядка.
- [ ] Корректно фиксирует текущие и финальные состояния торгов.
- [ ] Read API отдает корректные данные.
- [ ] Логи/метрики достаточны для диагностики.

**DoD эпика:** Receiver-only MVP принят.

---

## Очередность работ для агентов

- [ ] Волна A: `Epic 1` (спеки и решения).
- [ ] Волна B: `Epic 2 + Epic 3` (архитектурная подготовка + БД).
- [ ] Волна C: `Epic 4 + Epic 5` (ingestion + pipeline).
- [ ] Волна D: `Epic 6` (read/ops API).
- [ ] Волна E: `Epic 7 + Epic 8` (hardening + тесты + приемка).

Правило: переход к следующей волне только после `DoD` предыдущей.

## Baseline

- В `DomainParser.sln` присутствуют все целевые проекты: `Api`, `Application`, `Infrastructure`, `Shared`, `SharedServices`, `ConsoleTest`.
- Стартовая композиция в `Api/Program.cs` уже включает DI, FastEndpoints, Swagger и авто-применение миграций `AppDbContext`.
- Слой данных в `Infrastructure/Db/App/AppDbContext.cs` реализует базовые паттерны: schema/snake_case/filter hooks и soft-delete через `SaveChangesAsync`.
- Распределение по слоям в целом соответствует целевой архитектуре, но в `Api` пока остаются сервисы вне явного receiver-domain (`Test`/`SMSGateway`).
- Для Epic 1 спецификация receiver-контрактов и правил обработки пока не зафиксирована в коде/документации как утверждённый v1.
- Для Epic 2 есть частичная техническая база (startup wiring, middleware), но отсутствуют завершённые ADR и формализация cross-cutting стандартов.
