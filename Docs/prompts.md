# Prompts для поэтапной разработки Receiver-only проекта

Ниже готовые промпты, которые можно последовательно давать агенту разработки.  
Каждый промпт ограничивает объём изменений, указывает целевые файлы и требует синхронизацию статуса в `Docs/dev-plan.md`.

## Общий системный пролог (давать перед каждой итерацией)

```text
Ты работаешь в проекте Receiver-only backend.
Главный источник задач: Docs/dev-plan.md.

Обязательные правила:
1) Перед началом прочитай Docs/dev-plan.md и определи только текущий целевой блок задач.
2) Не выходи за рамки текущей итерации и не делай “следующие этапы” заранее.
3) После выполнения обнови статусы чекбоксов в Docs/dev-plan.md только для реально завершённых пунктов.
4) Если задача неоднозначна — добавь пометку [!] в соответствующем пункте плана и кратко опиши блокер.
5) Не трогай нерелевантные файлы. Работай только в указанных файлах/папках.
6) После изменений проверь сборку/тесты затронутых проектов.
7) В финале отчитайся: что сделано, какие файлы изменены, какие пункты в dev-plan закрыты, что осталось.

Ограничение контекста:
- Если итерация получается слишком большой, остановись на логически завершённом подэтапе.
- Не изменяй более 20-25 файлов за итерацию без явной необходимости.
```

---

## Итерация 0. Аудит и стартовый baseline

```text
Выполни стартовый аудит проекта перед разработкой.

Цель:
- Проверить текущую структуру solution и сопоставить её с Docs/dev-plan.md.
- Подготовить краткий baseline-отчёт для дальнейших итераций.

Что сделать:
1) Прочитай Docs/dev-plan.md.
2) Проверь структуру проектов: Api, Application, Infrastructure, Shared, SharedServices, ConsoleTest.
3) Определи, какие пункты Epic 1/2 уже частично реализованы.
4) Добавь в Docs/dev-plan.md статусы: [x] где уже сделано, [ ] где нет, [!] где есть неоднозначность.
5) Добавь короткий раздел "Baseline" в конец Docs/dev-plan.md (3-7 пунктов).

Файлы для работы:
- Docs/dev-plan.md
- DomainParser.sln
- Api/Program.cs
- Api/Extensions/IServiceCollectionExtensions.cs
- Application/Extensions/IServiceCollectionExtensions.cs
- Infrastructure/Db/App/AppDbContext.cs

Критерий завершения:
- Есть актуализированный dev-plan со стартовыми статусами и baseline.
```

---

## Итерация 1. Спецификации Receiver (Epic 1)

```text
Реализуй только спецификационный блок Epic 1 из Docs/dev-plan.md.

Цель:
- Зафиксировать однозначные правила receiver: контракт observation, идемпотентность, финализация статусов, API v1 рамки.

Что сделать:
1) Оформи отдельный документ спецификации в Docs (создай новый markdown).
2) Включи: required/optional поля observation, валидации, правила SchemaVersion, dedupe (MessageId + fingerprint), out-of-order политику, terminal status precedence.
3) Зафиксируй формат ошибок ingestion/read API на уровне спецификации.
4) Обнови чекбоксы Epic 1 в Docs/dev-plan.md.

Файлы для работы:
- Docs/dev-plan.md
- Docs/receiver-spec-v1.md (новый файл)
- Docs/ТЗ для проекта.md (как источник требований)

Не делать:
- Не писать production-код.
- Не менять код в Api/Application/Infrastructure.

Критерий завершения:
- Epic 1 в плане закрыт или помечен [!] с понятными блокерами.
```

---

## Итерация 2. Архитектурная подготовка solution (Epic 2)

