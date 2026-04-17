using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Infrastructure.Db.App.Entities;
using Shared.Extensions;

namespace Infrastructure.Db.App;

public partial class AppDbContext
{
    /// <summary>
    /// Таблицы, свойства, ключи, внеш. ключи, индексы переводит в нижний регистр в БД.
    /// </summary>
    protected void SetAllToSnakeCase(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.GetTableName().ToSnakeCase());

            foreach (var property in entityType.GetProperties())
            {
                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();
                var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
                property.SetColumnName(property.GetColumnName(storeObjectIdentifier).ToSnakeCase());
            }

            foreach (var key in entityType.GetKeys())
                key.SetName(key.GetName().ToSnakeCase());

            foreach (var key in entityType.GetForeignKeys())
                key.SetConstraintName(key.GetConstraintName().ToSnakeCase());

            foreach (var index in entityType.GetIndexes())
                index.SetDatabaseName(index.GetDatabaseName().ToSnakeCase());
        }
    }

    /// <summary>
    /// Задать наименование таблиц и схемы для таблиц.
    /// </summary>
    private void SetSchemasToTables(ModelBuilder builder)
    {
        // Определение сущностей по схемам.
        builder.Entity<StatEventEntity>().ToTable("stat_events", Infrastructure.Db.App.AppDbContext.appSchema);
        builder.Entity<DomainEntity>().ToTable("domains", Infrastructure.Db.App.AppDbContext.appSchema);
        builder.Entity<SourcePlatformEntity>().ToTable("source_platforms", Infrastructure.Db.App.AppDbContext.appSchema);
        builder.Entity<AuctionEntity>().ToTable("auctions", Infrastructure.Db.App.AppDbContext.appSchema);
        builder.Entity<AuctionObservationEntity>().ToTable("auction_observations", Infrastructure.Db.App.AppDbContext.appSchema);
        builder.Entity<AuctionStatusHistoryEntity>().ToTable("auction_status_history", Infrastructure.Db.App.AppDbContext.appSchema);
        builder.Entity<ReceiverProcessingLogEntity>().ToTable("receiver_processing_logs", Infrastructure.Db.App.AppDbContext.appSchema);
    }

    /// <summary>
    /// Настройка фильтров запросов.
    /// </summary>
    public static void SetFilters(ModelBuilder modelBuilder)
    {
        var entities = modelBuilder.Model
            .GetEntityTypes()
            .Where(e => e.ClrType.BaseType == typeof(BaseEntity))
            .Select(e => e.ClrType);

        Expression<Func<BaseEntity, bool>>
            expression = del => del.DeletedAt == null;

        foreach (var e in entities)
        {
            ParameterExpression p = Expression.Parameter(e);
            Expression body =
                ReplacingExpressionVisitor
                    .Replace(expression.Parameters.Single(),
                        p, expression.Body);

            modelBuilder.Entity(e)
                .HasQueryFilter(
                    Expression.Lambda(body, p));
        }
    }

    public static void ConfigureEntities(ModelBuilder builder)
    {
        builder.Entity<DomainEntity>(entity =>
        {
            entity.HasIndex(x => x.NameNormalized).IsUnique();
            entity.HasIndex(x => x.NamePunycode);
        });

        builder.Entity<SourcePlatformEntity>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<AuctionEntity>(entity =>
        {
            entity.Property(x => x.CurrentPrice).HasPrecision(18, 4);
            entity.Property(x => x.FinalPrice).HasPrecision(18, 4);

            entity.HasIndex(x => new { x.SourcePlatformId, x.ExternalAuctionId }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.EndAt);
            entity.HasIndex(x => x.LastObservedAt);

            entity.HasOne(x => x.Domain)
                .WithMany(x => x.Auctions)
                .HasForeignKey(x => x.DomainId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SourcePlatform)
                .WithMany(x => x.Auctions)
                .HasForeignKey(x => x.SourcePlatformId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuctionObservationEntity>(entity =>
        {
            entity.Property(x => x.CurrentPrice).HasPrecision(18, 4);
            entity.Property(x => x.FinalPrice).HasPrecision(18, 4);
            entity.Property(x => x.RawPayload).HasColumnType("jsonb");
            entity.Property(x => x.AttributesJson).HasColumnType("jsonb");

            entity.HasIndex(x => x.MessageId).IsUnique();
            entity.HasIndex(x => x.ObservedAt);
            entity.HasIndex(x => new { x.AuctionId, x.ObservedAt });

            entity.HasOne(x => x.Auction)
                .WithMany(x => x.Observations)
                .HasForeignKey(x => x.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuctionStatusHistoryEntity>(entity =>
        {
            entity.HasIndex(x => new { x.AuctionId, x.ObservedAt });
            entity.HasIndex(x => new { x.AuctionId, x.NewStatus });

            entity.HasOne(x => x.Auction)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReceiverProcessingLogEntity>(entity =>
        {
            entity.HasIndex(x => x.MessageId);
            entity.HasIndex(x => x.ProcessedAt);
            entity.HasIndex(x => new { x.SourcePlatformCode, x.ExternalAuctionId });
        });
    }
}
