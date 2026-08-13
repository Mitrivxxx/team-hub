using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Span;

namespace TeamHub.Observability;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddTeamHubSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

        return hostBuilder;
    }
}
