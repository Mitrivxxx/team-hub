using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

namespace TeamHub.AppHost;

/// <summary>
/// Connection settings for Compose-backed local infra (project team-hub-dev).
/// </summary>
internal sealed class DevInfraOptions
{
    public const string SectionName = "Aspire:DevInfra";

    public PostgresOptions Postgres { get; set; } = new();
    public RedisOptions Redis { get; set; } = new();
    public KafkaOptions Kafka { get; set; } = new();
    public BlobStorageOptions BlobStorage { get; set; } = new();

    public sealed class PostgresOptions
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5433;
        public string Username { get; set; } = "teamhub";
        public string Password { get; set; } = string.Empty;
    }

    public sealed class RedisOptions
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 6379;
    }

    public sealed class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "127.0.0.1:9092";
    }

    public sealed class BlobStorageOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "avatars";
        public string PublicBlobEndpoint { get; set; } = "http://127.0.0.1:10000/devstoreaccount1";
        public int SasExpiryMinutes { get; set; } = 60;
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 10000;
    }

    public string PostgresConnectionString(string database) =>
        $"Host={Postgres.Host};Port={Postgres.Port};Database={database};Username={Postgres.Username};Password={Postgres.Password}";

    public string RedisConnectionString => $"{Redis.Host}:{Redis.Port}";
}

internal static class DevInfra
{
    public static DevInfraOptions Load(IConfiguration configuration)
    {
        var options = new DevInfraOptions();
        configuration.GetSection(DevInfraOptions.SectionName).Bind(options);

        if (string.IsNullOrWhiteSpace(options.Postgres.Password))
            throw new InvalidOperationException(
                "Aspire:DevInfra:Postgres:Password is required in AppHost configuration.");

        if (string.IsNullOrWhiteSpace(options.BlobStorage.ConnectionString))
            throw new InvalidOperationException(
                "Aspire:DevInfra:BlobStorage:ConnectionString is required in AppHost configuration.");

        return options;
    }

    /// <summary>
    /// Fail fast when compose-dev infra is not reachable on the host loopback ports.
    /// </summary>
    public static void EnsureReachable(DevInfraOptions infra, bool requireBlobStorage)
    {
        var endpoints = new List<(string Name, string Host, int Port)>
        {
            ("Postgres", infra.Postgres.Host, infra.Postgres.Port),
            ("Redis", infra.Redis.Host, infra.Redis.Port),
            ("Kafka", ParseHost(infra.Kafka.BootstrapServers), ParsePort(infra.Kafka.BootstrapServers, 9092))
        };

        if (requireBlobStorage)
            endpoints.Add(("Azurite", infra.BlobStorage.Host, infra.BlobStorage.Port));

        var missing = new List<string>();
        foreach (var (name, host, port) in endpoints)
        {
            if (!CanConnect(host, port))
                missing.Add($"{name} ({host}:{port})");
        }

        if (missing.Count == 0)
            return;

        throw new InvalidOperationException(
            "Compose dev infra is not reachable: " + string.Join(", ", missing) + ". " +
            "Start it first: ./scripts/compose-dev.sh up -d --wait");
    }

    static bool CanConnect(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            return task.Wait(TimeSpan.FromSeconds(2)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    static string ParseHost(string bootstrapServers)
    {
        var first = bootstrapServers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        var colon = first.LastIndexOf(':');
        return colon > 0 ? first[..colon] : first;
    }

    static int ParsePort(string bootstrapServers, int fallback)
    {
        var first = bootstrapServers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        var colon = first.LastIndexOf(':');
        if (colon > 0 && int.TryParse(first[(colon + 1)..], out var port))
            return port;
        return fallback;
    }
}

internal static class DevInfraResourceExtensions
{
    public static IResourceBuilder<T> WithTeamHubDevPostgres<T>(
        this IResourceBuilder<T> builder,
        DevInfraOptions infra,
        string database)
        where T : IResourceWithEnvironment =>
        builder.WithEnvironment(
            "ConnectionStrings__DefaultConnection",
            infra.PostgresConnectionString(database));

    public static IResourceBuilder<T> WithTeamHubDevRedis<T>(
        this IResourceBuilder<T> builder,
        DevInfraOptions infra)
        where T : IResourceWithEnvironment =>
        builder.WithEnvironment("Redis__ConnectionString", infra.RedisConnectionString);

    public static IResourceBuilder<T> WithTeamHubDevKafka<T>(
        this IResourceBuilder<T> builder,
        DevInfraOptions infra,
        string clientId)
        where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Kafka__BootstrapServers", infra.Kafka.BootstrapServers)
            .WithEnvironment("Kafka__ClientId", clientId);

    public static IResourceBuilder<T> WithTeamHubDevBlobStorage<T>(
        this IResourceBuilder<T> builder,
        DevInfraOptions infra)
        where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("BlobStorage__ConnectionString", infra.BlobStorage.ConnectionString)
            .WithEnvironment("BlobStorage__ContainerName", infra.BlobStorage.ContainerName)
            .WithEnvironment("BlobStorage__PublicBlobEndpoint", infra.BlobStorage.PublicBlobEndpoint)
            .WithEnvironment("BlobStorage__SasExpiryMinutes", infra.BlobStorage.SasExpiryMinutes.ToString())
            .WithEnvironment("ConnectionStrings__blobs", infra.BlobStorage.ConnectionString);
}
