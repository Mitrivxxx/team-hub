var builder = DistributedApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Aspire:Jwt:Key"]
    ?? throw new InvalidOperationException("Aspire:Jwt:Key is required in AppHost configuration.");
var jwtIssuer = builder.Configuration["Aspire:Jwt:Issuer"] ?? "AuthService";
var jwtAudience = builder.Configuration["Aspire:Jwt:Audience"] ?? "AuthServiceUsers";

var postgres = builder.AddPostgres("db-postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Aspire resource names allow only letters, digits, hyphens — DB name can still be auth_db.
var authDatabase = postgres.AddDatabase("auth-db", "auth_db");

var redis = builder.AddRedis("cache-redis");

var kafka = builder.AddKafka("msg-kafka");

var blobs = builder.AddAzureStorage("blob-storage")
    .RunAsEmulator()
    .AddBlobs("blobs");

var organizationDatabase = postgres.AddDatabase("organization-db", "organization_db");
var notificationDatabase = postgres.AddDatabase("notification-db", "notification_db");

var auth = builder.AddProject<Projects.team_hub_auth>("srv-auth")
    .WithReference(authDatabase, "DefaultConnection")
    .WithEnvironment("Redis__ConnectionString", redis)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEndpoint("grpc", endpoint =>
    {
        endpoint.Port = 5101;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    });

var organization = builder.AddProject<Projects.team_hub_organization>("srv-organization")
    .WithReference(organizationDatabase, "DefaultConnection")
    .WithReference(auth)
    .WithReference(blobs)
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
    .WithEndpoint("grpc", endpoint =>
    {
        endpoint.Port = 5102;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    });

var notification = builder.AddProject<Projects.team_hub_notification>("srv-notification")
    .WithReference(notificationDatabase, "DefaultConnection")
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Kafka__BootstrapServers", kafka)
    .WithEnvironment("Kafka__ClientId", "srv-notification")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5004;
        endpoint.IsProxied = false;
    });

var bff = builder.AddProject<Projects.team_hub_bff>("srv-bff")
    .WithReference(auth)
    .WithReference(organization)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Grpc__Auth", "http://127.0.0.1:5101")
    .WithEnvironment("Grpc__Organization", "http://127.0.0.1:5102")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5003;
        endpoint.IsProxied = false;
    });

// launchSettings already registers endpoint "http" on :5000 — configure it, do not add a second one.
var gateway = builder.AddProject<Projects.team_hub_gateway>("gw-api")
    .WithReference(auth)
    .WithReference(organization)
    .WithReference(notification)
    .WithReference(bff)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address", "http://srv-auth")
    .WithEnvironment("ReverseProxy__Clusters__team-cluster__Destinations__team__Address", "http://srv-organization")
    .WithEnvironment("ReverseProxy__Clusters__notification-cluster__Destinations__notification__Address", "http://srv-notification")
    .WithEnvironment("ReverseProxy__Clusters__bff-cluster__Destinations__bff__Address", "http://srv-bff")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5000;
        endpoint.IsProxied = false;
    });

var nginx = builder.AddDockerfile("gw-nginx", "../..", "infrastructure/nginx/Dockerfile")
    .WithHttpEndpoint(port: 8080, targetPort: 443, name: "https", isProxied: false)
    .WithBindMount("../../certs", "/etc/nginx/certs")
    .WithEnvironment("GATEWAY_UPSTREAM", "host.docker.internal:5000")
    .WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway");

var web = builder.AddExecutable("ui-web", "npm", "../../frontend/team-hub-web", "start")
    .WithHttpsEndpoint(port: 4200, targetPort: 4200, name: "frontend", isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