```text
Реализуй Epic 2 из Docs/dev-plan.md: подготовка текущего solution под Receiver-only.

Цель:
- Привести composition root и конфигурацию к ясной receiver-архитектуре.

Что сделать:
1) Проверить и привести DI-регистрации к актуальному receiver-назначению.
2) Добавить/уточнить конфиги receiver и RabbitMQ в appsettings (без секретов в репозитории).
3) Добавить раннюю валидацию обязательных конфигов на старте приложения.
4) Уточнить и структурировать логирование (минимум: correlation по MessageId/TraceId).
5) Обновить статусы Epic 2 в Docs/dev-plan.md.

Файлы для работы (приоритет):
- Api/Program.cs
- Api/Extensions/IServiceCollectionExtensions.cs
- Api/appsettings.json
- Api/appsettings.Development.example.json
- Shared/Configs/*
- Docs/dev-plan.md

Критерий завершения:
- Проект стартует с валидной конфигурацией и понятной структурой DI под receiver.
```

---

## Итерация 3. Доменная модель и БД (Epic 3, часть A)

```text
Реализуй первую часть Epic 3: сущности, DbContext, конфигурации.

Цель:
- Добавить минимально достаточную receiver-модель данных в Infrastructure.

Что сделать:
1) Добавить/обновить сущности receiver-домена (Domain, SourcePlatform, Auction, AuctionObservation, AuctionStatusHistory, ReceiverProcessingLog).
2) Настроить связи, ограничения, индексы на уровне EF конфигураций.
3) Встроить это в AppDbContext.
4) Не реализовывать пока ingestion pipeline и API-эндпоинты.
5) Обновить чекбоксы соответствующих пунктов Epic 3 в Docs/dev-plan.md.

Файлы для работы:
- Infrastructure/Db/App/AppDbContext.cs
- Infrastructure/Db/App/AppDbContext.ConfigurationMethods.cs
- Infrastructure/Db/App/Entities/*
- Docs/dev-plan.md

Критерий завершения:
- Модель компилируется и логически завершена на уровне кода.
```

---

## Итерация 4. Миграции и проверка схемы (Epic 3, часть B)

```text
Заверши Epic 3: миграции, индексы, проверка применения.

Цель:
- Получить применяемую БД-схему и инструкции по миграциям.

Что сделать:
1) Сгенерировать миграцию(и) EF Core для новых сущностей.
2) Убедиться, что миграции применяются на старте (или документировать выбранный способ).
3) Обновить Infrastructure/migration_commands.md под реальное состояние.
4) Добавить seed SourcePlatform (минимальный baseline, без parser-логики).
5) Обновить статусы Epic 3 в Docs/dev-plan.md.

Файлы для работы:
- Infrastructure/* (миграции и db код)
- Infrastructure/migration_commands.md
- Api/Program.cs
- Docs/dev-plan.md

Критерий завершения:
- Миграции создаются и применяются без ручных правок SQL.
```

---

## Итерация 5. Ingestion транспорт (Epic 4)

```text
Реализуй Epic 4: ingestion транспорт для receiver.

Цель:
- Подключить приём observation через RabbitMQ (и HTTP ingest только если явно нужен).

Что сделать:
1) Реализовать RabbitMQ consumer с manual ack.
2) Определить и реализовать классы ошибок: retryable/non-retryable.
3) Реализовать базовую политику nack/requeue/dlq.
4) При необходимости добавить internal HTTP ingestion endpoint с тем же контрактом.
5) Подготовить минимальные метрики/логи входящего потока.
6) Обновить статусы Epic 4 в Docs/dev-plan.md.

Файлы для работы:
- Api/* (host/background services/endpoints)
- Application/* (ingestion use-case interfaces)
- Infrastructure/* (transport implementation)
- Shared/* (контракты/настройки)
- Docs/dev-plan.md

Критерий завершения:
- Сообщение поступает в систему и передаётся в application-level обработчик.
```

---

## Итерация 6. Receiver pipeline (Epic 5)

