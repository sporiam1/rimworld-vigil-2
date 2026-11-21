using RimWorld;
using RimWorld.Planet;
using Verse;
using VEF.Abilities;

namespace VigilForgottenBond
{
    /// <summary>
    /// Fail-closed caster restriction. Patch_LuministPathLock makes VPE enforce this at
    /// unlock and cast time; the ability classes below check again independently,
    /// so a Luminist power granted by any other route still refuses to fire.
    /// </summary>
    public static class LuminantAbilityGate
    {
        public const int FirstLightAge = 7;

        public static bool CanCast(Pawn pawn) => pawn != null && !pawn.Dead &&
            pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYears >= FirstLightAge &&
            LumenWorldComponent.Instance?.IsLuminant(pawn) == true;

        public static bool Check(Pawn pawn, out string reason)
        {
            if (CanCast(pawn)) { reason = null; return true; }
            reason = "Only the living Luminant, after her First Light, can channel Luminist psycasts.";
            return false;
        }

        public static bool RejectCast(Pawn pawn)
        {
            if (CanCast(pawn)) return false;
            Messages.Message("Luminist psycasts require the current Luminant.",
                MessageTypeDefOf.RejectInput, false);
            return true;
        }
    }

    public static class LuministAbilities
    {
        public const string PathDefName = "VGL_Luminist";

        public static bool IsLuministPath(VanillaPsycastsExpanded.PsycasterPathDef path) =>
            path?.defName == PathDefName;

        public static bool IsLuministAbility(VEF.Abilities.AbilityDef def) =>
            IsLuministPath(def?.GetModExtension<VanillaPsycastsExpanded.AbilityExtension_Psycast>()?.path);
    }

    public class Ability_LuministSupport : Ability_Blank
    {
        public override bool IsEnabledForPawn(out string reason) =>
            LuminantAbilityGate.Check(pawn, out reason) && base.IsEnabledForPawn(out reason);

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            if (LuminantAbilityGate.RejectCast(pawn)) return;
            base.Cast(targets);
        }
    }

    public class Ability_LuministProjectile : Ability_ShootProjectile
    {
        public override bool IsEnabledForPawn(out string reason) =>
            LuminantAbilityGate.Check(pawn, out reason) && base.IsEnabledForPawn(out reason);

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            if (LuminantAbilityGate.RejectCast(pawn)) return;
            base.Cast(targets);
        }
    }
}
