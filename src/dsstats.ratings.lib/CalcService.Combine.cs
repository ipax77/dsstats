using pax.dsstats.shared.Calc;

namespace dsstats.ratings.lib;

public partial class CalcService
{

    public List<CalcDto> CombineCalcDtos(List<CalcDto> dsstatsCalcDtos,
                                                List<CalcDto> sc2ArcadeCalcDtos,
                                                HashSet<int> processedSc2ArcadeReplayIds)
    {
        List<CalcDto> combinedCalcDtos = new();

        var dsstatsDic = GenerateHashDic(dsstatsCalcDtos);
        var sc2arcadeDic = GenerateHashDic(sc2ArcadeCalcDtos);

        foreach (var ent in dsstatsDic)
        {
            foreach (var calcDto in ent.Value)
            {
                if (!calcDto.TournamentEdition)
                {
                    if (sc2arcadeDic.TryGetValue(ent.Key, out var calcDtos))
                    {
                        foreach (var sc2ArcadeCalcDto in calcDtos.ToArray())
                        {
                            if (IsMatchReasonable(calcDto, sc2ArcadeCalcDto))
                            {
                                calcDto.Sc2ArcadeReplayId = sc2ArcadeCalcDto.Sc2ArcadeReplayId;
                                calcDtos.Remove(sc2ArcadeCalcDto);
                                break;
                            }
                        }
                    }
                }
                combinedCalcDtos.Add(calcDto);
            }
        }

        combinedCalcDtos.AddRange(sc2arcadeDic.SelectMany(s => s.Value)
            .Where(x => !processedSc2ArcadeReplayIds.Contains(x.Sc2ArcadeReplayId)));

        return combinedCalcDtos
            .OrderBy(o => o.GameTime)
            .ThenBy(o => o.DsstatsReplayId)
            .ThenBy(o => o.Sc2ArcadeReplayId).ToList();
    }

    private static bool IsMatchReasonable(CalcDto dsstatsCalcDto, CalcDto sc2arcadeCalcDto)
    {
        var gameTimeDiff = Math.Abs((dsstatsCalcDto.GameTime - sc2arcadeCalcDto.GameTime).TotalSeconds);
        //var durationDiff = Math.Abs(dsstatsCalcDto.Duration - sc2arcadeCalcDto.Duration);

        //var durationPerDiff = durationDiff / (double)Math.Max(dsstatsCalcDto.Duration, sc2arcadeCalcDto.Duration);

        // if (gameTimeDiff < 86400 && durationPerDiff < 0.2)
        if (gameTimeDiff < 86400)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static Dictionary<string, List<CalcDto>> GenerateHashDic(List<CalcDto> calcDtos)
    {
        Dictionary<string, List<CalcDto>> hashDic = new();

        for (int i = 0; i < calcDtos.Count; i++)
        {
            var calcDto = calcDtos[i];

            var key = string.Join('|', calcDto.Players
                .OrderBy(o => o.Team)
                .ThenBy(o => o.ProfileId)
                .Select(s => s.ProfileId.ToString()));
                // .Select(s => $"{s.ProfileId},{s.RegionId},{s.RealmId}"));

            var gameMode = calcDto.GameMode switch
            {
                3 => "Cmdr",
                4 => "Cmdr",
                7 => "Std",
                _ => ""
            };

            key = gameMode + key;

            if (!hashDic.TryGetValue(key, out var dtos))
            {
                hashDic[key] = new();
            }
            hashDic[key].Add(calcDto);
        }
        return hashDic;
    }
}
