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

namespace dsstats.Tests
{
    public class MmrTests : IDisposable
    {
        private readonly IMapper mapper;
        private readonly IServiceProvider serviceProvider;

        private readonly DbConnection _connection;
        private readonly DbContextOptions<ReplayContext> _contextOptions;

        public MmrTests(IMapper mapper)
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _contextOptions = new DbContextOptionsBuilder<ReplayContext>()
                .UseSqlite(_connection)
                .Options;

            using (var context = new ReplayContext(_contextOptions)) {
                context.Database.EnsureCreated();
            }

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddDbContext<ReplayContext>(options =>
            {
                options.UseSqlite(_connection, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("SqliteMigrations");
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
                //.EnableDetailedErrors()
                //.EnableDetailedErrors()
                ;
            });

            serviceCollection.AddScoped<IRatingRepository, RatingRepository>();
            serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();
            serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
            serviceCollection.AddLogging();

            serviceProvider = serviceCollection.BuildServiceProvider();

            this.mapper = mapper;
        }

        public void Dispose() => _connection.Dispose();
        ReplayContext CreateContext() => new ReplayContext(_contextOptions);


        [Fact]
        public async Task Test()
        {
            using var scope = serviceProvider.CreateScope();
            using var context = CreateContext();

            var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

            Dictionary<int, CalcRating> mmrIdRatigns = new();
            Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic = new();

            var replays = await GetCmdrReplayDsRDtos(allStartTime: DateTime.MinValue, new DateTime(2022, 1, 1), 1);


            (mmrIdRatigns, double maxMmr) = await MmrService.GeneratePlayerRatings(replays,
                                                                            cmdrMmrDic,
                                                                            mmrIdRatigns,
                                                                            MmrService.startMmr,
                                                                            ratingRepository,
                                                                            new());

            Assert.True(replays.Any());
        }

        private async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(DateTime allStartTime, DateTime playerStartTime, int playerId)
        {
            var context = CreateContext();

            var replays = context.Replays
                .Include(r => r.ReplayPlayers)
                    .ThenInclude(rp => rp.Player)
                .Where(r => r.Playercount == 6
                    && r.Duration >= 300
                    && r.WinnerTeam > 0
                    && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic))
                .AsNoTracking();

            replays = replays.Where(r => (r.GameTime >= allStartTime)
                && (!r.ReplayPlayers.Any(p => p.PlayerId == playerId) || (r.GameTime >= playerStartTime)));

            return await replays
                .OrderBy(o => o.GameTime)
                    .ThenBy(o => o.ReplayId)
                .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}
