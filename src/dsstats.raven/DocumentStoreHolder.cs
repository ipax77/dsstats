using Raven.Client.Documents;

namespace dsstats.raven;
public class DocumentStoreHolder
{
    private static readonly Lazy<IDocumentStore> _store = new Lazy<IDocumentStore>(CreateDocumentStore);

    private static IDocumentStore CreateDocumentStore()
    {
        string serverURL = "http://localhost:9102";
        string databaseName = "mmrdb";

        IDocumentStore documentStore = new DocumentStore
        {
            Urls = new[] {serverURL},
            Database = databaseName
        };

        documentStore.Initialize();

        // indices
        new PlayerRating_ByToonId().Execute(documentStore);
        new PlayerRating_ByPlayerId().Execute(documentStore);
        new ReplayPlayerMmrChange_ByReplayPlayerId().Execute(documentStore);

        return documentStore;
    }

    public static IDocumentStore Store
    {
        get { return _store.Value; }
    }
}
