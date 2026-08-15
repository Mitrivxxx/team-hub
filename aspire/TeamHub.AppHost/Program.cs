var seedMode = args.Contains("--seed", StringComparer.Ordinal);
var appHostArgs = args.Where(a => !string.Equals(a, "--seed", StringComparison.Ordinal)).ToArray();

var builder = DistributedApplication.CreateBuilder(appHostArgs);

var jwtKey = builder.Configuration["Aspire:Jwt:Key"]
    ?? throw new InvalidOperationException("Aspire:Jwt:Key is required in AppHost configuration.");
var jwtIssuer = builder.Configuration["Aspire:Jwt:Issuer"] ?? "AuthService";
var jwtAudience = builder.Configuration["Aspire:Jwt:Audience"] ?? "AuthServiceUsers";

const string otlpEndpoint = "http://127.0.0.1:4317";

var postgres = builder.AddPostgres("db-postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Aspire resource names allow only letters, digits, hyphens — DB name can still be auth_db.
var authDatabase = postgres.AddDatabase("auth-db", "auth_db");
var organizationDatabase = postgres.AddDatabase("organization-db", "organization_db");
var notificationDatabase = postgres.AddDatabase("notification-db", "notification_db");

var redis = builder.AddRedis("cache-redis");
// Stable host ports so service .env / docs (9092, 10000) stay valid beside Aspire injection.
var kafka = builder.AddKafka("msg-kafka", port: 9092);

if (seedMode)
{
    Console.WriteLine("Team Hub Aspire seed mode: seed-auth → seed-auth-api → seed-organization → seed-notification");
    Console.WriteLine("When seed-notification finishes, AppHost exits. Login: JanWilk123 / janwilk123");

    var seedAuth = builder.AddProject<Projects.team_hub_auth>("seed-auth", launchProfileName: null)
        .WithArgs("--seed")
        .WithReference(authDatabase, "DefaultConnection")
        .WithEnvironment("Redis__ConnectionString", redis)
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithTeamHubDevSeed()
        .WithTeamHubOtlp(otlpEndpoint)
        .WaitFor(authDatabase)
        .WaitFor(redis);

    // Temporary gRPC host for org/notification seed. Use dedicated ports (not 5001/5101) so a
    // running full stack does not collide; override appsettings Kestrel URLs explicitly.
    const int seedAuthHttpPort = 15001;
    const int seedAuthGrpcPort = 15101;
    var seedAuthHttpUrl = "http://127.0.0.1:" + seedAuthHttpPort;
    var seedAuthGrpcUrl = "http://127.0.0.1:" + seedAuthGrpcPort;

    var seedAuthApi = builder.AddProject<Projects.team_hub_auth>("seed-auth-api", launchProfileName: null)
        .WithReference(authDatabase, "DefaultConnection")
        .WithEnvironment("Redis__ConnectionString", redis)
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
        .WaitFor(authDatabase)
        .WaitFor(redis)
        .WaitForCompletion(seedAuth);

    var seedOrganization = builder.AddProject<Projects.team_hub_organization>("seed-organization", launchProfileName: null)
        .WithArgs("--seed")
        .WithReference(organizationDatabase, "DefaultConnection")
        .WithReference(seedAuthApi)
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithEnvironment("Grpc__Auth", seedAuthGrpcUrl)
        .WithEnvironment("Kafka__BootstrapServers", kafka)
        .WithEnvironment("Kafka__ClientId", "seed-organization")
        .WithTeamHubDevSeed(organizationCount: 1)
        .WithTeamHubOtlp(otlpEndpoint)
        .WaitFor(organizationDatabase)
        .WaitFor(kafka)
        .WaitFor(seedAuthApi)
        .WaitForCompletion(seedAuth);

    builder.AddProject<Projects.team_hub_notification>("seed-notification", launchProfileName: null)
        .WithArgs("--seed")
        .WithReference(notificationDatabase, "DefaultConnection")
        .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
        .WithEnvironment("Grpc__Auth", seedAuthGrpcUrl)
        .WithEnvironment("Kafka__BootstrapServers", kafka)
        .WithEnvironment("Kafka__ClientId", "seed-notification")
        .WithTeamHubDevSeed()
        .WithTeamHubOtlp(otlpEndpoint)
        .WaitFor(notificationDatabase)
        .WaitFor(seedAuthApi)
        .WaitForCompletion(seedOrganization);

    var app = builder.Build();
    await app.StartAsync();
    await app.ResourceNotifications.WaitForResourceAsync(
        "seed-notification",
        KnownResourceStates.Finished);
    Console.WriteLine("Demo seed complete. Data persisted in Postgres volume. Exiting AppHost.");
    await app.StopAsync();
    return;
}

var blobs = builder.AddAzureStorage("blob-storage")
    .RunAsEmulator(emulator => emulator.WithBlobPort(10000).WithDataVolume())
    .AddBlobs("blobs");

var auth = builder.AddProject<Projects.team_hub_auth>("srv-auth")
    .WithReference(authDatabase, "DefaultConnection")
    .WithReference(blobs)
    .WithEnvironment("Redis__ConnectionString", redis)
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("BlobStorage__ConnectionString", blobs)
    .WithEnvironment("BlobStorage__ContainerName", "avatars")
    .WithEnvironment("BlobStorage__PublicBlobEndpoint", "http://127.0.0.1:10000/devstoreaccount1")
    .WithEnvironment("BlobStorage__SasExpiryMinutes", "60")
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
    })
    .WaitFor(authDatabase)
    .WaitFor(redis)
    .WaitFor(blobs);

var organization = builder.AddProject<Projects.team_hub_organization>("srv-organization")
    .WithReference(organizationDatabase, "DefaultConnection")
    .WithReference(auth)
    .WithReference(blobs)
    .WithEnvironment("BlobStorage__ConnectionString", blobs)
    .WithEnvironment("BlobStorage__ContainerName", "avatars")
    .WithEnvironment("BlobStorage__PublicBlobEndpoint", "http://127.0.0.1:10000/devstoreaccount1")
    .WithEnvironment("BlobStorage__SasExpiryMinutes", "60")
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
    .WithEnvironment("Kafka__BootstrapServers", kafka)
    .WithEnvironment("Kafka__ClientId", "srv-organization")
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
    })
    .WaitFor(organizationDatabase)
    .WaitFor(kafka)
    .WaitFor(blobs);

var notification = builder.AddProject<Projects.team_hub_notification>("srv-notification")
    .WithReference(notificationDatabase, "DefaultConnection")
    .WithTeamHubJwt(jwtKey, jwtIssuer, jwtAudience)
    .WithEnvironment("Kafka__BootstrapServers", kafka)
    .WithEnvironment("Kafka__ClientId", "srv-notification")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5004;
        endpoint.IsProxied = false;
    })
    .WaitFor(notificationDatabase)
    .WaitFor(kafka);

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
    .WithReference(bff)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithTeamHubOtlp(otlpEndpoint)
    .WithEnvironment("ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address", "http://srv-auth")
    .WithEnvironment("ReverseProxy__Clusters__team-cluster__Destinations__team__Address", "http://srv-organization")
    .WithEnvironment("ReverseProxy__Clusters__notification-cluster__Destinations__notification__Address", "http://srv-notification")
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
