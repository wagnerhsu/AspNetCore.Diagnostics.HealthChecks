using HealthChecks.UI.Core;
using HealthChecks.UI.Core.Configuration;
using HealthChecks.UI.Core.Middlewares;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHealthChecksUI(this IApplicationBuilder app, Action<HealthCheckUIOptions> setup = null)
        {
            var options = new HealthCheckUIOptions();
            setup?.Invoke(options);

            return ConfigurePipeline(app, options);
        }
        private static IApplicationBuilder ConfigurePipeline(IApplicationBuilder app, HealthCheckUIOptions options)
        {
            EnsureValidApiOptions(options);

            var embeddedResourcesAssembly = typeof(UIResource).Assembly;

            app.Map(options.ApiPath, appBuilder => appBuilder.UseMiddleware<UIApiEndpointMiddleware>());
            app.Map(options.WebhookPath, appBuilder => appBuilder.UseMiddleware<UIWebHooksApiMiddleware>());

            new UIResourcesMapper(
                new UIEmbeddedResourcesReader(embeddedResourcesAssembly))
                .Map(app, options);

            return app;
        }
        private static void EnsureValidApiOptions(HealthCheckUIOptions options)
        {
            Action<string, string> ensureValidPath = (string path, string argument) =>
             {
                 if (string.IsNullOrEmpty(path) || !path.StartsWith("/"))
                 {
                     throw new ArgumentException("The value for customized path can't be null and need to start with / characater.");
                 }
             };

            ensureValidPath(options.ApiPath, nameof(HealthCheckUIOptions.ApiPath));
            ensureValidPath(options.UIPath, nameof(HealthCheckUIOptions.UIPath));
            ensureValidPath(options.WebhookPath, nameof(HealthCheckUIOptions.WebhookPath));
        }
    }
}
