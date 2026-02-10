using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace FoodHub.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            
            if (!File.Exists(envPath))
            {
               var currentBreak = Directory.GetCurrentDirectory();
               
               while (!File.Exists(Path.Combine(currentBreak, ".env")) && Directory.GetParent(currentBreak) != null)
               {
                   currentBreak = Directory.GetParent(currentBreak).FullName;
               }
               envPath = Path.Combine(currentBreak, ".env");
            }

            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var dbUser = Environment.GetEnvironmentVariable("DB_USER");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var connectionString = $"Host={dbHost};Port={dbPort ?? "5432"};Database={dbName ?? "FoodHub"};Username={dbUser ?? "postgres"};Password={dbPassword}";

            if (string.IsNullOrEmpty(dbHost) || string.IsNullOrEmpty(dbPassword))
            {
                Console.WriteLine($"Warning: .env file not found or incomplete variables at {envPath}");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString)
                          .UseSnakeCaseNamingConvention();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
