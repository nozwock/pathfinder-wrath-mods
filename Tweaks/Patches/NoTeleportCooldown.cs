using System;
using HarmonyLib;
using Kingmaker.Crusade.GlobalMagic;
using Kingmaker.Crusade.GlobalMagic.Actions;
using Kingmaker.Crusade.GlobalMagic.SpellsManager;

namespace Nozwock.Tweaks.Patches;

[HarmonyPatchCategory(nameof(Settings.NoTeleportCooldown))]
public static class PatchNoTeleportCooldown
{
    const string PartyTeleportSpellGuid = "8accb3511e0b4eeb822c5867a3dde1e1";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ManuallySetGlobalSpellCooldown), nameof(ManuallySetGlobalSpellCooldown.RunAction))]
    static bool ManuallySetGlobalSpellCooldown_RunAction_Prefix(ManuallySetGlobalSpellCooldown __instance)
    {
        if (__instance.m_Spell?.Guid.ToString() == PartyTeleportSpellGuid)
        {
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpellState), nameof(SpellState.Cooldown), MethodType.Getter)]
    static void SpellState_Cooldown_get_Postfix(SpellState __instance, ref float __result)
    {
        if (__instance.BlueprintGuid.ToString() == PartyTeleportSpellGuid)
        {
            __result = 0f;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpellState), nameof(SpellState.CooldownTimeSpan), MethodType.Getter)]
    static void SpellState_CooldownTimeSpan_get_Postfix(SpellState __instance, ref TimeSpan __result)
    {
        if (__instance.BlueprintGuid.ToString() == PartyTeleportSpellGuid)
        {
            __result = TimeSpan.Zero;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlueprintGlobalMagicSpell), nameof(BlueprintGlobalMagicSpell.Cooldown), MethodType.Getter)]
    static void BlueprintGlobalMagicSpell_Cooldown_get_Postfix(BlueprintGlobalMagicSpell __instance, ref int __result)
    {
        if (__instance.AssetGuid.ToString() == PartyTeleportSpellGuid)
        {
            __result = 0;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlueprintGlobalMagicSpell), nameof(BlueprintGlobalMagicSpell.GetCooldownTimeSpan))]
    static void BlueprintGlobalMagicSpell_GetCooldownTimeSpan_Postfix(BlueprintGlobalMagicSpell __instance, ref TimeSpan __result)
    {
        if (__instance.AssetGuid.ToString() == PartyTeleportSpellGuid)
        {
            __result = TimeSpan.Zero;
        }
    }
}
