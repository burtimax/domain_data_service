# Receiver Specification v1

Статус: `approved-for-implementation`  
Область: Receiver-only backend (ingestion + processing + read API)  
Источник требований: `Docs/ТЗ для проекта.md`

## 1. Scope и границы

### 1.1 In scope (MVP)
- Прием observation-сообщений (RabbitMQ как основной транспорт, HTTP ingestion как опциональный канал).
- Валидация схемы и бизнес-валидация observation.
- Идемпотентная обработка (duplicate + out-of-order).
- Нормализация доменов/статусов/времени/валюты.
- Сохранение текущего состояния торгов и истории наблюдений.
- Read API для доменов, торгов, финальных результатов и истории.

### 1.2 Out of scope
- Логика парсинга источников, polling, anti-bot, retry парсеров.
- Прямой доступ парсеров к БД receiver.
- Реализация UI и внешнего публичного API с расширенной auth-моделью.

### 1.3 Минимальные сценарии для v1
- Valid message -> принят и обработан.
- Duplicate message (тот же `MessageId`) -> не приводит к повторному изменению состояния.
- Out-of-order message (`ObservedAt` старее уже обработанного) -> обрабатывается по правилам stale/terminal.
- Terminal status message -> фиксирует финальное состояние согласно precedence.

## 2. Observation Contract v1

## 2.1 Формат и общие правила
- Формат: JSON object, UTF-8.
- Поле `SchemaVersion` обязательно и должно соответствовать семантике v1 (`1`, `1.0`, `1.0.x`).
- Все даты/время передаются в UTC в формате ISO 8601 (`yyyy-MM-ddTHH:mm:ss.fffZ`).
- Денежные значения передаются decimal, не float.

### 2.2 Required поля
- `MessageId` (`string`, UUID/ULID, non-empty)
- `SchemaVersion` (`string`)
- `ParserCode` (`string`, non-empty)
- `ParserVersion` (`string`, non-empty)
- `SourcePlatformCode` (`string`, non-empty)
- `ObservedAt` (`string`, ISO 8601 UTC)
- `ExternalAuctionId` (`string`, non-empty)
- `DomainNameOriginal` (`string`, non-empty)
- `AuctionStatusRaw` (`string`, non-empty)

### 2.3 Optional поля
- `ExternalDomainId` (`string|null`)
- `DomainNameNormalized` (`string|null`)
- `AuctionStatusNormalized` (`string|null`)
- `CurrentPrice` (`decimal|null`)
- `FinalPrice` (`decimal|null`)
- `CurrencyCode` (`string|null`, ISO 4217 where applicable)
- `AuctionStartAt` (`string|null`, ISO 8601 UTC)
- `AuctionEndAt` (`string|null`, ISO 8601 UTC)
- `AuctionExtendedEndAt` (`string|null`, ISO 8601 UTC)
- `IsExtended` (`bool|null`)
- `LotUrl` (`string|null`, absolute URL)
- `AttributesJson` (`object|null`, расширяемые атрибуты)
- `RawPayload` (`object|string|null`, сырой payload источника)

### 2.4 Базовые валидации
- `MessageId` должен быть корректным идентификатором и уникальным в хранилище observation.
- `ObservedAt` не может быть пустым, некорректным и слишком будущим (допуск clock skew: до 5 минут).
- `DomainNameOriginal` должен проходить базовую проверку доменного формата (включая IDN).
- Если передан `FinalPrice`, то `CurrencyCode` обязателен.
- Если переданы `AuctionStartAt`/`AuctionEndAt`/`AuctionExtendedEndAt`, они должны быть валидными UTC timestamp.
- `AttributesJson` и `RawPayload` не должны нарушать максимальный лимит сообщения (лимит определяется transport-конфигурацией).

### 2.5 Примеры payload

Пример valid:

