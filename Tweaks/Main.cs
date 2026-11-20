using System;
using HarmonyLib;
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
            _harmony?.SetPatchCategory(nameof(Settings.InfiniteArmyMovement), Settings.InfiniteArmyMovement);
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
        }
        GUILayout.Space(4);
        GUILayout.EndVertical();
    }

    static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Save(modEntry);
        OnSettingChanged();
    }
}
