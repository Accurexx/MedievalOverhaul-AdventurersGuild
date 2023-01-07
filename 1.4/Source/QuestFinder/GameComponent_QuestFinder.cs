using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QuestFinder;

public class GameComponent_QuestFinder : GameComponent
{
    public static GameComponent_QuestFinder Instance;
    private HashSet<QuestScriptDef> completed = new();
    private HashSet<Quest> givenByFinder = new();

    public GameComponent_QuestFinder(Game game) => Instance = this;

    public void Notify_QuestComplete(Quest quest)
    {
        if (givenByFinder.Contains(quest))
        {
            givenByFinder.Remove(quest);
            completed.Add(quest.root);
        }
    }

    public bool Completed(QuestScriptDef quest) => completed.Contains(quest);

    public void Notify_QuestIssued(Quest quest)
    {
        givenByFinder.Add(quest);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref completed, nameof(completed), LookMode.Def);
        Scribe_Collections.Look(ref givenByFinder, nameof(givenByFinder), LookMode.Reference);
    }
}
