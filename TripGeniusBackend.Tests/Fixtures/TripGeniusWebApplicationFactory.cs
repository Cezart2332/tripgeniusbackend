using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using TripGeniusBackend.Infrastructure.Persistence;

namespace TripGeniusBackend.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing with self-healing, hybrid (PostgreSQL/In-Memory) database configuration
/// </summary>
public class TripGeniusWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dbConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                         ?? "Host=localhost;Port=5432;Database=tripgenius_test;Username=postgres;Password=password";

            var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") 
                         ?? "superSecretKeyForTestingPurposesOnly1234567890";

            var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") 
                            ?? "TripGenius";

            var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience") 
                              ?? "TripGenius";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", dbConn },
                { "Jwt:Key", jwtKey },
                { "Jwt:Issuer", jwtIssuer },
                { "Jwt:Audience", jwtAudience }
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                         ?? "Host=localhost;Port=5432;Database=tripgenius_test;Username=postgres;Password=password";

            bool isDbReachable = false;
            try
            {
                var connStringBuilder = new NpgsqlConnectionStringBuilder(dbConn)
                {
                    Timeout = 2 // 2 seconds timeout for fast testing check
                };

                using (var conn = new NpgsqlConnection(connStringBuilder.ConnectionString))
                {
                    conn.Open();
                    isDbReachable = true;
                }
            }
            catch
            {
                isDbReachable = false;
            }

            if (!isDbReachable)
            {
                // If PostgreSQL is unreachable, replace context with in-memory database to allow offline/local development tests to pass
                var toRemove = services.Where(d => 
                    d.ServiceType.FullName != null && (
                        d.ServiceType.FullName.Contains("Npgsql") || 
                        d.ServiceType.FullName.Contains("EntityFrameworkCore") ||
                        d.ServiceType.Name.Contains("DbContext")
                    )).ToList();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            }

            // Build the service provider and create the database
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();

                // Ensure the database is created
                db.Database.EnsureCreated();
            }
        });
    }
}
