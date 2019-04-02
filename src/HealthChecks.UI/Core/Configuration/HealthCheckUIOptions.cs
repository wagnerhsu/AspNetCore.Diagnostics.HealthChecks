namespace HealthChecks.UI.Core.Configuration
{
    public class HealthCheckUIOptions
    {
        public string UIPath { get; set; } = "/healthchecks-ui";
        public string ApiPath { get; set; } = "/healthchecks-api";
        public string WebhookPath { get; set; } = "/healthchecks-webhooks";
        public string ResourcesPath { get; set; } = "/ui/resources";
    }
}
