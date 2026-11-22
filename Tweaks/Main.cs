using System;
using HarmonyLib;
using Kingmaker.Utility;
using Nozwock.Tweaks.Patches;
using UnityEngine;
using UnityModManagerNet;

namespace Nozwock.Tweaks;

static class Main
{
    internal static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
    internal static Settings Settings { get; private set; } = null!;

    static Harmony? _harmony;

    public static void Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        ModEntry.OnToggle = OnToggle;
        ModEntry.OnGUI = OnGUI;
        ModEntry.OnSaveGUI = OnSaveGUI;
    }

    static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        if (value)
        {
            _harmony?.UnpatchSelf();
            _harmony = new(modEntry.Info.Id);
            _harmony.PatchAllUncategorized(typeof(Main).Assembly);
            OnSettingChanged();
        }
        else
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
        }

        return true;
    }

    static void OnSettingChanged()
    {
        try
        {
            _harmony?.SetPatchCategory(nameof(Settings.NoFriendlyFire), Settings.NoFriendlyFire);
            _harmony?.SetPatchCategory(nameof(Settings.RelicInfo), Settings.RelicInfo);

            _harmony?.SetPatchCategory(nameof(Settings.InfiniteArmyMovement), Settings.InfiniteArmyMovement);
            _harmony?.SetPatchCategory(nameof(Settings.VillageTeleportCircle), Settings.VillageTeleportCircle);
            _harmony?.SetPatchCategory(nameof(Settings.NoTeleportCooldown), Settings.NoTeleportCooldown);

            _harmony?.SetPatchCategory(nameof(Settings.DisableWeatherVfx), Settings.DisableWeatherVfx);
            _harmony?.SetPatchCategory(nameof(Settings.DisableAmbientParticles), Settings.DisableAmbientParticles);
            _harmony?.SetPatchCategory(nameof(Settings.DisableNonEssentialCombatVfx), Settings.DisableNonEssentialCombatVfx);
            _harmony?.SetPatchCategory(nameof(Settings.DisableCosmeticDecals), Settings.DisableCosmeticDecals);

            _harmony?.SetPatchCategory(nameof(Settings.DisableClothPhysics), Settings.DisableClothPhysics);
            _harmony?.SetPatchCategory(nameof(Settings.HideAllCapes), Settings.HideAllCapes);

            BuildModeUtility.Data?.CompensateCharacterAtlasMips = Settings.FixAtlasDoubleDownscale;
            PatchDisableClothPhysics.OnUpdate();
            PatchHideAllCapes.OnUpdate();
        }
        catch (Exception ex)
        {
            ModEntry.Logger.Error($"Failed to apply settings: {ex}");
        }
    }

    static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        static bool Toggle(bool value, string text, string? description = null)
        {
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = Color.gray;
            var toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                alignment = TextAnchor.MiddleLeft,
            };

            GUILayout.BeginHorizontal();

            var ret = GUILayout.Toggle(value, text, toggleStyle);
            if (!string.IsNullOrWhiteSpace(description))
            {
                GUILayout.Label(description, labelStyle);
            }
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            return ret;
        }

        static void CategoryToggle(
            ref bool setting,
            string category,
            string text,
            string? description = null,
            Action? onToggled = null
        )
        {
            var value = Toggle(setting, text, description);
            if (value != setting)
            {
                setting = value;
                _harmony?.SetPatchCategory(category, value);
                onToggled?.Invoke();
            }
        }

        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Gameplay</b>");
        {
            CategoryToggle(
                ref Settings.NoFriendlyFire,
                nameof(Settings.NoFriendlyFire),
                "Disable Friendly Fire",
                "(Spells and abilities will no longer damage allies.)"
            );
            CategoryToggle(
                ref Settings.RelicInfo,
                nameof(Settings.RelicInfo),
                "Show More Relic Info",
                "(Displays intermediate choices and final relic item rewards on relic decrees and events.)"
            );
        }
        GUILayout.Space(4);
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Cheats</b>");
        {
            CategoryToggle(
                ref Settings.InfiniteArmyMovement,
                nameof(Settings.InfiniteArmyMovement),
                "Infinite Army Movement Points"
            );
            CategoryToggle(
                ref Settings.VillageTeleportCircle,
                nameof(Settings.VillageTeleportCircle),
                "Disable Teleportation Circle Restrictions",
                "(Allow building Teleportation Circles on basic forts.)",
                () => PatchVillageTeleportCircle.OnToggle(Settings.VillageTeleportCircle)
            );
            CategoryToggle(
                ref Settings.NoTeleportCooldown,
                nameof(Settings.NoTeleportCooldown),
                "Disable Crusade Teleport Cooldown",
                "(Removes the 1-day cooldown when using the Party Teleportation spell on the world map.)"
            );
        }
        GUILayout.Space(4);
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Graphics</b>");
        {
            CategoryToggle(
                ref Settings.DisableWeatherVfx,
                nameof(Settings.DisableWeatherVfx),
                "Disable Weather VFX",
                "(Falling snow, rain, fog particles, thunder flashes, etc.)"
            );
            CategoryToggle(
                ref Settings.DisableAmbientParticles,
                nameof(Settings.DisableAmbientParticles),
                "Disable Ambient Particles",
                "(Chimney smoke, local scene dust, falling particles, etc.)"
            );
            CategoryToggle(
                ref Settings.DisableNonEssentialCombatVfx,
                nameof(Settings.DisableNonEssentialCombatVfx),
                "Disable Non-Essential Combat VFX",
                "(Blood puddles, minor cosmetic impact sparks, etc.)"
            );
            CategoryToggle(
                ref Settings.DisableCosmeticDecals,
                nameof(Settings.DisableCosmeticDecals),
                "Disable Cosmetic Ground Decals",
                "(Blood splatters, footprint decals, etc.)"
            );

            CategoryToggle(
                ref Settings.DisableClothPhysics,
                nameof(Settings.DisableClothPhysics),
                "Disable Cloth Physics",
                onToggled: PatchDisableClothPhysics.OnUpdate
            );

            CategoryToggle(
                ref Settings.HideAllCapes,
                nameof(Settings.HideAllCapes),
                "Hide All Capes",
                onToggled: PatchHideAllCapes.OnUpdate
            );

            Settings.FixAtlasDoubleDownscale = Toggle(
                Settings.FixAtlasDoubleDownscale,
                "Fix Character Atlas Double-Downscaling",
                "(Prevents blurry 128x128 character texture on Low.)"
            );
        }
        BuildModeUtility.Data?.CompensateCharacterAtlasMips = Settings.FixAtlasDoubleDownscale;

        GUILayout.Space(4);
        GUILayout.EndVertical();
    }

    static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Save(modEntry);
        OnSettingChanged();
    }
}
