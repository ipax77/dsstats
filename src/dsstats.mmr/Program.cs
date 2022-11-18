using System.Diagnostics;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Indexes;

namespace dsstats.mmr;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        using (var store = new DocumentStore
        {
            Urls = new string[] { "http://localhost:9102" },
            Database = "mmrdb"
        })
        {
            store.Initialize();

            // using (var session = store.OpenSession())
            // {
            //     var data = GetRandomData();
            //     session.Store(data);
            //     session.SaveChanges();
            // }


            // BULK INSERT
            // var data = GetRandomData();


            // using (BulkInsertOperation bulkInsert = store.BulkInsert())
            // {
            //     foreach (var d in data)
            //     {
            //         bulkInsert.Store(d);
            //     }
            // };


            // GETDATA
            using var session = store.OpenSession();

            Stopwatch sw = Stopwatch.StartNew();

            var mmr = session
                .Query<ToonIdMmr, Auto_ToonIdMmrs_ByToonId>()
                .Where(x => x.ToonId == 5555)
                .Select(s => s.Mmr)
                .FirstOrDefault();


            sw.Stop();
            Console.WriteLine($"elapsed {sw.ElapsedMilliseconds} ms");

            Console.WriteLine($"mmr: {mmr}");
        }
    }

    private static List<ToonIdMmr> GetRandomData()
    {
        Random random = new();
        List<ToonIdMmr> toonIdMmrs = new List<ToonIdMmr>();
        for (int i = 0; i < 100000; i++)
        {
            toonIdMmrs.Add(new ToonIdMmr()
            {
                ToonId = i + 1,
                Mmr = random.Next(500, 2000)
            });
        }
        return toonIdMmrs;
    }
}

public class ToonIdMmr
{
    public int ToonId { get; set; }
    public float Mmr { get; set; }
}

public class Auto_ToonIdMmrs_ByToonId : AbstractIndexCreationTask<ToonIdMmr>
{
    public Auto_ToonIdMmrs_ByToonId()
    {
        Map = toonIdMmrs => from toonIdMmr in toonIdMmrs
                           select new
                           {
                                ToonId = toonIdMmr.ToonId
                           };
    }
}