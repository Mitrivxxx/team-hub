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

var redis = builder.AddRedis("cache-redis");
// Stable host ports so service .env / docs (9092, 10000) stay valid beside Aspire injection.
var kafka = builder.AddKafka("msg-kafka", port: 9092);

if (seedMode)
{
    Console.WriteLine("Team Hub Aspire seed mode: auth --seed → srv-auth (gRPC) → organization --seed");
    Console.WriteLine("When org-seed shows Finished, demo data is ready (login JanWilk123 / janwilk123). Stop AppHost with Ctrl+C.");

    var authSeed = builder.AddProject<Projects.team_hub_auth>("auth-seed", launchProfileName: null)
        .WithArgs("--seed")
        .WithReference(authDatabase, "DefaultConnection")
        .WithEnvironment("Redis__ConnectionString", redis)
        .WithEnvironment("Jwt__Key", jwtKey)
        .WithEnvironment("Jwt__Issuer", jwtIssuer)
        .WithEnvironment("Jwt__Audience", jwtAudience)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("Seed__Enabled", "true")
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
        .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
        .WaitFor(authDatabase)
        .WaitFor(redis);

    var authForSeed = builder.AddProject<Projects.team_hub_auth>("srv-auth")
        .WithReference(authDatabase, "DefaultConnection")
        .WithEnvironment("Redis__ConnectionString", redis)
        .WithEnvironment("Jwt__Key", jwtKey)
        .WithEnvironment("Jwt__Issuer", jwtIssuer)
        .WithEnvironment("Jwt__Audience", jwtAudience)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
        .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
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
        .WaitForCompletion(authSeed);

    builder.AddProject<Projects.team_hub_organization>("org-seed", launchProfileName: null)
        .WithArgs("--seed")
        .WithReference(organizationDatabase, "DefaultConnection")
        .WithReference(authForSeed)
        .WithEnvironment("Jwt__Key", jwtKey)
        .WithEnvironment("Jwt__Issuer", jwtIssuer)
        .WithEnvironment("Jwt__Audience", jwtAudience)
        .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
        .WithEnvironment("Kafka__BootstrapServers", kafka)
        .WithEnvironment("Kafka__ClientId", "srv-organization-seed")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("Seed__Enabled", "true")
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
        .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
        .WaitFor(organizationDatabase)
        .WaitFor(kafka)
        .WaitFor(authForSeed)
        .WaitForCompletion(authSeed);

    builder.Build().Run();
    return;
}

var notificationDatabase = postgres.AddDatabase("notification-db", "notification_db");

var blobs = builder.AddAzureStorage("blob-storage")
    .RunAsEmulator(emulator => emulator.WithBlobPort(10000))
    .AddBlobs("blobs");

var auth = builder.AddProject<Projects.team_hub_auth>("srv-auth")
    .WithReference(authDatabase, "DefaultConnection")
    .WithEnvironment("Redis__ConnectionString", redis)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
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
    .WaitFor(redis);

var organization = builder.AddProject<Projects.team_hub_organization>("srv-organization")
    .WithReference(organizationDatabase, "DefaultConnection")
    .WithReference(auth)
    .WithReference(blobs)
    .WithEnvironment("BlobStorage__ConnectionString", blobs)
    .WithEnvironment("BlobStorage__ContainerName", "avatars")
    .WithEnvironment("BlobStorage__PublicBlobEndpoint", "http://127.0.0.1:10000/devstoreaccount1")
    .WithEnvironment("BlobStorage__SasExpiryMinutes", "60")
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
    .WithEnvironment("Kafka__BootstrapServers", kafka)
    .WithEnvironment("Kafka__ClientId", "srv-organization")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
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
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Kafka__BootstrapServers", kafka)
    .WithEnvironment("Kafka__ClientId", "srv-notification")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
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
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
    .WithEnvironment("Grpc__Organization", "http://127.0.0.1:5102")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
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
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("Observability__OtlpEndpoint", otlpEndpoint)
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
