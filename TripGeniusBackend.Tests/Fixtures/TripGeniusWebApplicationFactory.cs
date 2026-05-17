using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using TripGeniusBackend.Infrastructure.Persistence;

namespace TripGeniusBackend.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing with in-memory database
/// </summary>
public class TripGeniusWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "super-secret-key-development-2026" },
                { "Jwt:Issuer", "tripgenius" },
                { "Jwt:Audience", "tripgenius" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the production DbContext and all relational/Npgsql services to avoid provider collision
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

            // Add in-memory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

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
