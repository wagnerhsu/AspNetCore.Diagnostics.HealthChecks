using HealthChecks.UI.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthChecks.UI.Sample
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
            //get the healthcheckui-options (ie, the healthz endpoints to include, the webhooks notifications etc.), you can create it also as code!
            var settings = Configuration
                .GetValue<HealthCheckSettings>("HealthChecks-UI");

            services.AddHealthChecksUI(settings);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHealthChecksUI();
        }
    }
}
