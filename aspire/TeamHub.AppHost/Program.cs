using TeamHub.AppHost;

var seedMode = args.Contains("--seed", StringComparer.Ordinal);
var appHostArgs = args.Where(a => !string.Equals(a, "--seed", StringComparison.Ordinal)).ToArray();

var builder = DistributedApplication.CreateBuilder(appHostArgs);

var jwtKey = builder.Configuration["Aspire:Jwt:Key"]
    ?? throw new InvalidOperationException("Aspire:Jwt:Key is required in AppHost configuration.");
var jwtIssuer = builder.Configuration["Aspire:Jwt:Issuer"] ?? "AuthService";
var jwtAudience = builder.Configuration["Aspire:Jwt:Audience"] ?? "AuthServiceUsers";

var infra = DevInfra.Load(builder.Configuration);
DevInfra.EnsureReachable(infra, requireBlobStorage: !seedMode);

const string otlpEndpoint = "http://127.0.0.1:4317";

if (seedMode)
{
    Console.WriteLine("Team Hub Aspire seed mode: seed-auth → seed-auth-api → seed-organization → seed-notification");
    Console.WriteLine("Infra: compose-dev Postgres/Redis/Kafka. When seed-notification finishes, AppHost exits.");
    Console.WriteLine("Login: JanWilk123 / janwilk123");

    var seedAuth = builder.AddProject<Projects.team_hub_auth>("seed-auth", launchProfileName: null)
        .WithArgs("--seed")
        .WithTeamHubDevPostgres(infra, "auth_db")
        .WithTeamHubDevRedis(infra)
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithTeamHubDevSeed()
        .WithTeamHubOtlp(otlpEndpoint);

    // Temporary gRPC host for org/notification seed. Use dedicated ports (not 5001/5101) so a
    // running full stack does not collide; override appsettings Kestrel URLs explicitly.
    const int seedAuthHttpPort = 15001;
    const int seedAuthGrpcPort = 15101;
    var seedAuthHttpUrl = "http://127.0.0.1:" + seedAuthHttpPort;
    var seedAuthGrpcUrl = "http://127.0.0.1:" + seedAuthGrpcPort;

    var seedAuthApi = builder.AddProject<Projects.team_hub_auth>("seed-auth-api", launchProfileName: null)
        .WithTeamHubDevPostgres(infra, "auth_db")
        .WithTeamHubDevRedis(infra)
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithTeamHubOtlp(otlpEndpoint)
        .WithEnvironment("Kestrel__Endpoints__Http__Url", seedAuthHttpUrl)
        .WithEnvironment("Kestrel__Endpoints__Grpc__Url", seedAuthGrpcUrl)
        .WithEndpoint("http", endpoint =>
        {
            endpoint.Port = seedAuthHttpPort;
            endpoint.UriScheme = "http";
            endpoint.IsProxied = false;
        })
        .WithEndpoint("grpc", endpoint =>
        {
            endpoint.Port = seedAuthGrpcPort;
            endpoint.UriScheme = "http";
            endpoint.IsProxied = false;
        })
        .WaitForCompletion(seedAuth);

    var seedOrganization = builder.AddProject<Projects.team_hub_organization>("seed-organization", launchProfileName: null)
        .WithArgs("--seed")
        .WithTeamHubDevPostgres(infra, "organization_db")
        .WithReference(seedAuthApi)
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithEnvironment("Grpc__Auth", seedAuthGrpcUrl)
        .WithTeamHubDevKafka(infra, "seed-organization")
        .WithTeamHubDevSeed(organizationCount: 1)
        .WithTeamHubOtlp(otlpEndpoint)
        .WaitFor(seedAuthApi)
        .WaitForCompletion(seedAuth);

    builder.AddProject<Projects.team_hub_notification>("seed-notification", launchProfileName: null)
        .WithArgs("--seed")
        .WithTeamHubDevPostgres(infra, "notification_db")
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithEnvironment("Grpc__Auth", seedAuthGrpcUrl)
        .WithTeamHubDevKafka(infra, "seed-notification")
        .WithTeamHubDevSeed()
        .WithTeamHubOtlp(otlpEndpoint)
        .WaitFor(seedAuthApi)
        .WaitForCompletion(seedOrganization);

    var app = builder.Build();
    await app.StartAsync();
    await app.ResourceNotifications.WaitForResourceAsync(
        "seed-notification",
        KnownResourceStates.Finished);
    Console.WriteLine("Demo seed complete. Data persisted in compose-dev Postgres volume. Exiting AppHost.");
    await app.StopAsync();
    return;
}

