using LudeonTK;
using RimWorld;
using Verse;

namespace VigilForgottenBond
{
    // Companion controls for states which cannot be reached by quest generation
    // alone, such as assigning a grown female tester without waiting for birth.
    public static class VigilDebugActions
    {
        [DebugAction("VIGIL: Tests", "Spawn the Silent Vault relic", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnRelic() => VigilDevScenarios.RunAndNotify("Vault");

        [DebugAction("VIGIL: Tests", "Complete the Three Witnesses", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Witnesses() => VigilDevScenarios.RunAndNotify("Witnesses");

        [DebugAction("VIGIL: Tests", "Trigger First Light", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FirstLight() => VigilDevScenarios.RunAndNotify("FirstLight");

        [DebugAction("VIGIL: Tests", "Trigger guardianship oath", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Bond() => VigilDevScenarios.RunAndNotify("Bond");

        [DebugAction("VIGIL: Tests", "Finish succession wait", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Succession() => VigilDevScenarios.RunAndNotify("Succession");

        // --- Milestone 2a: VIGIL's abilities, Accord and awakening ---------------------

        [DebugAction("VIGIL: Tests", "Sword: Accord +25", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AccordUp() => WithBoundSword(c => c.DevAdjustAccord(25));

        [DebugAction("VIGIL: Tests", "Sword: Accord -25", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AccordDown() => WithBoundSword(c => c.DevAdjustAccord(-25));

        [DebugAction("VIGIL: Tests", "Sword: Fill Resonance", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FillResonance() => WithBoundSword(c => c.DevFillResonance());

        [DebugAction("VIGIL: Tests", "Sword: Reset ability cooldowns", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetCooldowns() => WithBoundSword(c => c.DevResetCooldowns());

        [DebugAction("VIGIL: Tests", "Sword: Advance awakening stage", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AdvanceStage() => WithBoundSword(c => c.DevAdvanceStage());

        private static void WithBoundSword(System.Action<CompVigilPersona> act)
        {
            CompVigilPersona comp = CompVigilPersona.FindAny();
            if (comp?.Bearer == null)
            {
                Messages.Message("DEV: VIGIL has no bearer yet. Spawn it and complete the Three Witnesses first.",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            act(comp);
            Messages.Message("DEV: VIGIL now " + CompVigilPersona.StageLabel(comp.Stage) + ", Accord " + comp.Accord +
                " (" + CompVigilPersona.TierLabel(comp.Tier) + "), Resonance " + (int)comp.Resonance + ".",
                MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("VIGIL: Tests", "Assign Luminant to existing female pawn", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AssignExisting(Pawn pawn)
        {
            bool ok = LumenWorldComponent.Instance?.DevAssign(pawn) == true;
            Messages.Message(ok ? "DEV: Luminant assigned to " + pawn.LabelShortCap :
                "DEV: Luminant assignment denied (female colonist required; one living mantle only).",
                ok ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput, false);
        }
    }
}
