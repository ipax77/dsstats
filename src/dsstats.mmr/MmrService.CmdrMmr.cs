﻿using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public partial class MmrService
{
    private static double GetCommandersComboMmr(ReplayData replayData, TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (replayData.IsInvalid)
        {
            return 1.0;
        }

        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].Race;

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = antiSynergyPlayerCmdr }];

                if (playerIndex == antiSynergyPlayerIndex)
                {
                    antiSynergySum += (MmrOptions.ownMatchupPercentage * antiSynergy.AntiSynergyMmr);
                }
                else
                {
                    antiSynergySum += (MmrOptions.matesMatchupsPercentage * antiSynergy.AntiSynergyMmr);
                }
            }

            commandersComboMMRSum +=
                (MmrOptions.antiSynergyPercentage * antiSynergySum)
                + (MmrOptions.synergyPercentage * synergySum);
        }

        return commandersComboMMRSum / 3;
    }

    private static void SetCommandersComboMmr(ReplayData replayData, TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (replayData.IsInvalid)
        {
            return;
        }

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].Race;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.Players[playerIndex].Deltas.CommanderMmr / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = antiSynergyPlayerCmdr }];

                antiSynergy.AntiSynergyMmr += teamData.Players[playerIndex].Deltas.CommanderMmr;
            }
        }
    }
}