using HarmonyLib;
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
            _harmony?.UnpatchAll(modEntry.Info.Id);
            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll();
        }
        else
        {
            _harmony?.UnpatchAll(modEntry.Info.Id);
            _harmony = null;
        }

        return true;
    }
}
