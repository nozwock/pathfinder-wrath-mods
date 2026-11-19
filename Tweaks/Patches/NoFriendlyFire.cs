using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;

namespace Nozwock.Tweaks.Patches;

// References:
// https://github.com/xADDBx/ToyBox-Wrath/blob/c4473258b2068d9eb0a577f490f62292fa962e2b/ToyBox/Classes/MonkeyPatchin/BagOfPatches/Combat/NoFriendlyFire.cs
//
// The problem with the implementation of the above patch is that:
// 1. It only works for party members and not event allies like soldiers, etc.
// 2. The spells don't ignore the allies completely but instead makes it so they always succeed the saving throws which
//    results in 0 damage or saving throws success messaged being shown.
[HarmonyPatch]
static class PatchNoFriendlyFire
{
    static bool IsFriendly(UnitEntityData? caster) => caster != null && !caster.IsPlayersEnemy;

    // Filter target selections to only be the enemies of the caster for non-persistent AOE abilities.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbilityTargetsAround), nameof(AbilityTargetsAround.Select))]
    static void AbilityTargetsAround_Select_Postfix(
        ref IEnumerable<TargetWrapper> __result,
        AbilityTargetsAround __instance,
        ConditionsChecker ___m_Condition,
        AbilityExecutionContext context
    )
    {
        var caster = context?.MaybeCaster;
        if (!IsFriendly(caster))
            return;
        if (
            // Ignore beneficial spells like healing spells.
            context!.AbilityBlueprint.NotOffensive
            || context.AbilityBlueprint.EffectOnAlly == AbilityEffectOnUnit.Helpful
        )
            return;
        if (context.AbilityBlueprint.GetComponent<AbilityUseOnRest>() != null)
            return;

        if (__instance.Targets == TargetType.Any && !___m_Condition.HasIsAllyCondition())
        {
            __result = __result.Where(t => t.Unit == null || caster!.IsEnemy(t.Unit));
        }
    }

    // Enemy-only filter for Conjuration persistent ground type like spells.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AreaEffectEntityData), nameof(AreaEffectEntityData.IsSuitableTargetType))]
    static bool AreaEffectEntityData_IsSuitableTargetType_Prefix(
        AreaEffectEntityData __instance,
        UnitEntityData unit,
        ref bool __result
    )
    {
        var caster = __instance.Context?.MaybeCaster;
        if (!IsFriendly(caster))
            return true;

        if (__instance.Blueprint.CanTargetAllies && __instance.Blueprint.CanTargetEnemies)
        {
            var sourceBp = __instance.Context?.SourceAbilityContext?.AbilityBlueprint;
            if (
                sourceBp != null
                && (sourceBp.NotOffensive || sourceBp.EffectOnAlly == AbilityEffectOnUnit.Helpful)
            )
                return true;

            foreach (var elem in __instance.Blueprint.ElementsArray)
            {
                if (elem is ContextConditionIsAlly)
                    return true;
            }

            if (!caster!.IsEnemy(unit))
            {
                __result = false;
                return false;
            }
        }
        return true;
    }

    // Enemy-only filter for abilities using projectile cones and lines.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbilityDeliverProjectile), nameof(AbilityDeliverProjectile.CanTargetUnit))]
    static bool AbilityDeliverProjectile_CanTargetUnit_Prefix(
        AbilityDeliverProjectile __instance,
        AbilityExecutionContext context,
        UnitEntityData unit,
        ref bool __result
    )
    {
        var caster = context?.MaybeCaster;
        if (!IsFriendly(caster))
            return true;

        if (!caster!.IsEnemy(unit))
        {
            var bp = context?.Ability?.Blueprint;
            if (bp != null)
            {
                if (bp.NotOffensive || bp.EffectOnAlly == AbilityEffectOnUnit.Helpful)
                    return true;

                bool affectsBoth =
                    (bp.CanTargetFriends && bp.CanTargetEnemies) || bp.CanTargetPoint;
                if (affectsBoth && !__instance.HasIsAllyConditions)
                {
                    __result = false;
                    return false;
                }
            }
        }
        return true;
    }

