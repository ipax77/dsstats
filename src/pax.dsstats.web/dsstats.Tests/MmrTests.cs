using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using dsstats.mmr;
using pax.dsstats.web.Server.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using static System.Formats.Asn1.AsnWriter;
using dsstats.raven;
using pax.dsstats.shared.Raven;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using pax.dsstats.web.Client;

namespace dsstats.Tests
{
    public class MmrTests : IDisposable
    {
        private readonly IMapper mapper;
        //private readonly IServiceProvider serviceProvider;

        private readonly WebApplication app;
        private object allMmrIdRatings;

        //private readonly DbConnection _connection;
        //private readonly DbContextOptions<ReplayContext> _contextOptions;

        public MmrTests(IMapper mapper)
        {
            //_connection = new SqliteConnection($"Data Source={Path.Combine(Environment.CurrentDirectory, "dsstats3.db")}"/*"Filename=:memory:"*/);
            //_connection.Open();

            //_contextOptions = new DbContextOptionsBuilder<ReplayContext>()
            //    .UseSqlite(_connection)
            //    .Options;

            //using (var context = new ReplayContext(_contextOptions)) {
            //    context.Database.EnsureCreated();
            //}

            var builder = WebApplication.CreateBuilder(Array.Empty<string>());

            builder.Host.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("/data/localserverconfig.json", optional: false, reloadOnChange: false);
            });

            var serverVersion = new MySqlServerVersion(new Version(5, 7, 40));
            var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];

            //var serviceCollection = new ServiceCollection();

            //serviceCollection.AddDbContext<ReplayContext>(options =>
            //{
            //    options.UseSqlite(_connection, sqlOptions =>
            //    {
            //        sqlOptions.MigrationsAssembly("SqliteMigrations");
            //        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            //    })
            //    //.EnableDetailedErrors()
            //    //.EnableDetailedErrors()
            //    ;
            //});

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
            builder.Services.AddTransient<IReplayRepository, ReplayRepository>();

            //this.serviceProvider = serviceCollection.BuildServiceProvider();



            app = builder.Build();
            using var scope = app.Services.CreateScope();

            mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            mapper.ConfigurationProvider.AssertConfigurationIsValid();

            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            // context.Database.EnsureDeleted();
            context.Database.Migrate();

            this.mapper = mapper;
        }

        public void Dispose() { GC.SuppressFinalize(this); }
        public IServiceScope CreateScope() => app.Services.CreateScope();

        [Fact]
        public async Task Test()
        {
            using var scope = CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

            var players = await GetPlayers();

            var allReplays = await GetCmdrReplayDsRDtos(
                allStartTime: DateTime.MinValue,
                playerStartTime: default,
                playerId: null
            );
            (var allMmrIdRatings, double maxMmr) = await MmrService.GeneratePlayerRatings(allReplays,
                                                                            new(),
                                                                            new(),
                                                                            MmrService.startMmr,
                                                                            ratingRepository,
                                                                            new());

            var seasonalReplays = await GetCmdrReplayDsRDtos(
                allStartTime: new DateTime(2022, 1, 1),
                playerStartTime: default,
                playerId: null
            );
            (var seasonalMmrIdRatings, maxMmr) = await MmrService.GeneratePlayerRatings(seasonalReplays,
                                                                            new(),
                                                                            new(),
                                                                            MmrService.startMmr,
                                                                            ratingRepository,
                                                                            new());

            var seperatedSeasonalReplays = await GetCmdrReplayDsRDtos(
                allStartTime: DateTime.MinValue,
                playerStartTime: new DateTime(2022, 1, 1),
                playerId: 10758
            );
            (var seperatedSeasonalMmrIdRatings, maxMmr) = await MmrService.GeneratePlayerRatings(seperatedSeasonalReplays,
                                                                            new(),
                                                                            new(),
                                                                            MmrService.startMmr,
                                                                            ratingRepository,
                                                                            new());


            Dictionary<int, double> diffs = new();

            for (int i = 0; i < allMmrIdRatings.Count; i++) {
                int key = allMmrIdRatings.ElementAt(i).Key;

                if (!seasonalMmrIdRatings.ContainsKey(key)) {
                    continue;
                }

                var allPlR = allMmrIdRatings[key];
                var seasPlR = seasonalMmrIdRatings[key];

                diffs.Add(key, Math.Abs(allPlR.Mmr - seasPlR.Mmr));
            }

            Assert.True(true);
        }

        private async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(DateTime allStartTime, DateTime playerStartTime, int? playerId)
        {
            using var scope = CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var replays = context.Replays
                .Include(r => r.ReplayPlayers)
                    .ThenInclude(rp => rp.Player)
                .Where(r => r.Playercount == 6
                    && r.Duration >= 300
                    && r.WinnerTeam > 0
                    && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic))
                .AsNoTracking();

            replays = replays.Where(r => (r.GameTime >= allStartTime)
                && (!playerId.HasValue || !r.ReplayPlayers.Any(p => p.PlayerId == playerId) || (r.GameTime >= playerStartTime)));

            return await replays
                .OrderBy(o => o.GameTime)
                    .ThenBy(o => o.ReplayId)
                .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        private async Task<List<PlayerDsRDto>> GetPlayers()
        {
            using var scope = CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var players = context.Players
                .AsNoTracking();

            return await players
                .OrderBy(o => o.PlayerId)
                .ProjectTo<PlayerDsRDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}
