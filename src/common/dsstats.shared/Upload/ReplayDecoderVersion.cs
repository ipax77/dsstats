using System.Reflection;

namespace dsstats.shared.Upload;

public enum ReplayDecoderSource : byte
{
    Unknown = 0,
    Maui = 1,
    MyDsstats = 2,
    Service = 3,
    Api = 4,
}

public readonly record struct ParsedReplayDecoderVersion(
    ReplayDecoderSource Source,
    string Version);

public static class ReplayDecoderVersion
{
    public const int MaxWireValueLength = 32;
    public const int MaxDecoderVersionLength = 24;
    public const string UnknownVersion = "unknown";
    public const string MauiPrefix = "ma";
    public const string MyDsstatsPrefix = "myds";
    public const string ServicePrefix = "ser";
    public const string ApiPrefix = "api";

    public static string Format(ReplayDecoderSource source, Assembly assembly)
        => Format(source, GetReleaseVersion(assembly));

    public static string Format(ReplayDecoderSource source, Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        var prefix = GetPrefix(source);
        var build = Math.Max(0, version.Build);
        return string.Create(
            prefix.Length + CountDigits(version.Major) + CountDigits(version.Minor) + CountDigits(build) + 2,
            (prefix, version.Major, version.Minor, build),
            static (destination, state) =>
            {
                state.prefix.AsSpan().CopyTo(destination);
                var position = state.prefix.Length;
                position += WriteNumber(destination[position..], state.Major);
                destination[position++] = '.';
                position += WriteNumber(destination[position..], state.Minor);
                destination[position++] = '.';
                WriteNumber(destination[position..], state.build);
            });
    }

    public static ParsedReplayDecoderVersion Parse(string? rawVersion)
    {
        var value = rawVersion.AsSpan().Trim();
        if (value.IsEmpty)
        {
            return new(ReplayDecoderSource.Maui, UnknownVersion);
        }

        if (TryRemovePrefix(value, MyDsstatsPrefix, out var suffix))
        {
            return Create(ReplayDecoderSource.MyDsstats, suffix);
        }

        if (TryRemovePrefix(value, ServicePrefix, out suffix))
        {
            return Create(ReplayDecoderSource.Service, suffix);
        }

        if (TryRemovePrefix(value, ApiPrefix, out suffix))
        {
            return Create(ReplayDecoderSource.Api, suffix);
        }

        if (TryRemovePrefix(value, MauiPrefix, out suffix))
        {
            return Create(ReplayDecoderSource.Maui, suffix);
        }

        // Unprefixed versions were emitted by the legacy MAUI client.
        return Create(ReplayDecoderSource.Maui, value);
    }

    public static Version GetReleaseVersion(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var version = assembly.GetName().Version;
        return version is null
            ? new(0, 0, 0)
            : new(version.Major, version.Minor, Math.Max(0, version.Build));
    }

    public static string LimitVersionForStorage(string version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version.Length <= MaxDecoderVersionLength
            ? version
            : version[..MaxDecoderVersionLength];
    }

    private static ParsedReplayDecoderVersion Create(
        ReplayDecoderSource source,
        ReadOnlySpan<char> version)
    {
        var trimmed = version.Trim();
        return new(source, trimmed.IsEmpty ? UnknownVersion : trimmed.ToString());
    }

    private static bool TryRemovePrefix(
        ReadOnlySpan<char> value,
        string prefix,
        out ReadOnlySpan<char> suffix)
    {
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            suffix = value[prefix.Length..];
            return true;
        }

        suffix = default;
        return false;
    }

    private static string GetPrefix(ReplayDecoderSource source) => source switch
    {
        ReplayDecoderSource.Maui => MauiPrefix,
        ReplayDecoderSource.MyDsstats => MyDsstatsPrefix,
        ReplayDecoderSource.Service => ServicePrefix,
        ReplayDecoderSource.Api => ApiPrefix,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown decoder sources cannot be emitted."),
    };

    private static int CountDigits(int value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    private static int WriteNumber(Span<char> destination, int value)
    {
        value.TryFormat(destination, out var written);
        return written;
    }
}