    // Enemy-only filter for abilities that chain (for e.g. Chain Lightning).
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbilityDeliverChain), nameof(AbilityDeliverChain.CheckTarget))]
    static bool AbilityDeliverChain_CheckTarget_Prefix(
        AbilityExecutionContext context,
        UnitEntityData unit,
        ref bool __result,
        TargetType ___m_TargetType,
        ConditionsChecker ___m_Condition
    )
    {
        var caster = context?.MaybeCaster;
        if (!IsFriendly(caster))
            return true;

        if (
            ___m_TargetType == TargetType.Any
            && !___m_Condition.HasIsAllyCondition()
            && !caster!.IsEnemy(unit)
        )
        {
            __result = false;
            return false;
        }
        return true;
    }
    // Enemy-only filter for abilities that ricochet.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbilityDeliverRicochet), nameof(AbilityDeliverRicochet.CheckTarget))]
    static bool AbilityDeliverRicochet_CheckTarget_Prefix(
        AbilityExecutionContext context,
        UnitEntityData unit,
        ref bool __result,
        TargetType ___m_TargetType,
        ConditionsChecker ___m_TargetCondition
    )
    {
        if (!AbilityDeliverChain_CheckTarget_Prefix(context, unit, ref __result, ___m_TargetType, ___m_TargetCondition))
        {
            return false;
        }
        return true;
    }

    // Prevents targeting UI preview from showing allies as being affected by abilities.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.WouldCurrentlyTarget))]
    static bool AbilityData_WouldCurrentlyTarget_Prefix(
        AbilityData __instance,
        UnitEntityData unit,
        ref bool __result
    )
    {
        var caster = __instance.Caster?.Unit;
        if (!IsFriendly(caster))
            return true;

        if (!caster!.IsEnemy(unit))
        {
            var bp = __instance.Blueprint;
            if (bp != null && !bp.NotOffensive && bp.EffectOnAlly != AbilityEffectOnUnit.Helpful)
            {
                if ((bp.CanTargetFriends && bp.CanTargetEnemies) || bp.CanTargetPoint)
                {
                    var deliver = __instance.GetDeliverBlueprint();
                    var deliverProj = deliver?.GetComponent<AbilityDeliverProjectile>();
                    if (deliverProj != null && deliverProj.HasIsAllyConditions)
                        return true;

                    var targetsAround = deliver?.GetComponent<AbilityTargetsAround>();
                    if (targetsAround != null && targetsAround.Targets != TargetType.Any)
                        return true;

                    __result = false;
                    return false;
                }
            }
        }
        return true;
    }

    // Enemy-only filter for Crusade tactical mode abilities.
    #region CrusadeTacticalMode
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbilityTargetsAroundOnGrid), nameof(AbilityTargetsAroundOnGrid.Select))]
    static void AbilityTargetsAroundOnGrid_Select_Postfix(
        ref IEnumerable<TargetWrapper> __result,
        AbilityTargetsAroundOnGrid __instance,
        ConditionsChecker ___m_Condition,
        AbilityExecutionContext context
    )
    {
        var caster = context?.MaybeCaster;
        if (!IsFriendly(caster))
            return;

        if (__instance.Targets == TargetType.Any && !___m_Condition.HasIsAllyCondition())
        {
            __result = __result.Where(t => t.Unit == null || caster!.IsEnemy(t.Unit));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbilityAffectLineOnGrid), nameof(AbilityAffectLineOnGrid.Select))]
    static void AbilityAffectLineOnGrid_Select_Postfix(
        ref IEnumerable<TargetWrapper> __result,
        AbilityAffectLineOnGrid __instance,
        ConditionsChecker ___m_Condition,
        TargetType ___m_TargetType,
        AbilityExecutionContext context
    )
    {
        var caster = context?.MaybeCaster;
        if (!IsFriendly(caster))
            return;

        if (___m_TargetType == TargetType.Any && !___m_Condition.HasIsAllyCondition())
        {
            __result = __result.Where(t => t.Unit == null || caster!.IsEnemy(t.Unit));
        }
    }
    #endregion
}