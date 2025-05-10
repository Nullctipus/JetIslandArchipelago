using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace JetIslandArchipelago.Patches;


[HarmonyPatch(typeof(Miniboss))]
public class MiniBossPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void PrefixMiniBossUpdate(Miniboss __instance)
    {
        if (__instance.health > 0 || __instance.weakpointExplodeGameObject.activeInHierarchy) return;

        __instance.weakpointExplodeGameObject.SetActive(true);
        _ = ArchipelagoWrapper.Instance.CheckMiniBoss(__instance.minibossIndex-1);
        if(__instance.unlockCrystalGameObject)
            Object.Destroy(__instance.unlockCrystalGameObject);
    }
}