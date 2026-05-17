// AppDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TripGeniusBackend.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), 
            "../TripGeniusBackend.API"); 
        DotNetEnv.Env.TraversePath().Load();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.ConnectionStringBuilder.SearchPath = "public";
        var dataSource = dataSourceBuilder.Build();

        optionsBuilder.UseNpgsql(dataSource, o => o.UseVector())
            .ConfigureWarnings(w => 
                w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new AppDbContext(optionsBuilder.Options);
    }
}