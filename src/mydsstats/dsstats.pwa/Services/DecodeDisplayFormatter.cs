using System.Globalization;

namespace dsstats.pwa.Services;

internal static class DecodeDisplayFormatter
{
    public static string FormatElapsed(DecodeInfoEventArgs decodeInfo)
    {
        if (!decodeInfo.Finished)
        {
            return decodeInfo.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        if (decodeInfo.Elapsed < TimeSpan.FromMinutes(1))
        {
            return $"{decodeInfo.Elapsed.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s";
        }

        return decodeInfo.Elapsed < TimeSpan.FromHours(1)
            ? decodeInfo.Elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture)
            : decodeInfo.Elapsed.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
    }
}
