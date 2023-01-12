using pax.dsstats.shared;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using System.Text.Json;

namespace dsstats.raven;
public class DocumentStoreHolder
{
    private static readonly Lazy<IDocumentStore> _store = new Lazy<IDocumentStore>(CreateDocumentStore);

    private static IDocumentStore CreateDocumentStore()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        // var ravenCertPassword = config.GetProperty("RavenCertPassword").GetString();
        var ravenServerUrl = config.GetProperty("RavenServerUrl").GetString();

        // X509Certificate2 clientCertificate = new X509Certificate2("/data/ravenClientCert.pfx", ravenCertPassword);

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
        new MmrChange_ByHash().Execute(documentStore);
        new RatingStd_Average_ByMmr().Execute(documentStore);
        new RatingCmdr_Average_ByMmr().Execute(documentStore);

        return documentStore;
    }

    public static IDocumentStore Store
    {
        get { return _store.Value; }
    }
}

public class MmrChange_ByHash : AbstractIndexCreationTask<MmrChange>
{
    public MmrChange_ByHash()
    {
        Map = changes => from change in changes
                         select new
                         {
                             change.Hash
                         };
    }
}

public class RatingCmdr_Average_ByMmr : AbstractIndexCreationTask<RavenRating, RatingCmdr_Average_ByMmr.Result>
{
    public class Result
    {
        public int Mmr { get; set; }
        public int Count { get; set; }
    }

    public RatingCmdr_Average_ByMmr()
    {
        Map = ratings => from rating in ratings
                         where rating.Type == RatingType.Cmdr
                         select new
                         {
                             Mmr = (int)rating.Mmr,
                             Count = 1
                         };

        Reduce = results => from result in results
                            group result by result.Mmr into g
                            select new
                            {
                                Mmr = g.Key,
                                Count = g.Sum(s => s.Count)
                            };
    }
}

public class RatingStd_Average_ByMmr : AbstractIndexCreationTask<RavenRating, RatingStd_Average_ByMmr.Result>
{
    public class Result
    {
        public int Mmr { get; set; }
        public int Count { get; set; }
    }

    public RatingStd_Average_ByMmr()
    {
        Map = ratings => from rating in ratings
                         where rating.Type == RatingType.Std
                         select new
                         {
                             Mmr = (int)rating.Mmr,
                             Count = 1
                         };

        Reduce = results => from result in results
                            group result by result.Mmr into g
                            select new
                            {
                                Mmr = g.Key,
                                Count = g.Sum(s => s.Count)
                            };
    }


}