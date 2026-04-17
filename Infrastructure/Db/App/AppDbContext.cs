using Microsoft.EntityFrameworkCore;
using Infrastructure.Db.App.Entities;

namespace Infrastructure.Db.App;

public partial class AppDbContext : DbContext
{
    private const string appSchema = "app";

    protected string? InitiatorUserId { get; set; }

    public AppDbContext() { }
    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //  //Определение провайдера необходимо для создания миграции, поэтому пусть пока побудет здесь.
    //  string mockString = "Host=127.0.0.1;Port=5432;Database=life_time_bot_db;Username=postgres;Password=123;Include Error Detail=true";
    //  optionsBuilder.UseNpgsql(mockString);
    //  base.OnConfiguring(optionsBuilder);
    // }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IServiceProvider serviceProvider) : base(options)
    {
    }

    // Коллекции данных
    public DbSet<StatEventEntity> StatEvents => Set<StatEventEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        SetSchemasToTables(builder);
        SetAllToSnakeCase(builder);
        AppDbContext.SetFilters(builder);
        AppDbContext.ConfigureEntities(builder);
        base.OnModelCreating(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in
                 ChangeTracker.Entries<IBaseEntity>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    e.Entity.CreatedBy = InitiatorUserId;
                    break;
                case EntityState.Modified:
                    e.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    e.Entity.UpdatedBy = InitiatorUserId;
                    break;
                case EntityState.Deleted:
                    e.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    e.Entity.DeletedBy = InitiatorUserId;
                    e.State = EntityState.Modified;
                    break;
            }
        }

        return base.SaveChangesAsync(ct);
    }

}
