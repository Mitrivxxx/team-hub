using DotNetEnv;
using TeamHub.Observability;
using team_hub_bff.Configuration;

// NoClobber: Aspire/Compose env must win over local .env.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddTeamHubSerilog();

builder.Services.AddTeamHubOpenTelemetry(builder.Configuration, "team-hub-bff");
builder.Services.AddBffServices(builder.Configuration);

var app = builder.Build();

app.UseTeamHubExceptionHandling();
app.UseTeamHubCorrelationId();
app.UseTeamHubSessionId();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseTeamHubUserIdLogging();
app.UseSerilogRequestLoggingExcludingHealth();

app.MapHealthChecks("/health");
app.MapGraphQL("/api/graphql");
app.MapTeamHubObservabilityEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
