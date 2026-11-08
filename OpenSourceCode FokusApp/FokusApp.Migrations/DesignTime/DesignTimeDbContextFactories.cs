using FokusApp.Core.DataBase;
using FokusApp.Core.DataBase.ErrorLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FokusApp.Migrations.DesignTime
{
    public class FokusAppDbContextFactory : IDesignTimeDbContextFactory<FokusAppDbContext>
    {
        public FokusAppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<FokusAppDbContext>();
            builder.UseSqlite(
                "Data Source=design_fokusapp.db",
                sql => sql.MigrationsAssembly(typeof(FokusAppDbContextFactory).Assembly.FullName));

            return new FokusAppDbContext(builder.Options);
        }
    }

    public class AppErrorDbContextFactory : IDesignTimeDbContextFactory<AppErrorDbContext>
    {
        public AppErrorDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppErrorDbContext>();
            builder.UseSqlite(
                "Data Source=design_fokusapp_errors.db",
                sql => sql.MigrationsAssembly(typeof(AppErrorDbContextFactory).Assembly.FullName));

            return new AppErrorDbContext(builder.Options);
        }
    }
}
