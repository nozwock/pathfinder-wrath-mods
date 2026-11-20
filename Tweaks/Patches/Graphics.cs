using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.Decals;
using Kingmaker.Visual.Particles.ForcedCulling;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using Kingmaker.Visual.WeatherSystem;
using Owlcat.Runtime.Core.Physics.PositionBasedDynamics.Scene;
using Owlcat.Runtime.Visual.Effects.WeatherSystem;
using UnityEngine;

namespace Nozwock.Tweaks.Patches;

[HarmonyPatchCategory(nameof(Settings.DisableWeatherVfx))]
[HarmonyPatch]
static class PatchDisableWeatherVfx
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WeatherEffectController), nameof(WeatherEffectController.Update))]
    static void WeatherEffectController_Update_Prefix(ref float weatherIntensity)
    {
        weatherIntensity = 0f;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WeatherThunderController), nameof(WeatherThunderController.CanSpawn))]
    static bool WeatherThunderController_CanSpawn_Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPatch(typeof(WeatherCameraShakeController), nameof(WeatherCameraShakeController.Update))]
    static bool WeatherCameraShakeController_Update_Prefix() => false;
}

[HarmonyPatchCategory(nameof(Settings.DisableAmbientParticles))]
[HarmonyPatch]
static class PatchDisableAmbientParticles
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ForcedCullingRadius), nameof(ForcedCullingRadius.SetVisible))]
    static bool ForcedCullingRadius_SetVisible_Prefix(
        ForcedCullingRadius __instance,
        bool visible,
        ref bool? ___m_IsVisible)
    {
        if (visible)
        {
            ___m_IsVisible = false;
            foreach (var it in __instance.m_ParticleSystems)
            {
                if (it != null && it.isPlaying)
                {
                    it.Pause();
                }
            }
            return false;
        }
        return true;
    }
}

[HarmonyPatchCategory(nameof(Settings.DisableNonEssentialCombatVfx))]
[HarmonyPatch]
static class PatchDisableNonEssentialCombatVfx
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(FxSpawnController), nameof(FxSpawnController.Spawn))]
    static bool FxSpawnController_Spawn_Prefix(FxPriority priority, ref IFxHandle __result)
    {
        if (priority == FxPriority.NotImportant)
        {
            var handle = new FxHandle(null, FxPriority.NotImportant, 0);
            handle.HandleDestroy();
            __result = handle;
            return false;
        }
        return true;
    }
}

// Suppresses cosmetic ground decals.
[HarmonyPatchCategory(nameof(Settings.DisableCosmeticDecals))]
[HarmonyPatch]
static class PatchDisableCosmeticDecals
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ScreenSpaceDecalRenderer), nameof(ScreenSpaceDecalRenderer.DoCullingAndSort))]
    static void ScreenSpaceDecalRenderer_DoCullingAndSort_Postfix(List<ScreenSpaceDecal> ___m_VisibleDecals)
    {
        ___m_VisibleDecals?.RemoveAll(d => d != null && d.Type == ScreenSpaceDecal.DecalType.Default);
    }
}

// PDB - Position-Based Dynamics
// PDBMeshBody seemingly handles cape/cloth meshes while PDBSkinnedBody is left untouched which handles
// wings/tails/horns so that Arueshalae renders correctly and not have visible wings when she isn't supposed to.
[HarmonyPatchCategory(nameof(Settings.DisableClothPhysics))]
[HarmonyPatch]
static class PatchDisableClothPhysics
{
    internal static void OnUpdate()
    {
        try
        {
            var disable = Main.Settings.DisableClothPhysics;
            var meshBodies = UnityEngine.Object.FindObjectsOfType<PBDMeshBody>();
            foreach (var body in meshBodies)
            {
                body?.enabled = !disable;
            }
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Error($"Error updating cloth physics state: {ex}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PBDBodyBase), nameof(PBDBodyBase.OnEnable))]
    static bool PBDBodyBase_OnEnable_Prefix(PBDBodyBase __instance)
    {
        if (__instance is PBDMeshBody)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PBDMeshBody), nameof(PBDMeshBody.InitializeInternal))]
    static bool PBDMeshBody_InitializeInternal_Prefix() => false;

    // Ignore cloak collisions.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.SetupCloakColliders))]
    static bool Character_SetupCloakColliders_Prefix(GameObject cloakObject, List<PBDColliderBase>? additionalColliders)
    {
        if (cloakObject != null)
        {
            var meshBodies = cloakObject.GetComponentsInChildren<PBDMeshBody>(true);
            for (int i = 0; i < meshBodies.Length; i++)
            {
                meshBodies[i].enabled = false;
            }
        }
        return false;
    }
}

[HarmonyPatchCategory(nameof(Settings.HideAllCapes))]
[HarmonyPatch]
static class PatchHideAllCapes
{
    internal static void OnUpdate()
    {
        try
        {
            var characters = UnityEngine.Object.FindObjectsOfType<Character>();
            foreach (var character in characters)
            {
                character?.IsDirty = true;
            }
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Error($"Error updating cape visibility: {ex}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.HideCloakMode), MethodType.Getter)]
    static bool Character_HideCloakMode_get_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
