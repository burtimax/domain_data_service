# Receiver Runbook (MVP)

## 1. Назначение

Документ описывает эксплуатацию receiver-only backend: мониторинг ingestion, диагностику инцидентов, recovery через replay и базовые требования к retention/backup.

## 2. Ops endpoints

- `GET /app/ops/ingestion-status`
  - Возвращает метрики ingestion и текущую глубину очередей (`observation` + `dlq`).
- `POST /app/ops/replay/dlq`
  - Переигрывает сообщения из DLQ обратно в основную очередь.
  - Body: `{ "maxMessages": 100 }` (опционально).
- `POST /app/ops/replay/archive`
  - Переигрывает сообщения из архивного файла `DlqArchiveFilePath`.
  - Body: `{ "maxMessages": 100 }` (опционально).
- `GET /app/health` / `GET /app/readiness`
  - Liveness/readiness проверки сервиса.

## 3. Метрики и алерты

Ключевые поля из `ingestion-status`:
- `metrics.throughputPerSecond` — текущий средний throughput по ack.
- `metrics.duplicates` — количество дедуплицированных сообщений.
- `metrics.averageLagMs` — средний lag между `ObservedAt` и обработкой.
- `metrics.failed` / `metrics.deadLettered` — количество ошибок и переводов в DLQ.
- `queue.dlqQueueDepth` — текущий backlog DLQ.

Рекомендуемые алерты MVP:
- `failed` растёт быстрее `acked` > 5 минут.
- `dlqQueueDepth > 0` более 10 минут.
- `averageLagMs` растёт непрерывно (признак деградации upstream/consumer).
- `readiness != ready`.

## 4. Политика retries и poison messages

- Для transient ошибок используется retry через репаблиш в основную очередь.
- Счётчик попыток передаётся в header `x-retry-count`.
- Максимум попыток: `RabbitMq:MaxRetryAttempts`.
- При исчерпании retry сообщение переводится в DLQ и записывается в архив (`DlqArchiveFilePath`).
- Non-retryable ошибки сразу отправляются в DLQ и архив.

## 5. Процедуры восстановления

### 5.1 Replay из DLQ

1. Убедиться, что причина сбоя устранена (например, БД снова доступна).
2. Проверить `GET /app/ops/ingestion-status`, что `dlqQueueDepth > 0`.
3. Запустить `POST /app/ops/replay/dlq` с ограниченным батчем (например, 50-100).
4. Проверить уменьшение `dlqQueueDepth`, рост `acked`, отсутствие всплеска `failed`.
5. Повторять батчами до очистки DLQ.

### 5.2 Replay из архива

Используется, если сообщения были удалены из DLQ или требуется повторная обработка исторического сбоя:
1. Проверить доступность архивного файла.
2. Запустить `POST /app/ops/replay/archive`.
3. Мониторить `failed/deadLettered` и состояние очереди.

## 6. Retention политика (MVP)

- RabbitMQ primary queue: краткосрочный буфер, без долгого хранения.
- RabbitMQ DLQ: хранить до ручного разбора/реплея (операционный SLA, например 7-14 дней).
- Archive (`DlqArchiveFilePath`): хранить 30 дней, затем ротация/удаление.
- `receiver_processing_logs` в БД: хранить минимум 30 дней для расследования инцидентов.
- Application logs: хранить 14-30 дней в централизованном лог-хранилище.

## 7. Backup и smoke restore (операционный контракт)

Это настраивается во внешней инфраструктуре (не в коде приложения):
- Ежедневный backup PostgreSQL (`app` schema).
- Хранение backup минимум 14 дней.
- Еженедельный smoke restore в отдельную test БД:
  - восстановление последнего backup;
  - запуск read-запросов на `domains/auctions/results`;
  - проверка количества строк в ключевых таблицах (`auctions`, `auction_observations`, `receiver_processing_logs`).

## 8. Runbook инцидентов

### Инцидент: резкий рост `failed`/`dlq`

1. Проверить `readiness` и доступность БД.
2. Проверить последние ошибки в логах по `MessageId`.
3. Определить тип ошибки:
   - валидация payload (non-retryable),
   - transient инфраструктура (retryable).
4. После фикса причины выполнить replay из DLQ батчами.

### Инцидент: высокий processing lag

1. Проверить `queue.observationQueueDepth` и prefetch.
2. Проверить нагрузку БД и latency.
3. При необходимости временно снизить входной поток upstream.
4. После стабилизации наблюдать `averageLagMs` до нормализации.

## 9. Подключение нового upstream producer

1. Producer должен публиковать `ObservationIngestionMessage` v1 в `ObservationExchange`/`ObservationRoutingKey`.
2. Не изменять receiver-код, если соблюдён контракт:
   - обязательные поля (`MessageId`, `SchemaVersion`, `SourcePlatformCode`, `ExternalAuctionId`, `DomainNameOriginal`, `ObservedAt`);
   - корректные значения цены/валюты.
3. Для нового источника задаётся новый `SourcePlatformCode`; запись платформы создаётся автоматически при первом сообщении.
4. Перед включением в production выполнить dry-run на тестовом окружении и проверить:
   - отсутствие non-retryable ошибок,
   - стабильный lag,
   - корректную запись read-модели.
