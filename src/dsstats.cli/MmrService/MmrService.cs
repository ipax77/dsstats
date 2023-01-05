using dsstats.mmr.ProcessData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsstats.cli.MmrService
{
    public class MmrService
    {
        readonly IServiceProvider serviceProvider;

        public MmrService(string sqliteDbFile)
        {
            var services = new ServiceCollection();

            services.AddDbContext<ReplayContext>(options =>
            {
                options.UseSqlite($"Data Source={sqliteDbFile}",
                    x =>
                    {
                        x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
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

        public void DerivationTest()
        {
            using var scope = serviceProvider.CreateScope();
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
}
