using System.Net;

namespace dsstats.shared;

public static class ClientIpAddress
{
    public const string Unknown = "unknown";

    public static string Normalize(IPAddress? ipAddress)
    {
        if (ipAddress is null)
        {
            return Unknown;
        }

        return ipAddress.IsIPv4MappedToIPv6
            ? ipAddress.MapToIPv4().ToString()
            : ipAddress.ToString();
    }
}
