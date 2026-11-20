using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib;

static class HarmonyExtensions
{
    record class AttachedData(Dictionary<string, bool> EnabledCategories);

    static readonly ConditionalWeakTable<Harmony, AttachedData> _attachedData = new();

    extension(Harmony self)
    {
        AttachedData AttachedData
        {
            get
            {
                if (!_attachedData.TryGetValue(self, out var data))
                {
                    data = new AttachedData([]);
                    _attachedData.Add(self, data);
                }
                return data;
            }
        }
    }

    public static void UnpatchSelf(this Harmony self)
    {
        self.UnpatchAll(self.Id);
        self.AttachedData.EnabledCategories.Clear();
    }

    public static void SetPatchCategory(this Harmony self, string category, bool enabled)
    {
        var prevEnabled = !enabled;
        if (self.AttachedData.EnabledCategories.TryGetValue(category, out var value))
            prevEnabled = value;
        if (prevEnabled == enabled)
            return;

        var assembly = Assembly.GetCallingAssembly();
        if (enabled)
        {
            self.PatchCategory(assembly, category);
        }
        else
        {
            self.UnpatchCategory(assembly, category);
        }
        self.AttachedData.EnabledCategories[category] = enabled;
    }
}