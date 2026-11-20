using System.Linq;
using RimWorld;
using Verse;
using RimWorld.QuestGen;

namespace VigilForgottenBond
{
    // QuestScriptDefs generate *test quests* visible in Dev Mode > Generate quest.
    // Quest acceptance calls the SAME helpers used by standalone debug actions.
    public static class VigilDevScenarios
    {
        public static string Label(string key)
        {
            switch (key)
            {
                case "Vault": return "VIGIL — The Silent Vault [TEST]";
                case "Witnesses": return "VIGIL — The Three Witnesses [TEST]";
                case "FirstLight": return "Luminant — The First Light [TEST]";
                case "Bond": return "The Forgotten Bond [TEST]";
                case "Succession": return "Luminant — The Continuance [TEST]";
                default: return "VIGIL — Unknown test";
            }
        }
        public static string Description(string key)
        {
            switch (key)
            {
                case "Vault": return "Place the first VIGIL sword at the player home for bonding tests. " +
                    "No combat site is generated in this alpha.";
                case "Witnesses": return "Mark the current VIGIL candidate's Guardian and Executioner witnesses as complete. " +
                    "Use the sword's Arbiter Judgment gizmo to bond.";
                case "FirstLight": return "Grant the current living Luminant (age 7+) her First Light psycast. " +
                    "Use VIGIL: Tests > Assign Luminant to pawn first if needed.";
                case "Bond": return "Swear the guardianship oath when VIGIL has a bearer and a living Luminant exists.";
                case "Succession": return "Advance an already-active succession timer to its end. " +
                    "Does not cause a birth or automatically replace the prior Luminant.";
                default: return "Development-only scenario.";
            }
        }
        private static Map Home => Find.Maps.FirstOrDefault(m => m.IsPlayerHome);
        private static CompVigilPersona OnMapSword(Map map)
        {
            if (map == null) return null;
            return map.listerThings.ThingsOfDef(VGL_DefOf.VGL_VigilSword).Select(t => t.TryGetComp<CompVigilPersona>())
                .FirstOrDefault(c => c != null);
        }
        public static bool Execute(string key)
        {
            if (!Prefs.DevMode) return false;
            LumenWorldComponent world = LumenWorldComponent.Instance;
            Map map = Home;
            if (world == null || map == null) return false;
            switch (key)
            {
                case "Vault": return world.DevSpawnRelic(map);
                case "Witnesses": return OnMapSword(map)?.DevCompleteWitnesses() == true;
                case "FirstLight": return world.DevFirstLight();
                case "Bond":
                    var pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
                        p.equipment?.Primary?.GetComp<CompVigilPersona>()?.CanWield(p) == true);
                    return pawn?.equipment?.Primary?.GetComp<CompVigilPersona>()?.DevSwearOath() == true;
                case "Succession": return world.DevFinishSuccessionWait();
            }
            return false;
        }
        public static void RunAndNotify(string key)
        {
            bool success = Execute(key);
            Messages.Message((success ? "VIGIL test activated: " : "VIGIL test could not run: ") +
                Label(key) + (success ? "" : ". Check prerequisites in Docs/DEV_TESTS.md."),
                success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput, false);
        }
    }

    // A lightweight QuestScriptDef root. The generated quest is NOT a completed
    // story campaign; it is an opt-in launcher for the corresponding test stage.
    public sealed class QuestNode_VigilDebug : QuestNode
    {
        public string scenario;
        protected override bool TestRunInt(Slate slate) => Prefs.DevMode;
        protected override void RunInt()
        {
            QuestGen.quest.name = VigilDevScenarios.Label(scenario);
            QuestGen.quest.description = VigilDevScenarios.Description(scenario) +
                "\n\nDevelopment tool: accept this quest to execute its test action.";
            QuestGen.quest.AddPart(new QuestPart_VigilDebug { scenario = scenario });
        }
    }

    public sealed class QuestPart_VigilDebug : QuestPart
    {
        public string scenario;
        private bool queued;
        private bool finished;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref scenario, "vigilScenario");
            Scribe_Values.Look(ref queued, "vigilQueued");
            Scribe_Values.Look(ref finished, "vigilFinished");
        }
        public override void Notify_QuestAccepted()
        {
            base.Notify_QuestAccepted();
            queued = true;
        }
        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (finished || !queued) return;
            finished = true;
            VigilDevScenarios.RunAndNotify(scenario);
            // This test quest intentionally does not grant rewards.
            quest.End(QuestEndOutcome.Success);
        }
    }
}
