using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestFinder;

public class CompQuestFinder : CompScanner
{
    private CompRefuelable compRefuelable;

    private QuestScriptDef currentQuest;
    private CompAffectedByFacilities facilities;

    protected IEnumerable<QuestScriptDef> AvailableForFind =>
        from quest in QuestFinderUtility.PossibleQuests
        let ext = quest.FinderInfo()
        where ext.LinkablesNeeded <= facilities.LinkedFacilitiesListForReading.Count
        where ext.requiredLinkable == null || facilities.LinkedFacilitiesListForReading.Any(f => f.def == ext.requiredLinkable)
        where !(ext.onlyOnce && GameComponent_QuestFinder.Instance.Completed(quest))
        select quest;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compRefuelable = parent.TryGetComp<CompRefuelable>();
        facilities = parent.TryGetComp<CompAffectedByFacilities>();
        currentQuest ??= AvailableForFind.RandomElement();
    }

    protected override void DoFind(Pawn worker)
    {
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(currentQuest, StorytellerUtility.DefaultSiteThreatPointsNow());
        GameComponent_QuestFinder.Instance.Notify_QuestIssued(quest);
        QuestUtility.SendLetterQuestAvailable(quest);
    }

    protected override bool TickDoesFind(float scanSpeed) => daysWorkingSinceLastFinding >= currentQuest.FinderInfo().WorkTillTrigger.TicksToDays();

    public override IEnumerable<Gizmo> CompGetGizmosExtra() =>
        base.CompGetGizmosExtra()
           .Append(new Command_Action
            {
                defaultLabel = currentQuest.LabelCap(),
                defaultDesc = "EEG.SelectQuest".Translate(currentQuest.LabelCap()),
                icon = TexButton.Search,
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(AvailableForFind
                       .Select(quest => new FloatMenuOption(quest.LabelCap(), () => currentQuest = quest))
                       .ToList()));
                }
            });

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref currentQuest, nameof(currentQuest));
    }

    public override string CompInspectStringExtra() =>
        $"{"EEG.SearchingFor".Translate()}: {currentQuest.LabelCap()}\n"
      + (lastScanTick > Find.TickManager.TicksGame - 30 ? $"{"UserScanAbility".Translate()}: {lastUserSpeed.ToStringPercent()}\n" : "")
      + $"{"EEG.ScanningProgress".Translate()}: "
      + (daysWorkingSinceLastFinding / currentQuest.FinderInfo().WorkTillTrigger.TicksToDays()).ToStringPercent();

    public static void CanUseNow_Postfix(CompScanner __instance, ref bool __result)
    {
        if (__result && __instance is CompQuestFinder { compRefuelable.HasFuel: false }) __result = false;
    }
}

public class CompProperties_QuestFinder : CompProperties_Scanner
{
    public CompProperties_QuestFinder() => compClass = typeof(CompQuestFinder);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        yield break;
    }
}

public class QuestInformation : DefModExtension
{
    public int LinkablesNeeded;
    public int WorkTillTrigger; // ReSharper disable InconsistentNaming
    [MustTranslate] public string label;
    public bool onlyOnce;
    public ThingDef requiredLinkable;
}
