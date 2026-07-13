using System.Text.Json;
using System.Text.Json.Serialization;
using MySqlConnector;

namespace dsstats.discord.cli;

internal sealed record DiscordSettings(
    string Token,
    string ServerId,
    string ChannelId,
    string ConnectionString = "",
    bool Production = false)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<DiscordSettings> LoadAsync(
        string discordConfigPath,
        string databaseConfigPath,
        bool production,
        bool requireToken,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(discordConfigPath))
        {
            throw new FileNotFoundException(
                $"Discord configuration file not found: {discordConfigPath}",
                discordConfigPath);
        }

        await using var stream = new FileStream(discordConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var root = await JsonSerializer.DeserializeAsync<ConfigRoot>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Configuration file is empty: {discordConfigPath}");
        var discord = root.Discord
            ?? throw new InvalidOperationException("Missing 'discord' configuration section.");
        var connectionString = production
            ? GetConnectionString(root, discordConfigPath)
            : await LoadDevelopmentConnectionStringAsync(databaseConfigPath, cancellationToken);
        var token = Environment.GetEnvironmentVariable("DSSTATS_DISCORD_TOKEN") ?? discord.Token;

        if (requireToken && string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Missing discord:Token (or DSSTATS_DISCORD_TOKEN). Discord's API does not support user/password login; create a bot token and grant that bot View Channel and Read Message History permissions.");
        }

        if (!IsSnowflake(discord.ServerId))
        {
            throw new InvalidOperationException("discord:ServerID must be a numeric Discord server ID.");
        }

        if (!IsSnowflake(discord.ChannelId))
        {
            throw new InvalidOperationException("discord:ChannelID must be a numeric Discord channel ID.");
        }

        return new(token ?? string.Empty, discord.ServerId!, discord.ChannelId!, connectionString, production);
    }

    private static async Task<string> LoadDevelopmentConnectionStringAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Database configuration file not found: {path}", path);
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var root = await JsonSerializer.DeserializeAsync<ConfigRoot>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Database configuration file is empty: {path}");
        var connectionString = GetConnectionString(root, path);

        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (!IsLoopbackHost(builder.Server))
        {
            throw new InvalidOperationException(
                $"Refusing non-development database host '{builder.Server}'. Patch-note CLI database connections must target localhost.");
        }

        return connectionString;
    }

    private static string GetConnectionString(ConfigRoot root, string path) =>
        !string.IsNullOrWhiteSpace(root.Dsstats?.ConnectionString)
            ? root.Dsstats.ConnectionString
            : throw new InvalidOperationException($"Missing dsstats:ConnectionString in {path}.");

    private static bool IsLoopbackHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
        || string.Equals(host, "::1", StringComparison.Ordinal);

    private static bool IsSnowflake(string? value) => ulong.TryParse(value, out var parsed) && parsed > 0;

    private sealed class ConfigRoot
    {
        public DiscordConfig? Discord { get; init; }
        public DsstatsConfig? Dsstats { get; init; }
    }

    private sealed class DsstatsConfig
    {
        public string? ConnectionString { get; init; }
    }

    private sealed class DiscordConfig
    {
        public string? Token { get; init; }

        [JsonPropertyName("ServerID")]
        public string? ServerId { get; init; }

        [JsonPropertyName("ChannelID")]
        public string? ChannelId { get; init; }
    }
}