```json
{
  "MessageId": "01JQ4S8GQ8TK81Y3TW8G5EZ3M4",
  "SchemaVersion": "1.0",
  "ParserCode": "nicksell",
  "ParserVersion": "1.3.0",
  "SourcePlatformCode": "nicksell",
  "ObservedAt": "2026-04-17T10:15:30.000Z",
  "ExternalAuctionId": "lot-184822",
  "DomainNameOriginal": "марка.рф",
  "AuctionStatusRaw": "active",
  "CurrentPrice": 12500.00,
  "CurrencyCode": "RUB",
  "LotUrl": "https://example.test/lot/184822",
  "AttributesJson": {
    "bidsCount": 7
  }
}
```

Пример invalid (причины: пустой `MessageId`, неверный `ObservedAt`):

```json
{
  "MessageId": "",
  "SchemaVersion": "1.0",
  "ParserCode": "nicksell",
  "ParserVersion": "1.3.0",
  "SourcePlatformCode": "nicksell",
  "ObservedAt": "17-04-2026 10:15",
  "ExternalAuctionId": "lot-184822",
  "DomainNameOriginal": "example.com",
  "AuctionStatusRaw": "active"
}
```

## 3. SchemaVersion policy

- v1 принимает версии: `1`, `1.0`, `1.0.x`.
- Минор/патч обратно совместимы: новые optional поля допускаются без ломки consumer.
- Мажорная смена (`2.x`) не обрабатывается текущим v1 pipeline: сообщение отклоняется как `SCHEMA_VERSION_UNSUPPORTED`.
- Для поддержки новой major-версии требуется новая спецификация и отдельный compatibility review.

## 4. Идемпотентность и dedupe

### 4.1 Dedupe по MessageId (жесткий)
- Первичный ключ идемпотентности: `MessageId`.
- Повтор сообщения с тем же `MessageId` и тем же payload -> результат `duplicate`, side effects не повторяются.
- Повтор сообщения с тем же `MessageId`, но иным payload -> `DUPLICATE_MESSAGE_ID_CONFLICT`, сообщение отклоняется в error-flow.

### 4.2 Semantic dedupe по fingerprint (мягкий)
- Fingerprint вычисляется из нормализованного набора полей:
  - `SourcePlatformCode`
  - `ExternalAuctionId`
  - `DomainNameNormalized` (или результат нормализации из `DomainNameOriginal`)
  - `AuctionStatusNormalized` (или нормализованный from raw)
  - `CurrentPrice`
  - `FinalPrice`
  - `CurrencyCode`
  - `ObservedAt` (округление до 1 секунды)
- Окно semantic dedupe: `5 минут` от `ObservedAt`.
- Если в окне найден идентичный fingerprint с другим `MessageId`, сообщение маркируется как `semantic-duplicate` и не изменяет snapshot, но может логироваться как входящее событие.

### 4.3 Out-of-order и stale policy
- Базовый порядок определяется по `ObservedAt`.
- Сообщение считается `stale`, если `ObservedAt` меньше `LastObservedAt` в auction snapshot и не несет более сильного terminal-сигнала.
- `stale` сообщение:
  - не должно откатывать текущий snapshot;
  - может быть сохранено в историю с признаком `IsOutOfOrder=true`;
  - учитывается в диагностике.

## 5. Canonical statuses и terminal precedence

### 5.1 Канонические статусы v1
- `active`
- `ending`
- `extended`
- `sold` (terminal)
- `not_sold` (terminal)
- `cancelled` (terminal)
- `disappeared` (terminal, специальное правило)
- `unknown` (только промежуточный fallback, не terminal)

### 5.2 Terminal precedence (при конфликте сигналов)
1. `sold`
2. `not_sold`
3. `cancelled`
4. `disappeared`

Правило: terminal-статус с более высоким приоритетом не может быть понижен более низким terminal-статусом из более позднего, но менее достоверного сигнала без явного manual override.

