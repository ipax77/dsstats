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
            const double startClip = 168;
            double clip = 1600; //startClip; //
            List<ReplayData> replayDatas;
            double realAccuracy;
            double loss;
            double loss_d;

            var results = new List<(double, double, double, double)>();
            do
            {
                replayDatas = await ProduceRatings(new MmrOptions(true, startClip, clip));
                replayDatas = GetReplayDatasOfPlayer(10758, replayDatas);

                var winRate = GetWinRate(10758, replayDatas);
                var avgETW = GetAvgETW(10758, replayDatas);
                var confidence = 1 - Math.Abs(winRate - avgETW);

                realAccuracy = replayDatas.Count(x => x.CorrectPrediction) / (double)replayDatas.Count;

                loss = Loss.GetLoss(clip, replayDatas);
                //loss_d = Loss.GetLoss_d(clip, replayDatas.ToArray());

                results.Add((clip, realAccuracy, loss, 0/*-loss_d*/));

                //clip += 500;//-loss_d * startClip;
            } while (loss > 0);

            return results;
        }

        public async Task<double> ConfidenceTest(int playerId = 10758)
        {
            var replayDatas = await ProduceRatings(new MmrOptions(true));
            replayDatas = GetReplayDatasOfPlayer(playerId, replayDatas);

            var winRate = GetWinRate(playerId, replayDatas);
            var avgETW = GetAvgETW(playerId, replayDatas);
            var confidence = 1 - Math.Abs(winRate - avgETW);

            return confidence;
        }

        private static double GetAvgETW(int playerId, List<ReplayData> replayDatas)
        {
            double avgETW_sum = 0;
            foreach (var replayData in replayDatas)
            {
                if (replayData.WinnerTeamData.Players.Any(p => p.PlayerId == playerId))
                {
                    avgETW_sum += replayData.WinnerTeamData.ExpectedResult;
                }
                else
                {
                    avgETW_sum += replayData.LoserTeamData.ExpectedResult;
                }
            }
            return avgETW_sum / replayDatas.Count;
        }

        private static double GetWinRate(int playerId, List<ReplayData> replayDatas)
        {
            int winCounts = replayDatas.Count(r => r.WinnerTeamData.Players.Any(p => p.PlayerId == playerId));
            return winCounts / (double)replayDatas.Count;
        }

        private static List<ReplayData> GetReplayDatasOfPlayer(int playerId, List<ReplayData> replayDatas)
        {
            var listWinnerTeam = replayDatas.Where(r => r.WinnerTeamData.Players.Any(p => p.PlayerId == playerId));
            var listLoserTeam = replayDatas.Where(r => r.LoserTeamData.Players.Any(p => p.PlayerId == playerId));
            return listWinnerTeam.Concat(listLoserTeam).ToList();
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