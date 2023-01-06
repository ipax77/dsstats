//using dsstats.mmr;
//using dsstats.mmr.ProcessData;
//using pax.dsstats.dbng.Services;
//using pax.dsstats.shared;
//using pax.dsstats.shared.Raven;
//using System;
//using System.Collections.Generic;
//using System.Net.Http.Headers;

//var accuracies = new Dictionary<(int, int), double>();

//for (int clip = 400; clip <= 400; clip += 100)
//{
//    for (int eloK = 0; eloK <= 128; eloK += 32)
//    {
//        var (replays, mmrIdRatings) = GetSynthesisedReplayDatas(clip);

//        (mmrIdRatings, double accuracy) = ProduceRatings(replays, mmrIdRatings, new MmrOptions(true, (eloK == 0 ? 1 : eloK), clip));
//        accuracies.Add(((eloK == 0 ? 1 : eloK), clip), accuracy);
//    }
//}

//var result = accuracies.OrderByDescending(_ => _.Value).ToList();
//Console.ReadKey();


//(List<ReplayData>, Dictionary<int, CalcRating>) GetSynthesisedReplayDatas(double clip)
//{
//    var result = new List<ReplayData>();
//    var mmrIdRatings = new Dictionary<int, CalcRating>();

//    int duration = 500;
//    double winnersConfidence = 0.15;
//    double winnersMmr = 1000;
//    double losersConfidence = 0.15;
//    double losersMmr = 1000;
//    double winnersExpectedResult = MmrService.EloExpectationToWin(winnersMmr, losersMmr, clip);

//    var replayData = new ReplayData(
//        gameTime: new DateTime(1),
//        duration: duration,
//        maxLeaver: 0,
//        maxkillsum: 10_000,
//        confidence: (winnersConfidence + losersConfidence) / 2,
//        winnerTeamData: new dsstats.mmr.ProcessData.TeamData(true, winnersMmr, winnersConfidence, 0, winnersExpectedResult, new PlayerData[]
//        {
//            new PlayerData(Commander.Artanis, Commander.Artanis, duration, false, winnersMmr, 0, winnersConfidence, 1, 1, 1, 1, true, 10_000, PlayerResult.Win),
//            new PlayerData(Commander.Artanis, Commander.Artanis, duration, false, winnersMmr, 0, winnersConfidence, 2, 2, 2, 2, true, 10_000, PlayerResult.Win),
//            new PlayerData(Commander.Artanis, Commander.Artanis, duration, false, winnersMmr, 0, winnersConfidence, 3, 3, 3, 3, true, 10_000, PlayerResult.Win),
//        }),
//        loserTeamData: new dsstats.mmr.ProcessData.TeamData(false, losersMmr, losersConfidence, 0, (1 - winnersExpectedResult), new PlayerData[]
//        {
//            new PlayerData(Commander.Artanis, Commander.Artanis, duration, false, losersMmr, 0, losersConfidence, 4, 4, 4, 4, true, 10_000, PlayerResult.Los),
//            new PlayerData(Commander.Artanis, Commander.Artanis, duration, false, losersMmr, 0, losersConfidence, 5, 5, 5, 5, true, 10_000, PlayerResult.Los),
//            new PlayerData(Commander.Artanis, Commander.Artanis, duration, false, losersMmr, 0, losersConfidence, 6, 6, 6, 6, true, 10_000, PlayerResult.Los),
//        })
//    );
//    result.Add(replayData);

//    foreach (var playerData in replayData.WinnerTeamData.Players.Concat(replayData.LoserTeamData.Players))
//    {
//        if (!mmrIdRatings.TryGetValue(playerData.MmrId, out var plRating))
//        {
//            plRating = mmrIdRatings[playerData.MmrId] = new CalcRating()
//            {
//                PlayerId = playerData.PlayerId,
//                Mmr = playerData.Mmr,
//                Consistency = playerData.Consistency,
//                Confidence = playerData.Confidence,
//                Games = 0,
//            };
//        }
//    }

//    return (result, mmrIdRatings);
//}

//(Dictionary<int, CalcRating>, double) ProduceRatings(List<ReplayData> replays, Dictionary<int, CalcRating> mmrIdRatings, MmrOptions mmrOptions)
//{
//    var cmdrMmrDic = new Dictionary<CmdrMmmrKey, CmdrMmmrValue>();

//    (mmrIdRatings, double accuracy) =
//        GeneratePlayerRatings(replays,
//                              cmdrMmrDic,
//                              mmrIdRatings,
//                              mmrOptions);

//    return (mmrIdRatings, accuracy);
//}

//(Dictionary<int, CalcRating>, double) GeneratePlayerRatings(List<ReplayData> replays,
//                                                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
//                                                                    Dictionary<int, CalcRating> mmrIdRatings,
//                                                                    MmrOptions mmrOptions)
//{
//    List<bool> accuracyList = new();
//    List<MmrChange> mmrChanges = new();

//    for (int i = 0; i < replays.Count; i++)
//    {
//        try
//        {
//            var (changes, correctPrediction) = ProcessReplay(replays[i], mmrIdRatings, cmdrMmrDic, mmrOptions);

//            if (changes != null)
//            {
//                mmrChanges.Add(changes);
//            }
//            if (correctPrediction != null)
//            {
//                accuracyList.Add(correctPrediction.Value);
//            }
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e.Message);
//        }
//    }

//    double accuracy = accuracyList.Count(x => x == true) / (double)accuracyList.Count;

//    return (mmrIdRatings, accuracy);
//}

//(MmrChange?, bool?) ProcessReplay(ReplayData replayData,
//                                  Dictionary<int, CalcRating> mmrIdRatings,
//                                  Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
//                                  MmrOptions mmrOptions)
//{
//    var mmrChanges = MmrService.ProcessReplay(replayData, mmrIdRatings, cmdrMmrDic, mmrOptions);

//    bool correctPrediction = (replayData.WinnerTeamData.ExpectedResult > 0.5);
//    return (new MmrChange() { Changes = mmrChanges }, correctPrediction);
//}