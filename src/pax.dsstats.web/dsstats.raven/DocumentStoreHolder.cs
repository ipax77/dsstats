using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Raven.Client.Documents;

namespace dsstats.raven;
public class DocumentStoreHolder
{
    private static readonly Lazy<IDocumentStore> _store = new Lazy<IDocumentStore>(CreateDocumentStore);

    private static IDocumentStore CreateDocumentStore()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var ravenCertPassword = config.GetProperty("RavenCertPassword").GetString();
        var ravenServerUrl = config.GetProperty("RavenServerUrl").GetString();

        X509Certificate2 clientCertificate = new X509Certificate2("/data/ravenClientCert.pfx", ravenCertPassword);

        // string serverURL = "http://localhost:9102";
        string serverURL = ravenServerUrl ?? "http://localhost:9102";
        string databaseName = "mmrdb";


        IDocumentStore documentStore = new DocumentStore
        {
            Urls = new[] { serverURL },
            Database = databaseName,
            // Certificate = clientCertificate
        };

        documentStore.Initialize();

        // indices

        return documentStore;
    }

    public static IDocumentStore Store
    {
        get { return _store.Value; }
    }
}
