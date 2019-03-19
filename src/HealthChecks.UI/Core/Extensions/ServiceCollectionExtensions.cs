using HealthChecks.UI;
using HealthChecks.UI.Configuration;
using HealthChecks.UI.Core.Discovery.K8S;
using HealthChecks.UI.Core.Discovery.K8S.Extensions;
using HealthChecks.UI.Core.HostedService;
using HealthChecks.UI.Core.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IHealthChecksUIBuilder AddHealthChecksUI(this IServiceCollection services, Action<Settings> setupSettings = null)
        {
            var provider = services.BuildServiceProvider();

            var configuration = provider
                .GetService<IConfiguration>();

            var kubernetesDiscoverySettings = provider
                .GetService<IOptions<KubernetesDiscoverySettings>>()
                .Value ?? new KubernetesDiscoverySettings();

            services
                .AddOptions()
                .Configure<Settings>(settings =>
                {
                    configuration.Bind(Keys.HEALTHCHECKSUI_SECTION_SETTING_KEY, settings);
                    setupSettings?.Invoke(settings);
                })
                .Configure<KubernetesDiscoverySettings>(settings =>
                {
                    configuration.Bind(Keys.HEALTHCHECKSUI_KUBERNETES_DISCOVERY_SETTING_KEY, settings);
                })
                .AddSingleton<IHostedService, HealthCheckCollectorHostedService>()
                .AddScoped<IHealthCheckFailureNotifier, WebHookFailureNotifier>()
                .AddScoped<IHealthCheckReportCollector, HealthCheckReportCollector>()
                .AddHttpClient(Keys.HEALTH_CHECK_HTTP_CLIENT_NAME);

            if (kubernetesDiscoverySettings.Enabled)
            {
                services.AddSingleton(kubernetesDiscoverySettings)
                    .AddHostedService<KubernetesDiscoveryHostedService>()
                    .AddHttpClient(Keys.K8S_DISCOVERY_HTTP_CLIENT_NAME, (pvd, ci) => ci.ConfigureKubernetesClient(pvd))
                        .ConfigureKubernetesMessageHandler()
                    .Services
                    .AddHttpClient(Keys.K8S_CLUSTER_SERVICE_HTTP_CLIENT_NAME);
            }

            return new HealthChecksUIBuilder(services);
        }
    }
}