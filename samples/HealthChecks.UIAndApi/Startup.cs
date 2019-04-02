using HealthChecks.UI.Client;
using HealthChecks.UI.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthChecks.UIAndApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //add health checks 
            services
                .AddHealthChecks()
                .AddCheck<RandomHealthCheck>("random")
                .AddUrlGroup(new Uri("http://httpbin.org/status/200"));

            //get the healthcheckui-options (ie, the healthz endpoints to include, the webhooks notifications etc.), you can create it also as code!
            var settings = Configuration
                .GetValue<HealthCheckSettings>("HealthChecks-UI");

            //add healthchecks ui 
            services
                .AddHealthChecksUI(settings)
                .UseDefaultStore(); // this use SqlLite with healtchecksdb as a default

            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //
            //   below show howto use default policy handlers ( polly )
            //   with httpclient on asp.net core also
            //   on uri health checks 
            //

            //var retryPolicy = HttpPolicyExtensions
            //    .HandleTransientHttpError()
            //    .Or<TimeoutRejectedException>()
            //    .RetryAsync(5);

            //services.AddHttpClient("uri-group") //default healthcheck registration name for uri ( you can change it on AddUrlGroup )
            //    .AddPolicyHandler(retryPolicy);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //add healthz endpoint and ui endopoint

            app.UseHealthChecks("/healthz", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            })
            .UseHealthChecksUI()
            .UseMvc();
        }
    }

    public class RandomHealthCheck
        : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (DateTime.UtcNow.Minute % 2 == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(description: "failed"));
        }
    }
}
