using dsstats.shared;
using System.Net;

namespace dsstats.api;

public static class ClientPartitionKeys
{
    public static string GetClientPartitionKey(HttpContext httpContext)
        => GetClientPartitionKey(httpContext.Connection.RemoteIpAddress);

    public static string GetClientPartitionKey(IPAddress? ipAddress)
    {
        var normalized = ClientIpAddress.Normalize(ipAddress);
        return normalized == ClientIpAddress.Unknown ? "unknown-client" : $"ip:{normalized}";
    }
}
