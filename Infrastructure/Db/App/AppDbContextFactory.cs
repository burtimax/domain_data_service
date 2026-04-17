// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Design;
//
// namespace Infrastructure.Db.App;
//
// public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
// {
//     public AppDbContext CreateDbContext(string[] args)
//     {
//         var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
//         //optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5432;Database=app_db;Username=postgres;Password=123;Include Error Detail=true");
//
//         return new AppDbContext(optionsBuilder.Options);
//     }
//
// }
