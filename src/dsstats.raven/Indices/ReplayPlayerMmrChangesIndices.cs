
using pax.dsstats.shared;
using Raven.Client.Documents.Indexes;

namespace dsstats.raven;

public class ReplayPlayerMmrChange_ByReplayPlayerId : AbstractIndexCreationTask<ReplayPlayerMmrChange>
{
    public ReplayPlayerMmrChange_ByReplayPlayerId()
    {
        Map = replayPlayerMmrChanges => from replayPlayerMmrChange in replayPlayerMmrChanges
                            select new
                            {
                                ReplayPlayerId = replayPlayerMmrChange.ReplayPlayerId
                            };
    }
}