### 5.3 Политика `disappeared`
- `disappeared` фиксируется, когда лот перестал быть доступен, но нет явного terminal-сигнала `sold/not_sold/cancelled`.
- Если позже приходит явный terminal (`sold`, `not_sold`, `cancelled`) с корректным `ObservedAt` и валидной связью по auction, он заменяет `disappeared`.
- Если явный terminal не пришел в пределах retention-окна наблюдения, `disappeared` считается финальным.

### 5.4 Правило финализации финальной цены
- `FinalPrice` может быть зафиксирована только при terminal-статусе.
- Для `sold`:
  - если есть `FinalPrice` -> использовать ее;
  - иначе использовать последнюю известную валидную цену (`CurrentPrice`) как `FinalPriceDerived=true`.
- Для `not_sold`/`cancelled`/`disappeared`: `FinalPrice` по умолчанию `0` или `null` по бизнес-настройке домена хранения (в v1: допускается `null`, а `0` только при явном сигнале источника).

## 6. API v1 рамки

### 6.1 Ingestion API (HTTP, опционально)
- `POST /api/v1/ingestion/observations`
- Назначение: альтернативный внутренний канал приема observation, семантически эквивалентен queue-ingestion.
- Ограничение: internal-only (сетевая изоляция/allowlist).

### 6.2 Read API (обязательный функциональный контур v1)
- `GET /api/v1/domains`
- `GET /api/v1/domains/{domainId}`
- `GET /api/v1/auctions`
- `GET /api/v1/auctions/{auctionId}`
- `GET /api/v1/auctions/{auctionId}/observations`
- `GET /api/v1/results/final`

## 7. Единый формат ошибок (спецификация)

### 7.1 Error envelope
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "ObservedAt must be a valid UTC ISO 8601 timestamp.",
    "details": [
      {
        "field": "ObservedAt",
        "reason": "invalid_format"
      }
    ],
    "traceId": "00-2f8f8f3d9b6c41b9bbf57f73df09f740-0b3b6f9db45d4d65-01",
    "messageId": "01JQ4S8GQ8TK81Y3TW8G5EZ3M4"
  }
}
```

### 7.2 Ingestion error codes
- `VALIDATION_ERROR` -> schema/field validation failed.
- `SCHEMA_VERSION_UNSUPPORTED` -> unsupported major schema version.
- `DUPLICATE_MESSAGE_ID` -> exact duplicate processed ранее.
- `DUPLICATE_MESSAGE_ID_CONFLICT` -> same `MessageId`, different content.
- `SEMANTIC_DUPLICATE` -> duplicate by fingerprint in dedupe window.
- `STALE_MESSAGE` -> out-of-order message does not affect current snapshot.
- `PROCESSING_ERROR` -> internal processing failure.

### 7.3 Read API error codes
- `NOT_FOUND` -> resource not found.
- `BAD_REQUEST` -> invalid query/filter/pagination.
- `CONFLICT` -> state conflict (rare for read flow, mostly technical).
- `INTERNAL_ERROR` -> unhandled server error.

### 7.4 HTTP status mapping
- `400` -> `VALIDATION_ERROR`, `BAD_REQUEST`
- `404` -> `NOT_FOUND`
- `409` -> `DUPLICATE_MESSAGE_ID_CONFLICT`, `CONFLICT`
- `422` -> `SCHEMA_VERSION_UNSUPPORTED`, domain validation failures
- `500` -> `PROCESSING_ERROR`, `INTERNAL_ERROR`
- `202` -> accepted ingestion message (async processing)
- `200` -> read success

## 8. Decision log для Epic 1

- Контракт observation v1 зафиксирован и пригоден к реализации.
- Граница parser/receiver зафиксирована: parser вне текущего репозитория receiver.
- Идемпотентность определяется комбинацией hard dedupe (`MessageId`) и soft dedupe (`fingerprint`).
- Out-of-order политика фиксирует stale-сообщения без отката актуального состояния.
- Финализация статусов и precedence определены, включая политику `disappeared`.
- Единый формат ошибок задан для ingestion/read API.
