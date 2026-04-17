Команды для миграции БД (`AppDbContext`, PostgreSQL).

## Текущее состояние
- Актуальная миграция: `20260417172111_AddReceiverDomainModel`.
- Миграции хранятся в `Infrastructure/Db/App/Migrations`.
- На старте API применяется `db.Database.Migrate()` (см. `Api/Program.cs`).

## Подготовка
Из корня репозитория при необходимости задайте строку подключения для design-time factory:

```powershell
$env:APP_DB_CONNECTION="Host=localhost;Port=5432;Database=domain_parser;Username=postgres;Password=postgres"
```

## Создать миграцию
```powershell
dotnet ef migrations add "<MigrationName>" `
  --context AppDbContext `
  --project "Infrastructure/Infrastructure.csproj" `
  --startup-project "Infrastructure/Infrastructure.csproj" `
  --output-dir "Db/App/Migrations"
```

## Удалить последнюю миграцию
```powershell
dotnet ef migrations remove `
  --context AppDbContext `
  --project "Infrastructure/Infrastructure.csproj" `
  --startup-project "Infrastructure/Infrastructure.csproj"
```

## Применить миграции в БД вручную
```powershell
dotnet ef database update `
  --context AppDbContext `
  --project "Infrastructure/Infrastructure.csproj" `
  --startup-project "Infrastructure/Infrastructure.csproj"
```

## Сгенерировать idempotent SQL-скрипт (без ручных правок)
```powershell
dotnet ef migrations script `
  --context AppDbContext `
  --project "Infrastructure/Infrastructure.csproj" `
  --startup-project "Infrastructure/Infrastructure.csproj" `
  --idempotent
```
