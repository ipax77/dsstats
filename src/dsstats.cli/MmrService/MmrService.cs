using dsstats.mmr.ProcessData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace dsstats.cli.MmrService
{
    public class MmrService
    {
        readonly IServiceProvider serviceProvider;

        public MmrService()
        {
            var services = new ServiceCollection();

            var serverVersion = new MySqlServerVersion(new Version(5, 7, 40));
            var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
            var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
            var config = json.GetProperty("ServerConfig");
            var connectionString = config.GetProperty("DsstatsConnectionString").GetString();

            services.AddDbContext<ReplayContext>(options =>
            {
                options.UseMySql(connectionString, serverVersion, p =>
                {
                    p.CommandTimeout(120);
                    p.MigrationsAssembly("MysqlMigrations");
                    p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                });
            });

            services.AddMemoryCache();
            services.AddAutoMapper(typeof(AutoMapperProfile));
            services.AddLogging();

            services.AddScoped<IRatingRepository, RatingRepository>();
            services.AddScoped<MmrProduceService>();
            services.AddTransient<IReplayRepository, ReplayRepository>();

            serviceProvider = services.BuildServiceProvider();
        }

        //public void Do()
        //{
        //    using var scope = serviceProvider.CreateScope();
        //    using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        //}

        public async Task<List<(double, double, double, double)>> DerivationTest()
        {
            using var scope = serviceProvider.CreateScope();

            const double startClip = 168;
            double clip = startClip; //985
            List<ReplayData> replayDatas;
            double realAccuracy;
            double loss;
            double loss_d;

            var results = new List<(double, double, double, double)>();
            do
            {
                replayDatas = await ProduceRatings(new MmrOptions(true, startClip, clip));
                realAccuracy = replayDatas.Count(x => x.CorrectPrediction) / (double)replayDatas.Count;

                loss = Loss.GetLoss(clip, replayDatas);
                //loss_d = Loss.GetLoss_d(clip, replayDatas.ToArray());

                results.Add((clip, realAccuracy, loss, 0/*-loss_d*/));

                clip += 500;//-loss_d * startClip;
            } while (loss > 0);

            return results;
        }

        private async Task<List<ReplayData>> ProduceRatings(MmrOptions mmrOptions,
                                        DateTime latestReplay = default,
                                        List<ReplayDsRDto>? dependentReplays = null,
                                        DateTime startTime = default,
                                        DateTime endTime = default)
        {
            Stopwatch sw = Stopwatch.StartNew();

            using var scope = serviceProvider.CreateScope();
            var produceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();

            var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

            var cmdrMmrDic = await produceService.GetCommanderMmrsDic(mmrOptions, true);

            if (!mmrOptions.ReCalc
                && dependentReplays != null
                && dependentReplays.Any()
                && dependentReplays.Any(a => a.GameTime < latestReplay))
            {
                mmrOptions.ReCalc = true;
                dependentReplays = null;
            }

            var mmrIdRatings = await produceService.GetMmrIdRatings(mmrOptions, ratingRepository, dependentReplays);
            int mmrChangesAppendId = await produceService.GetMmrChangesAppendId(mmrOptions);

            if (mmrOptions.ReCalc)
            {
                latestReplay = startTime;
            }

            (latestReplay, List<ReplayData> replayDatas) = await produceService.ProduceRatings(mmrOptions, cmdrMmrDic, mmrIdRatings, ratingRepository, mmrChangesAppendId, latestReplay, endTime);

            await produceService.SaveCommanderMmrsDic(cmdrMmrDic);
            sw.Stop();

            return replayDatas;
        }
    }
}