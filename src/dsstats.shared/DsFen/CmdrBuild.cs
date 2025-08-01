﻿using System.Collections.Frozen;

namespace dsstats.shared.DsFen;

public abstract class CmdrBuild
{
    protected Dictionary<string, BuildOption> UnitMap = [];
    protected Dictionary<string, BuildOption> UpgradeMap = [];
    protected Dictionary<string, BuildOption> AbilityMap = [];
    public Dictionary<string, bool> activeUnits = [];

    protected void CreateActiveUnits()
    {
        activeUnits = UnitMap.Where(x => x.Value.RequiresToggle).ToDictionary(k => k.Key, v => v.Value.IsActive);
    }

    public virtual BuildOption? GetUnitBuildOption(string unitName)
    {
        return UnitMap.TryGetValue(unitName, out var buildOption)
            ? buildOption
            : null;
    }

    public virtual char? GetUpgradeChar(string upgradeName)
    {
        return UpgradeMap.TryGetValue(upgradeName, out var buildOption)
            ? buildOption.Key
            : null;
    }

    public virtual BuildOption? GetAbilityBuildOption(string abilityName)
    {
        return AbilityMap.TryGetValue(abilityName, out var buildOption)
            ? buildOption
            : null;
    }

    public string? GetUpgradeName(char key)
    {
        foreach (var kv in UpgradeMap)
        {
            if (char.ToLower(kv.Value.Key) == char.ToLower(key))
            {
                return kv.Key;
            }
        }
        return null;
    }

    public string? GetAbilityName(char key)
    {
        foreach (var kv in AbilityMap)
        {
            if (char.ToLower(kv.Value.Key) == char.ToLower(key))
            {
                return kv.Key;
            }
        }
        return null;
    }

    public string? GetUnitNameFromKey(char key, bool isAir)
    {
        bool requiresToggle = char.IsUpper(key);
        key = char.ToLower(key);
        foreach (var kv in UnitMap)
        {
            var opt = kv.Value;
            if (opt.Key != key)
                continue;

            if (opt.IsAir != isAir)
                continue;

            if (requiresToggle && opt.IsActive)
                continue;

            return kv.Key;
        }

        return null;
    }

    public IReadOnlyDictionary<string, BuildOption> GetUnitMap()
    {
        return UnitMap;
    }
}

public sealed record BuildOption(char Key, int UnitSize = 1, bool IsAir = false, bool RequiresToggle = false, bool IsActive = false, bool IsAbility = false);

public static class CmdrBuildFactory
{
    private static readonly HashSet<Commander> SupportedCommanders = new()
    {
        Commander.Protoss,
        Commander.Terran,
        Commander.Zerg,
        Commander.Abathur,
        Commander.Alarak,
    };

    public static CmdrBuild? Create(Commander commander)
    {
        return commander switch
        {
            Commander.Protoss => new ProtossBuild(),
            Commander.Terran => new TerranBuild(),
            Commander.Zerg => new ZergBuild(),

            Commander.Abathur => new AbathurBuild(),
            Commander.Alarak => new AlarakBuild(),
            _ => null
        };
    }

    public static bool IsSupported(Commander commander)
    {
        return SupportedCommanders.Contains(commander);
    }
}