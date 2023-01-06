using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsstats.cli.MmrService
{
    public class RatingRepository : IRatingRepository
    {
        public Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos, MmrOptions mmrOptions)
        {
            throw new NotImplementedException();
        }

        public List<int> GetNameToonIds(string name)
        {
            throw new NotImplementedException();
        }

        public Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<List<MmrDevDto>> GetRatingsDeviation()
        {
            throw new NotImplementedException();
        }

        public Task<List<MmrDevDto>> GetRatingsDeviationStd()
        {
            throw new NotImplementedException();
        }

        public Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<RequestNames?> GetRequestNames(int toonId)
        {
            throw new NotImplementedException();
        }

        public Task<List<RequestNames>> GetRequestNames(string name)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetToonIdName(int toonId)
        {
            throw new NotImplementedException();
        }

        public Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<List<RequestNames>> GetTopPlayers(RatingType ratingType, int minGames)
        {
            throw new NotImplementedException();
        }

        public Task SetReplayListMmrChanges(List<ReplayListDto> replays, string? searchPlayer = null, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task SetReplayListMmrChanges(List<ReplayListDto> replays, int toonId, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId, string csvBasePath = "/data/mysqlfiles")
        {
            return Task.FromResult(0);
        }

        public Task<UpdateResult> UpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings, bool continueCalc, string csvBasePath = "/data/mysqlfiles")
        {
            return Task.FromResult(new UpdateResult());
        }
    }
}
