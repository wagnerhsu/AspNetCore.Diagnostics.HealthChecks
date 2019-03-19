using HealthChecks.UI.Configuration;
using HealthChecks.UI.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IHealthChecksUIBuilder
    {
        IServiceCollection Services { get; }
    }
    public class HealthChecksUIBuilder
        : IHealthChecksUIBuilder
    {
        public IServiceCollection Services { get; }

        public HealthChecksUIBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
    }
    public static class HealthChecksUIBuilderExtensions
    {
        public static IHealthChecksUIBuilder ConfigureStore(this IHealthChecksUIBuilder builder, Action<DbContextOptionsBuilder> setup, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var provider = builder.Services
                .BuildServiceProvider();

            var healthCheckSettings = provider
                      .GetService<IOptions<Settings>>()
                      .Value ?? new Settings();

            builder
                .Services
                .AddDbContext<HealthChecksDb>(optionsAction: setup, optionsLifetime: lifetime);

            CreateDatabase(builder.Services, healthCheckSettings);

            return builder;
        }
        public static IHealthChecksUIBuilder UseDefaultStore(this IHealthChecksUIBuilder builder)
        {
            const string DEFAULT_DATABASE_NAME = "healthchecksdb";

            var provider = builder.Services
                .BuildServiceProvider();

            var healthCheckSettings = provider
                       .GetService<IOptions<Settings>>()
                       .Value ?? new Settings();

            var configuration = provider
                .GetService<IConfiguration>();

            return ConfigureStore(builder, db =>
            {
                var connectionString = healthCheckSettings.HealthCheckDatabaseConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    var contentRoot = configuration[HostDefaults.ContentRootKey];
                    var path = Path.Combine(contentRoot, DEFAULT_DATABASE_NAME);
                    connectionString = $"Data Source={path}";
                }
                else
                {
                    connectionString = Environment.ExpandEnvironmentVariables(connectionString);
                }

                db.UseSqlite(connectionString);
            });
        }
        static void CreateDatabase(IServiceCollection services, Settings settings)
        {
            var scopeFactory = services.BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetService<HealthChecksDb>();

                var migrator = scope.ServiceProvider
                    .GetService<IMigrator>();

                db.Database.EnsureDeleted();
               
                if(migrator != null )
                {
                    db.Database.Migrate();
                }

                var healthCheckConfigurations = settings?
                    .HealthChecks?
                    .Select(s => new HealthCheckConfiguration()
                    {
                        Name = s.Name,
                        Uri = s.Uri
                    });

                if (healthCheckConfigurations != null
                    &&
                    healthCheckConfigurations.Any())
                {
                    db.Configurations
                        .AddRange(healthCheckConfigurations);

                    db.SaveChanges();
                }
            }
        }
    }
}
