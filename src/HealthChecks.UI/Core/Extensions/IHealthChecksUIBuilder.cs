using HealthChecks.UI.Core.Configuration;
using HealthChecks.UI.Core.Data;
using Microsoft.EntityFrameworkCore;
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
        public static IHealthChecksUIBuilder UseDefaultStore(this IHealthChecksUIBuilder builder, string databaseName = "healthchecksdb")
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

                setup.UseSqlite(connectionString);
            });

            CreateDatabase(builder.Services, healthCheckSettings);

            return builder;
        }
        static void CreateDatabase(IServiceCollection services, HealthCheckSettings settings)
        {
            var scopeFactory = services.BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetService<HealthChecksDb>();

                db.Database.EnsureDeleted();

                db.Database.Migrate();

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
