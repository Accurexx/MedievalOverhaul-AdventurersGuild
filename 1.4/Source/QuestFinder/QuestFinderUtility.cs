using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QuestFinder;

[StaticConstructorOnStartup]
public static class QuestFinderUtility
{
    private static readonly List<QuestScriptDef> possibleQuests;
    private static readonly Dictionary<QuestScriptDef, QuestInformation> extensions;
    public static Harmony Harm;

    static QuestFinderUtility()
    {
        Harm = new Harmony("legodude17.questfinder");
        possibleQuests = DefDatabase<QuestScriptDef>.AllDefs.Where(def => def.GetModExtension<QuestInformation>() != null).ToList();
        extensions = possibleQuests.ToDictionary(q => q, q => q.GetModExtension<QuestInformation>());
        Harm.Patch(AccessTools.PropertyGetter(typeof(CompScanner), nameof(CompScanner.CanUseNow)),
            postfix: new HarmonyMethod(typeof(CompQuestFinder), nameof(CompQuestFinder.CanUseNow_Postfix)));
        Harm.Patch(AccessTools.Method(typeof(Quest), nameof(Quest.End)), postfix: new HarmonyMethod(typeof(QuestFinderUtility), nameof(Quest_End_Postfix)));
    }

    public static IEnumerable<QuestScriptDef> PossibleQuests => possibleQuests;
    public static QuestInformation FinderInfo(this QuestScriptDef quest) => extensions[quest];
    public static string Label(this QuestScriptDef quest) => extensions[quest].label;
    public static string LabelCap(this QuestScriptDef quest) => extensions[quest].label.CapitalizeFirst();

    public static void Quest_End_Postfix(Quest __instance, QuestEndOutcome outcome)
    {
        if (outcome == QuestEndOutcome.Success) GameComponent_QuestFinder.Instance.Notify_QuestComplete(__instance);
    }
}