```text
Реализуй Epic 5: основной pipeline обработки observation.

Цель:
- Полный цикл: validate -> dedupe -> normalize -> match -> persist -> processing log.

Что сделать:
1) Добавить schema/business validation.
2) Добавить dedupe по MessageId и semantic dedupe по fingerprint.
3) Реализовать normalizers: domain/status/currency-time.
4) Реализовать upsert/match логику для Domain/Auction.
5) Реализовать append observation + status history.
6) Реализовать out-of-order и terminal precedence правила.
7) Обновить статусы Epic 5 в Docs/dev-plan.md.

Файлы для работы:
- Application/Services/*
- Application/Models/*
- Infrastructure/Db/App/*
- Shared/Contracts/*
- Docs/dev-plan.md

Критерий завершения:
- Повторные и несортированные сообщения обрабатываются корректно и идемпотентно.
```

---

## Итерация 7. Read API и ops endpoints (Epic 6)

```text
Реализуй Epic 6: read API поверх данных receiver-а.

Цель:
- Дать стабильный доступ к доменам, торгам, наблюдениям и финальным результатам.

Что сделать:
1) Добавить endpoints для lists/details по доменам и торгам.
2) Добавить endpoints для observations/status-history/results.
3) Добавить пагинацию, фильтрацию, сортировку.
4) Привести ответы к единому контракту.
5) Проверить и при необходимости обновить health/readiness endpoint.
6) Обновить статусы Epic 6 в Docs/dev-plan.md.

Файлы для работы:
- Api/Endpoints/**
- Api/BaseResponse.cs
- Application/Services/**
- Infrastructure/** (read queries/repositories)
- Docs/dev-plan.md

Критерий завершения:
- Read API закрывает сценарии Epic 6 из плана.
```

---

## Итерация 8. Надёжность и эксплуатация (Epic 7)

```text
Реализуй Epic 7: hardening и эксплуатационная готовность.

Цель:
- Сделать receiver устойчивым и удобным в сопровождении.

Что сделать:
1) Добавить метрики throughput/duplicates/lag/failures.
2) Доработать обработку ошибок и retry-политику.
3) Добавить процедуру replay из DLQ/архива (минимальный рабочий вариант).
4) Документировать runbook инцидентов.
5) Уточнить retention/backup подход и зафиксировать в Docs.
6) Обновить статусы Epic 7 в Docs/dev-plan.md.

Файлы для работы:
- Api/*
- Application/*
- Infrastructure/*
- Docs/dev-plan.md
- Docs/receiver-runbook.md (новый файл)

Критерий завершения:
- Есть техническая и документальная готовность к эксплуатации MVP.
```

---

## Итерация 9. Тестирование и приемка (Epic 8)

```text
Реализуй Epic 8: тесты и финальная приемка Receiver-only MVP.

Цель:
- Подтвердить корректность receiver-системы тестами и закрыть критерии приемки.

Что сделать:
1) Добавить unit-тесты нормализации, дедупликации, финализации.
2) Добавить integration-тесты для БД и ingestion->pipeline.
3) Добавить e2e сценарии duplicate/out-of-order/terminal.
4) Сформировать check-list приемки и отметить его в Docs/dev-plan.md.
5) Подготовить финальный краткий отчёт по готовности MVP.

Файлы для работы:
- ConsoleTest/* (если используется как тестовый хост)
- Проекты с тестами (создать при необходимости)
- Docs/dev-plan.md
- Docs/mvp-acceptance-report.md (новый файл)

Критерий завершения:
- Все пункты Epic 8 закрыты или помечены обоснованными [!].
```

---

## Универсальный промпт “продолжи с текущего места”

```text
Продолжи разработку Receiver-only проекта с текущего состояния.

Порядок работы:
1) Прочитай Docs/dev-plan.md и определи первый незавершённый пункт.
2) Выполни только одну логическую группу связанных задач (ограничься разумным объёмом контекста).
3) Обнови статусы в Docs/dev-plan.md.
4) Запусти проверку сборки/тестов для затронутых модулей.
5) Дай отчёт: выполненные пункты, изменённые файлы, оставшиеся следующие шаги.

Ограничения:
- Не переходи к следующему эпику, пока текущий не закрыт по DoD.
- Не делай parser-задачи и не добавляй функциональность парсинга.
```

