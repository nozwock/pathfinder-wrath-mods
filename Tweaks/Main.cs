using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Armies.Components;
using Kingmaker.Blueprints.Root;
using Kingmaker.Globalmap.State;
using Kingmaker.Kingdom;
using UnityModManagerNet;

namespace Nozwock.Tweaks;

static class Main
{
    internal static UnityModManager.ModEntry ModEntry { get; private set; } = null!;

    static Harmony? _harmony;

    public static void Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        ModEntry.OnToggle = OnToggle;
    }

    static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        if (value)
        {
            _harmony?.UnpatchAll();
            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll();
        }
        else
        {
            _harmony?.UnpatchAll();
            _harmony = null;
        }

        return true;
    }
}

[HarmonyPatch(typeof(GlobalMapArmyState))]
static class PatchInfiniteArmyMovement
{
    /// <summary>
    /// See <see cref="GlobalMapArmyState.RestoreMovementPoints"/>.
    /// </summary>
    static float MaxArmyMovementPoints(GlobalMapArmyState army)
    {
        var (max, daily) = army.Data.GetArmyBonusSkills().AsEnumerable()
            .Concat(KingdomState.Founded
                ? KingdomState.Instance.ActiveBuffs.SelectFactComponents<ArmyGlobalMapMovementBonus>()
                : [])
            .Aggregate((0, 0), (acc, it) => (acc.Item1 + it.MaxMovementPoints, acc.Item2 + it.DailyMovementPoints));
        return Math.Min(BlueprintRoot.Instance.GlobalMap.MaxArmyMovementPoints + max, daily);
    }

    // [HarmonyPrefix]
    // [HarmonyPatch(nameof(GlobalMapArmyState.MovementPoints), MethodType.Getter)]
    // static bool MovementPoints_get_Prefix(GlobalMapArmyState __instance, float __result)
    // {
    //     __result = MaxArmyMovementPoints(__instance);
    //     return false;
    // }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GlobalMapArmyState.SpendMovementPoints))]
    static bool SpendMovementPoints_Prefix(GlobalMapArmyState __instance)
    {
        __instance.RestoreMovementPoints(BlueprintRoot.Instance.GlobalMap.StartArmyMovementPoints);
        return false;
    }
}