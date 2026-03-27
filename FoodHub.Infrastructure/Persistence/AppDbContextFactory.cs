using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FoodHub.Application.Interfaces;

namespace FoodHub.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var dbUser = Environment.GetEnvironmentVariable("DB_USER");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            if (string.IsNullOrEmpty(dbHost) || string.IsNullOrEmpty(dbPassword))
            {
                // Better .env discovery: Try current dir, then parents
                var current = Directory.GetCurrentDirectory();
                while (current != null)
                {
                    var potentialEnv = Path.Combine(current, ".env");
                    if (File.Exists(potentialEnv))
                    {
                        DotNetEnv.Env.Load(potentialEnv);
                        break;
                    }
                    // Try looking inside FoodHub_BE subdirectory if not found in current (root search)
                    var subDirEnv = Path.Combine(current, "FoodHub_BE", ".env");
                    if (File.Exists(subDirEnv))
                    {
                        DotNetEnv.Env.Load(subDirEnv);
                        break;
                    }

                    current = Directory.GetParent(current)?.FullName;
                }

                // Re-evaluate after loading
                dbHost = Environment.GetEnvironmentVariable("DB_HOST");
                dbPort = Environment.GetEnvironmentVariable("DB_PORT");
                dbName = Environment.GetEnvironmentVariable("DB_NAME");
                dbUser = Environment.GetEnvironmentVariable("DB_USER");
                dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
            }

            // Apply defaults if still null
            dbHost ??= "localhost";
            dbPort ??= "5432";
            dbName ??= "FoodHubDb";
            dbUser ??= "postgres";
            dbPassword ??= "123456"; // Most common default in this project

            var connectionString =
                $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

            if (dbHost == "localhost" && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_HOST")))
            {
                Console.WriteLine(
                    $"Warning: .env file not found or incomplete. Using defaults: Host={dbHost}"
                );
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

            return new AppDbContext(optionsBuilder.Options, new DesignTimeAuditLogService());
        }

        private class DesignTimeAuditLogService : IAuditLogService
        {
            public string GetActorInfo() => "{\"type\":\"System\",\"info\":\"DesignTime\"}";

            public Task LogActivityAsync(
                FoodHub.Domain.Enums.AuditAction action,
                string entityName,
                string? entityId = null,
                object? oldValues = null,
                object? newValues = null
            ) => Task.CompletedTask;
        }
    }
}
