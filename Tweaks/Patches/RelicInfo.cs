using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.Kingdom.Actions;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.UI;
using Kingmaker.TextTools;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.MVVM._ConsoleView.Kingdom.Events.Details;
using Kingmaker.UI.MVVM._VM.Kingdom.Events.Details;
using Kingmaker.Utility;

namespace Nozwock.Tweaks.Patches;

// References:
// https://github.com/cabarius/ToyBox/blob/6582da76f7f613600c1d7b986b15fdc5862f43f3/ToyBox/classes/MonkeyPatchin/PreviewManagerWrath.cs#L507-L556
//
// Adds support for showing relic info on event cards besides just decree cards.
[HarmonyPatchCategory(nameof(Settings.RelicInfo))]
static class PatchRelicInfo
{
    static BlueprintCrusadeEvent? GetStartEvent(BlueprintKingdomProject? project) =>
        project?.Solutions.Entries?
            .SelectMany(it => it.Resolutions ?? [])
            .SelectMany(it => it.Actions?.Actions ?? [])
            .OfType<KingdomActionStartEvent>()
            .Select(it => it.Event as BlueprintCrusadeEvent)
            .FirstOrDefault(it => it != null);

    static IEnumerable<BlueprintItem> EventChoiceItems(EventSolution choice) =>
        choice.m_SuccessEffects?.Actions?.OfType<AddItemToPlayer>()
            .Select(it => it.ItemToGive)
            .Where(it => it != null)
            .Distinct() ?? [];

    static IEnumerable<BlueprintUnlockableFlag> EventChoiceFlags(EventSolution choice) =>
        choice.m_SuccessEffects?.Actions?.OfType<UnlockFlag>()
            .Select(it => it.flag)
            .Where(it => it != null)
            .Distinct() ?? [];

    static IEnumerable<BlueprintUnlockableFlag> ConditionFlags(ConditionsChecker? checker) =>
        checker?.Conditions?.OfType<FlagUnlocked>()
            .Select(it => it.ConditionFlag)
            .Where(it => it != null) ?? [];

    static string BlueprintItemName(BlueprintItem item)
    {
        var name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : item.name;
        return $"<b>[{name.Trim()}]</b>";
    }

    static string EventChoiceText(EventSolution choice)
    {
        var text = choice.SolutionText?.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        try
        {
            text = TextTemplateEngine.Process(text);
        }
        catch { }
        return text?.Trim() ?? string.Empty;
    }

    static string CustomEventChoiceText(EventSolution choice)
    {
        var text = EventChoiceText(choice);
        var items = EventChoiceItems(choice).ToList();
        var itemNames = items.Count > 0
            ? string.Join(", ", items.Select(BlueprintItemName))
            : string.Empty;

        if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(itemNames))
        {
            return items.Count == 1 && string.Equals(text, items[0].Name, StringComparison.OrdinalIgnoreCase)
                ? itemNames
                : $"{text} — {itemNames}";
        }
        else
        {
            return !string.IsNullOrEmpty(itemNames) ? itemNames : text;
        }
    }

    static IEnumerable<EventSolution> GetFinalSolutions(
        BlueprintCrusadeEvent crusadeEvent,
        IEnumerable<BlueprintUnlockableFlag>? allowedFlags)
    {
        var anyAvailable = crusadeEvent.EventSolutions.Any(it => it.IsAvail);
        return crusadeEvent.EventSolutions.Where(choice =>
        {
            if (anyAvailable)
                return choice.IsAvail;

            var requiredFlags = ConditionFlags(choice.m_AvailConditions).ToList();
            return requiredFlags.Empty() || (allowedFlags != null && allowedFlags.Any(requiredFlags.Contains));
        });
    }

    static string? GetRelicInfo(BlueprintKingdomEventBase? blueprint)
    {
        if (blueprint == null)
            return null;

        var allRelicProjects = (
            from it in Game.Instance.BlueprintRoot.Kingdom.KingdomProjectEvents
            where it.Get().ProjectType == KingdomProjectType.Relics
            select it.Get()
        ).ToList();

        BlueprintCrusadeEvent? crusadeEvent = null;
        BlueprintKingdomProject? crusadeProject = null;

        if (blueprint is BlueprintKingdomProject _project)
        {
            // Decree cards
            crusadeProject = _project;
            if (crusadeProject.ProjectType != KingdomProjectType.Relics)
                return null;
            crusadeEvent = GetStartEvent(crusadeProject);
        }
        else if (blueprint is BlueprintCrusadeEvent _event)
        {
            // Event cards
            crusadeEvent = _event;
            crusadeProject = allRelicProjects.FirstOrDefault(it => GetStartEvent(it) == crusadeEvent);
            if (crusadeProject == null && !_event.EventSolutions.Any(s => !EventChoiceFlags(s).Empty()))
                return null;
        }

        if (crusadeEvent?.EventSolutions == null || crusadeEvent.EventSolutions.Length == 0)
            return null;

        var sb = new StringBuilder();
        var triggerFlags = ConditionFlags(crusadeProject?.TriggerCondition);

        foreach (var choice in GetFinalSolutions(crusadeEvent, triggerFlags))
        {
            sb.Append("\n");
            sb.Append(CustomEventChoiceText(choice));

            var choiceFlags = EventChoiceFlags(choice).ToList();
            var subProjects = allRelicProjects.Where(it =>
                it != crusadeProject && ConditionFlags(it.TriggerCondition).Any(choiceFlags.Contains));

            foreach (var subProject in subProjects)
            {
                if (GetStartEvent(subProject) is { } subEvent)
                {
                    foreach (var subChoice in GetFinalSolutions(subEvent, choiceFlags))
                    {
                        sb.Append("\n    ");
                        sb.Append(CustomEventChoiceText(subChoice));
                    }
                }
            }
        }

        if (sb.Length == 0)
            return null;

        return $"<size=85%><b>Relic Outcomes:</b>{sb}</size>";
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(KingdomUIEventWindow),
        nameof(KingdomUIEventWindow.SetDescription),
        typeof(KingdomEventUIView))]
    static void KingdomUIEventWindow_SetDescription_Postfix(
        KingdomUIEventWindow __instance,
        KingdomEventUIView kingdomEventView)
    {
        if (kingdomEventView?.Blueprint == null)
            return;

        try
        {
            var relicInfo = GetRelicInfo(kingdomEventView.Blueprint);
            if (!string.IsNullOrEmpty(relicInfo))
            {
                __instance.m_Description.text += $"\n\n{relicInfo}";
            }
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Error($"Error in KingdomUIEventWindow postfix: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KingdomEventDetailsConsoleView), nameof(KingdomEventDetailsConsoleView.SetDescription))]
    static void KingdomEventDetailsConsoleView_SetDescription_Postfix(KingdomEventDetailsConsoleView __instance)
    {
        try
        {
            var vm = Traverse
                .Create(__instance)
                .Property(nameof(KingdomEventDetailsConsoleView.ViewModel))
                .GetValue<KingdomEventDetailsVM>();
            var blueprint = vm?.Event?.Blueprint;
            if (blueprint == null)
                return;

            var relicInfo = GetRelicInfo(blueprint);
            if (!string.IsNullOrEmpty(relicInfo))
            {
                __instance.m_Description.text += $"\n\n{relicInfo}";
            }
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Error($"Error in KingdomEventDetailsConsoleView postfix: {ex}");
        }
    }
}
