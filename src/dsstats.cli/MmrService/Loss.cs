using dsstats.mmr.ProcessData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsstats.cli.MmrService;

public static class Loss
{
    public static double GetLoss(double clip, List<ReplayData> replays)
    {
        var accuracies = GetAccuracies(replays);
        int totalCount = accuracies.Sum(x => x.Value.Item2);

        double loss = 0;
        foreach (var ent in accuracies)
        {
            double weighting = ent.Value.Item2 / (double)totalCount;

            loss += weighting * Math.Abs(ent.Key - ent.Value.Item1);
        }
        return loss;

        //double summedLoss = 0;
        //foreach (var replay in replays)
        //{
        //    summedLoss += GetLossSingle(replay.WinnerTeamData.Mmr, replay.LoserTeamData.Mmr, replay.WinnerTeamData.ActualResult, clip);

        //    if (double.IsNaN(summedLoss))
        //    {
        //        throw new Exception();
        //    }
        //}

        //var result = summedLoss / replays.Count;
        //if (double.IsNaN(result))
        //{
        //    throw new Exception();
        //}
        //return result;
    }

    public static double GetLossSingle(double ratingOwn, double ratingOpp, double actual, double clip)
    {
        double prediction = GetExpectationToWin(ratingOpp, ratingOwn, clip);

        var result = GetLoss(actual, prediction);
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }

    public static double GetLoss(double actual, double prediction)
    {
        var result = /*Math.Pow(*/actual - prediction/*, 2)*/;
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }

    public static double GetExpectationToWin(double ratingOwn, double ratingOpp, double clip)
    {
        var result = 1.0 / (1 + Math.Pow(10, (2 / clip) * (ratingOpp - ratingOwn)));
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }


    public static double GetLoss_d(double clip, ReplayData[] replays)
    {
        double summedLoss = 0;
        foreach (var replay in replays)
        {
            summedLoss += GetLossSingle_d(replay.WinnerTeamData.Mmr, replay.LoserTeamData.Mmr, replay.WinnerTeamData.ActualResult, clip);

            if (double.IsNaN(summedLoss))
            {
                throw new Exception();
            }
        }

        var result = summedLoss/* / replays.Length*/;
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }

    public static double GetLossSingle_d(double ratingOwn, double ratingOpp, double actual, double clip)
    {
        var prediction = GetExpectationToWin(ratingOwn, ratingOpp, clip);

        var result = GetLoss_d(actual, prediction) * GetExpectationToWin_d(ratingOwn, ratingOpp, clip);
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }

    public static double GetLoss_d(double actual, double prediction)
    {
        var result = /*2 * */(prediction - actual);
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }

    public static double GetExpectationToWin_d(double ratingOwn, double ratingOpp, double clip)
    {
        var powed = Math.Pow(10, (2 / clip) * (ratingOpp - ratingOwn));
        var top = 2 * Math.Log(10) * (ratingOpp - ratingOwn) * powed;
        var bottom = Math.Pow(clip, 2) * Math.Pow(powed + 1, 2);

        var result = top / bottom;
        if (double.IsNaN(result))
        {
            throw new Exception();
        }
        return result;
    }


    public static Dictionary<double, (double, int)> GetAccuracies(List<ReplayData> replayDatas)
    {
        int digits = 3;

        var totalExpectationsCount = new Dictionary<double, int>();
        var correctExpectationsCount = new Dictionary<double, int>();

        for (double i = 0.5; i <= 1; i += (1.0 / Math.Pow(10, digits)))
        {
            i = Math.Round(i, digits);
            if (i == 0.5)
            {
                continue;
            }

            totalExpectationsCount.Add(i, 0);
            correctExpectationsCount.Add(i, 0);
        }

        foreach (var replayData in replayDatas)
        {
            double expectation = Math.Round(replayData.WinnerTeamData.ExpectedResult, digits);
            if (expectation == 0.50)
            {
                continue;
            }

            if (expectation > 0.5)
            {
                correctExpectationsCount[expectation]++;

                totalExpectationsCount[expectation]++;
            }
            else
            {
                totalExpectationsCount[Math.Round(1 - expectation, digits)]++;
            }
        }

        var accuracies = new Dictionary<double, (double, int)>();
        for (double i = 0.5; i <= 1; i += (1.0 / Math.Pow(10, digits)))
        {
            i = Math.Round(i, digits);
            if (i == 0.5)
            {
                continue;
            }

            if (totalExpectationsCount[i] == 0)
            {
                continue;
            }

            double accuracy = correctExpectationsCount[i] / (double)totalExpectationsCount[i];
            accuracies.Add(i, (accuracy, totalExpectationsCount[i]));
        }

        return accuracies;
    }
}
