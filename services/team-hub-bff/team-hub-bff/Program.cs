using DotNetEnv;
using TeamHub.Observability;
using team_hub_bff.Configuration;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddTeamHubSerilog();

builder.Services.AddTeamHubOpenTelemetry(builder.Configuration, "team-hub-bff");
builder.Services.AddBffServices(builder.Configuration);

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGraphQL("/api/graphql");
app.MapTeamHubObservabilityEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
