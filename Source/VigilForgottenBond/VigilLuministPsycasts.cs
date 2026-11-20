using System.Linq;
using RimWorld;
using Verse;
using VEF.Abilities;
using VanillaPsycastsExpanded;
using AbilityDef = VEF.Abilities.AbilityDef;

namespace VigilForgottenBond
{
    public static class VigilLuministPsycasts
    {
        // Adult story and complete path are for subsequent milestones.
        // Here we grant exactly one first-tier ability at the first awakening.
        public static bool EnsureFirstAbility(Pawn pawn)
        {
            if (pawn == null || !LumenWorldComponent.Instance.IsLuminant(pawn) ||
                pawn.ageTracker.AgeBiologicalYears < 7) return false;

            var path = DefDatabase<PsycasterPathDef>.GetNamedSilentFail("VGL_Luminist");
            var ward = DefDatabase<AbilityDef>.GetNamedSilentFail("VGL_LuminousWardCast");
            var comp = pawn.GetComp<CompAbilities>();
            if (path == null || ward == null || comp == null || pawn.health == null) return false;

            var psylink = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) as Hediff_Psylink;
            if (psylink == null)
            {
                var brain = pawn.health.hediffSet.GetBrain();
                if (brain == null) return false;
                psylink = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn, brain) as Hediff_Psylink;
                if (psylink == null) return false;
                psylink.Severity = 1f;
                pawn.health.AddHediff(psylink, brain);
            }

            var implant = pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_PsycastAbilityImplant)
                as Hediff_PsycastAbilities;
            if (implant == null)
            {
                implant = pawn.health.AddHediff(VPE_DefOf.VPE_PsycastAbilityImplant, psylink.Part)
                    as Hediff_PsycastAbilities;
            }
            if (implant == null) return false;
            if (implant.psylink == null) implant.InitializeFromPsylink(psylink);
            if (!implant.unlockedPaths.Contains(path)) implant.UnlockPath(path);
            // Checking the LearnedAbilities avoids duplicated grants when loading a save.
            if (!comp.LearnedAbilities.Any(a => a.def == ward))
                comp.GiveAbility(ward);
            return true;
        }
    }
}
