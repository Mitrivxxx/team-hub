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

var auth = builder.AddProject<Projects.team_hub_auth>("team-hub-auth")
    .WithReference(authDatabase, "DefaultConnection")
    .WithEnvironment("Redis__ConnectionString", redis)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// launchSettings already registers endpoint "http" on :5000 — configure it, do not add a second one.
var gateway = builder.AddProject<Projects.team_hub_gateway>("team-hub-gateway")
    .WithReference(auth)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address", "https+http://team-hub-auth")
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
    .WithHttpEndpoint(port: 4200, targetPort: 4200, name: "frontend", isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
