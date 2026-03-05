using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
                var entryDir =
                    Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location)
                    ?? Directory.GetCurrentDirectory();
                var searchDirs = new[]
                {
                    Directory.GetCurrentDirectory(),
                    entryDir,
                    AppContext.BaseDirectory,
                };

                foreach (var sDir in searchDirs)
                {
                    var current = sDir;
                    while (current != null)
                    {
                        var potentialEnv = Path.Combine(current, ".env");
                        if (File.Exists(potentialEnv))
                        {
                            DotNetEnv.Env.Load(potentialEnv);
                        }
                        else
                        {
                            // Try looking inside FoodHub_BE subdirectory if not found in current
                            var subDirEnv = Path.Combine(current, "FoodHub_BE", ".env");
                            if (File.Exists(subDirEnv))
                            {
                                DotNetEnv.Env.Load(subDirEnv);
                            }
                        }

                        dbHost = Environment.GetEnvironmentVariable("DB_HOST");
                        dbPort = Environment.GetEnvironmentVariable("DB_PORT");
                        dbName = Environment.GetEnvironmentVariable("DB_NAME");
                        dbUser = Environment.GetEnvironmentVariable("DB_USER");
                        dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

                        if (!string.IsNullOrEmpty(dbHost))
                            break;

                        current = Directory.GetParent(current)?.FullName;
                    }
                    if (!string.IsNullOrEmpty(dbHost))
                        break;
                }
            }

            var connectionString =
                $"Host={dbHost};Port={dbPort ?? "5432"};Database={dbName ?? "FoodHub"};Username={dbUser ?? "postgres"};Password={dbPassword}";

            if (string.IsNullOrEmpty(dbHost) || string.IsNullOrEmpty(dbPassword))
            {
                Console.WriteLine(
                    $"Warning: .env file not found or incomplete variables. Connection attempt may fail. Host={dbHost ?? "null"}"
                );
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