var auth = builder.AddProject<Projects.team_hub_auth>("srv-auth")
    .WithTeamHubDevPostgres(infra, "auth_db")
    .WithTeamHubDevRedis(infra)
    .WithTeamHubDevBlobStorage(infra)
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5001;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    })
    .WithEndpoint("grpc", endpoint =>
    {
        endpoint.Port = 5101;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    });

var organization = builder.AddProject<Projects.team_hub_organization>("srv-organization")
    .WithTeamHubDevPostgres(infra, "organization_db")
    .WithReference(auth)
    .WithTeamHubDevBlobStorage(infra)
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
    .WithTeamHubDevKafka(infra, "srv-organization")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5002;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    })
    .WithEndpoint("grpc", endpoint =>
    {
        endpoint.Port = 5102;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    });

var notification = builder.AddProject<Projects.team_hub_notification>("srv-notification")
    .WithTeamHubDevPostgres(infra, "notification_db")
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithTeamHubDevKafka(infra, "srv-notification")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5004;
        endpoint.IsProxied = false;
    });

var chat = builder.AddProject<Projects.team_hub_chat>("srv-chat")
    .WithTeamHubDevPostgres(infra, "chat_db")
    .WithTeamHubDevBlobStorage(infra)
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Grpc__Organization", "http://127.0.0.1:5102")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5005;
        endpoint.IsProxied = false;
    });

var bff = builder.AddProject<Projects.team_hub_bff>("srv-bff")
    .WithReference(auth)
    .WithReference(organization)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
    .WithEnvironment("Grpc__Organization", "http://127.0.0.1:5102")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5003;
        endpoint.IsProxied = false;
    });

// launchSettings already registers endpoint "http" on :5000 — configure it, do not add a second one.
builder.AddProject<Projects.team_hub_gateway>("gw-api")
    .WithReference(auth)
    .WithReference(organization)
    .WithReference(notification)
    .WithReference(chat)
    .WithReference(bff)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEnvironment("ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address", "http://srv-auth")
    .WithEnvironment("ReverseProxy__Clusters__team-cluster__Destinations__team__Address", "http://srv-organization")
    .WithEnvironment("ReverseProxy__Clusters__notification-cluster__Destinations__notification__Address", "http://srv-notification")
    .WithEnvironment("ReverseProxy__Clusters__chat-cluster__Destinations__chat__Address", "http://srv-chat")
    .WithEnvironment("ReverseProxy__Clusters__bff-cluster__Destinations__bff__Address", "http://srv-bff")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5000;
        endpoint.IsProxied = false;
    });

builder.AddDockerfile("gw-nginx", "../..", "infrastructure/nginx/Dockerfile")
    .WithHttpEndpoint(port: 8080, targetPort: 443, name: "https", isProxied: false)
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "stub-status", isProxied: false)
    .WithBindMount("../../certs", "/etc/nginx/certs")
    .WithBindMount("../../infrastructure/monitoring/.nginx-edge-logs", "/var/log/nginx-edge")
    .WithEnvironment("GATEWAY_UPSTREAM", "host.docker.internal:5000")
    .WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway");

builder.AddExecutable("ui-web", "npm", "../../frontend/team-hub-web", "start")
    .WithHttpsEndpoint(port: 4200, targetPort: 4200, name: "frontend", isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();

internal static class TeamHubAppHostExtensions
{
    public static IResourceBuilder<T> WithTeamHubJwt<T>(
        this IResourceBuilder<T> builder,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience)
        where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Jwt__Key", jwtKey)
            .WithEnvironment("Jwt__Issuer", jwtIssuer)
            .WithEnvironment("Jwt__Audience", jwtAudience);

    public static IResourceBuilder<T> WithTeamHubOtlp<T>(
        this IResourceBuilder<T> builder,
        string otlpEndpoint)
        where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
            .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint);

    public static IResourceBuilder<T> WithTeamHubDevSeed<T>(
        this IResourceBuilder<T> builder,
        int? organizationCount = null)
        where T : IResourceWithEnvironment
    {
        builder = builder
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Seed__Enabled", "true");

        if (organizationCount is int count)
            builder = builder.WithEnvironment("Seed__OrganizationCount", count.ToString());

        return builder;
    }
}
