using AutoMapper;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using dsstats.mmr.ProcessData;

namespace AlgoParamTests;

public class RealDataTests
{
    private readonly WebApplication app;

    public RealDataTests()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("/data/localserverconfig.json", optional: false, reloadOnChange: false);
        });

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 40));
        var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];
        var importConnectionString = builder.Configuration["ServerConfig:ImportTestConnectionString"];

        builder.Services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(120);
                p.EnableRetryOnFailure();
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            ;
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        builder.Services.AddLogging();

        builder.Services.AddScoped<IRatingRepository, RatingRepository>();
        builder.Services.AddScoped<MmrProduceService>();
        builder.Services.AddTransient<IReplayRepository, ReplayRepository>();

        app = builder.Build();

        Data.MysqlConnectionString = importConnectionString;
    }

    public List<KeyValuePair<(double, double), double>> MmrOptionsTest()
    {
        using var scope = app.Services.CreateScope();
        MmrProduceService produceService = scope.ServiceProvider.GetService<MmrProduceService>()!;

        //double a = produceService.ProduceRatings(new MmrOptions(true, 168, 1600)).GetAwaiter().GetResult();

        var accuracies = new Dictionary<(double, double), double>();

        //int count = 0;
        //for (double clip = 5.5; clip <= 6.5; clip += 0.01)
        //{
        //    var mmrOptions = new MmrOptions(true, 1, clip);
        //    double accuracy = produceService.ProduceRatings(mmrOptions).GetAwaiter().GetResult();
        //    accuracies.Add((1, clip), accuracy);

        //    PrintState(accuracies, ++count);

        //    //for (int eloK = 8; eloK <= 256; eloK += 8)
        //    //{
        //    //    var mmrOptions = new MmrOptions(true, (eloK == 0 ? 1 : eloK), clip);
        //    //    double accuracy = produceService.ProduceRatings(mmrOptions).GetAwaiter().GetResult();
        //    //    accuracies.Add(((eloK == 0 ? 1 : eloK), clip), accuracy);

        //    //    PrintState(accuracies, ++count);
        //    //}
        //}

        return accuracies.OrderByDescending(_ => _.Value).ToList();
    }

    //private void PrintState(Dictionary<(double, double), double> data, int count)
    //{
    //    Console.Clear();

    //    foreach (var item in data.OrderBy(x => x.Value))
    //    {
    //        Console.WriteLine("<{0}|{1}> {2}", item.Key.Item1, item.Key.Item2, item.Value);
    //    }

    //    Console.WriteLine("\nCount: {0}", count);
    //}

    public void DerivationTest()
    {
        using var scope = app.Services.CreateScope();
        MmrProduceService produceService = scope.ServiceProvider.GetService<MmrProduceService>()!;

        const double startClip = 168;
        double clip = startClip; //985
        List<ReplayData> replayDatas;
        double realAccuracy;
        double loss;
        double loss_d;

        var values = new List<(double, double, double, double)>();
        do
        {
            replayDatas = produceService.ProduceRatings(new MmrOptions(true, startClip, clip)).GetAwaiter().GetResult();
            realAccuracy = replayDatas.Count(x => x.CorrectPrediction) / (double)replayDatas.Count;

            loss = Loss.GetLoss(clip, replayDatas);
            //loss_d = Loss.GetLoss_d(clip, replayDatas.ToArray());

            values.Add((clip, realAccuracy, loss, 0/*-loss_d*/));

            clip += 500;//-loss_d * startClip;
        } while (loss > 0);
    }
}
