using HealthChecks.UI.Core.Configuration;
using HealthChecks.UI.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        public static IHealthChecksUIBuilder UseDefaultStore(this IHealthChecksUIBuilder builder, string databaseName = "healthchecksdb", bool recreateDatabase = true)
        {
            var provider = builder.Services
                .BuildServiceProvider();

            var healthCheckSettings = provider
                       .GetService<IOptions<HealthCheckSettings>>()
                       .Value ?? new HealthCheckSettings();

            var configuration = provider
                .GetService<IConfiguration>();

            builder.Services.AddDbContext<HealthChecksDb>(setup =>
            {
                var connectionString = healthCheckSettings.HealthCheckDatabaseConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    var contentRoot = configuration[HostDefaults.ContentRootKey];
                    var path = Path.Combine(contentRoot, databaseName);
                    connectionString = $"Data Source={path}";
                }
                else
                {
                    connectionString = Environment.ExpandEnvironmentVariables(connectionString);
                }
            });

            CreateDatabase(builder.Services, healthCheckSettings, recreateDatabase);

            return builder;
        }
        static void CreateDatabase(IServiceCollection services, HealthCheckSettings settings, bool recreateDatabase = true)
        {
            var scopeFactory = services.BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetService<HealthChecksDb>();

                var migrator = db.GetService<IServiceProvider>()
                    .GetService(typeof(IMigrator));

                if (recreateDatabase)
                {
                    db.Database.EnsureDeleted();

                    if (migrator != null)
                    {
                        db.Database.Migrate();
                    }

                    db.Database.EnsureCreated();

                    var healthCheckConfigurations = settings?.HealthChecks?
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
}
