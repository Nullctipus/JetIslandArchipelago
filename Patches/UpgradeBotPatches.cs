using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace JetIslandArchipelago.Patches;
[HarmonyPatch(typeof(UpgradeBot))]
public class UpgradeBotPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void PrefixUpgradeBotUpdate(UpgradeBot __instance, ref bool ___alreadySpawnedPickup,
        bool ___willLocalBotDropPickup, int ___locationIndex)
    {
        if (__instance.health > 0 || ___alreadySpawnedPickup || !___willLocalBotDropPickup ||
            Vector3.Distance(__instance.transform.position, PlayerBody.localPlayer.body.torsoParent.position) >=
            __instance.distToDestroy) return;

        ___alreadySpawnedPickup = true;
        _ = ArchipelagoWrapper.Instance.CheckUpgradeBot(___locationIndex);
        PlayerBody.localPlayer.stats.gottenUpgradeBots[___locationIndex] = true;
    }
}