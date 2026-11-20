using RimWorld;
using RimWorld.Planet;
using Verse;
using VEF.Abilities;

namespace VigilForgottenBond
{
    /// <summary>
    /// Fail-closed caster restriction. VPE paths, ability grants and other mods can
    /// all be bypassed in dev mode; the ability instance itself MUST refuse casts.
    /// Both our support psycasts and projectile psycast inherit this check.
    /// </summary>
    public static class LuminantAbilityGate
    {
        public static bool CanCast(Pawn pawn) => pawn != null && !pawn.Dead &&
            pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYears >= 7 &&
            LumenWorldComponent.Instance?.IsLuminant(pawn) == true;

        public static bool Check(Pawn pawn, out string reason)
        {
            if (CanCast(pawn)) { reason = null; return true; }
            reason = "Only the living Luminant, after her First Light, can channel Luminist psycasts.";
            return false;
        }
    }

    public class Ability_LuministSupport : Ability_Blank
    {
        public override bool IsEnabledForPawn(out string reason)
        {
            if (!LuminantAbilityGate.Check(pawn, out reason)) return false;
            return base.IsEnabledForPawn(out reason);
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            if (!LuminantAbilityGate.CanCast(pawn))
            {
                Messages.Message("Luminist psycasts require the current Luminant.",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            base.Cast(targets);
        }
    }

    public class Ability_LuministProjectile : Ability_ShootProjectile
    {
        public override bool IsEnabledForPawn(out string reason)
        {
            if (!LuminantAbilityGate.Check(pawn, out reason)) return false;
            return base.IsEnabledForPawn(out reason);
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            if (!LuminantAbilityGate.CanCast(pawn))
            {
                Messages.Message("Luminist psycasts require the current Luminant.",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            base.Cast(targets);
        }
    }
}
