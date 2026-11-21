using System;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Kingdom.Settlements;

namespace Nozwock.Tweaks.Patches;

// References:
// https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/196
[HarmonyPatchCategory(nameof(Settings.VillageTeleportCircle))]
[HarmonyPatch]
public static class PatchVillageTeleportCircle
{
    const string TeleportationCircleGuid = "ea277e5efe3d4f459e74ea83d7a73d2f";

    internal static void OnToggle(bool enabled)
    {
        try
        {
            var bp = ResourcesLibrary.TryGetBlueprint<BlueprintSettlementBuilding>(TeleportationCircleGuid);
            bp?.MinLevel = enabled ? SettlementState.LevelType.Village : SettlementState.LevelType.Town;
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Error($"Failed to update Teleportation Circle blueprint: {ex}");
        }
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    static class BlueprintsCache_Init_Patch
    {
        static bool _initialized;

        static void Postfix()
        {
            if (_initialized) return;
            _initialized = true;

            OnToggle(true);
        }
    }
}