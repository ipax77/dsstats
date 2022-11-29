using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace dsstats.installer;
class Program
{
    private static readonly string certPath = "dsstats.cer";

    static void Main(string[] args)
    {
        X509Certificate2 cert = new X509Certificate2(certPath);

        using (X509Store store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
        }

        Process.GetCurrentProcess().Close();
    }
}
