using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Domain.Entities;
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

            // Use the exact JWT settings configured in AuthTestFixture to ensure 100% authorization match
            var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") 
                         ?? "super-secret-key-development-2026";

            var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") 
                            ?? "tripgenius";

            var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience") 
                              ?? "tripgenius";

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
            // Replace real IEmailService registration with a NoOpEmailService to prevent Resend API failures in CI
            var emailServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }
            services.AddScoped<IEmailService, NoOpEmailService>();

            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IContentModerationService))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IContentModerationService, NoOpContentModerationService>();

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

                // Seed a default test user with Id = 1 (to satisfy database foreign keys in relational PostgreSQL DB in CI)
                var hasher = scopedServices.GetRequiredService<IPasswordHasher>();
                var testUser = db.Users.FirstOrDefault(u => u.Id == 1);
                if (testUser == null)
                {
                    testUser = User.UserCreate("user1@test.com", hasher.HashPassword("SecurePassword123!"));
                    testUser.VerifyEmail();
                    testUser.UpdateProfile("Test User", "", "");
                    
                    db.Users.Add(testUser);
                    db.SaveChanges();
                }
            }
        });
    }

    private class NoOpEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string content, string actionUrl, string actionLabel)
        {
            return Task.CompletedTask;
        }
    }
}
