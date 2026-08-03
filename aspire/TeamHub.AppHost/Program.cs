var builder = DistributedApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Aspire:Jwt:Key"]
    ?? throw new InvalidOperationException("Aspire:Jwt:Key is required in AppHost configuration.");
var jwtIssuer = builder.Configuration["Aspire:Jwt:Issuer"] ?? "AuthService";
var jwtAudience = builder.Configuration["Aspire:Jwt:Audience"] ?? "AuthServiceUsers";

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Aspire resource names allow only letters, digits, hyphens — DB name can still be auth_db.
var authDatabase = postgres.AddDatabase("auth-db", "auth_db");

var redis = builder.AddRedis("redis");

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobs("blobs");

var organizationDatabase = postgres.AddDatabase("organization-db", "organization_db");

var auth = builder.AddProject<Projects.team_hub_auth>("team-hub-auth")
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

var team = builder.AddProject<Projects.team_hub_organization>("team-hub-organization")
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
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEndpoint("grpc", endpoint =>
    {
        endpoint.Port = 5102;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
    });

var notification = builder.AddProject<Projects.team_hub_notification>("team-hub-notification")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5004;
        endpoint.IsProxied = false;
    });

var bff = builder.AddProject<Projects.team_hub_bff>("team-hub-bff")
    .WithReference(auth)
    .WithReference(team)
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
var gateway = builder.AddProject<Projects.team_hub_gateway>("team-hub-gateway")
    .WithReference(auth)
    .WithReference(team)
    .WithReference(notification)
    .WithReference(bff)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address", "http://team-hub-auth")
    .WithEnvironment("ReverseProxy__Clusters__team-cluster__Destinations__team__Address", "http://team-hub-organization")
    .WithEnvironment("ReverseProxy__Clusters__bff-cluster__Destinations__bff__Address", "http://team-hub-bff")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5000;
        endpoint.IsProxied = false;
    });

var nginx = builder.AddDockerfile("nginx", "../..", "infrastructure/nginx/Dockerfile")
    .WithHttpEndpoint(port: 8080, targetPort: 443, name: "https", isProxied: false)
    .WithBindMount("../../certs", "/etc/nginx/certs")
    .WithEnvironment("GATEWAY_UPSTREAM", "host.docker.internal:5000")
    .WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway");

var web = builder.AddExecutable("web", "npm", "../../frontend/team-hub-web", "start")
    .WithHttpsEndpoint(port: 4200, targetPort: 4200, name: "frontend", isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
