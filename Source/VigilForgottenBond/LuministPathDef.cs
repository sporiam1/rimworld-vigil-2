using Verse;
using VanillaPsycastsExpanded;
using AbilityDef = VEF.Abilities.AbilityDef;

namespace VigilForgottenBond
{
    /// <summary>
    /// The Luminist path, locked to the one living Luminant through VPE's own API.
    ///
    /// VPE consults CanPawnUnlock in three places, so overriding it replaces the
    /// v0.7 reflection patch:
    ///   * the Psycasts tab Unlock button (bypassed by VPE itself in Dev Mode, which
    ///     is how non-Luminants got the path during the v0.6 playtest),
    ///   * AbilityExtension_Psycast.IsEnabledForPawn at cast time, because the def
    ///     sets ignoreLockRestrictionsForNeurotrainers = false,
    ///   * PsycastUtility.RecheckPaths, because the def sets ensureLockRequirement =
    ///     true. A pawn who loses the mantle has the path moved aside, and gets it
    ///     back if the mantle returns (e.g. resurrection before succession).
    /// The Ability_Luminist* classes keep a second, independent check.
    /// </summary>
    public class LuministPathDef : PsycasterPathDef
    {
        public override bool CanPawnUnlock(Pawn pawn) =>
            base.CanPawnUnlock(pawn) && LuminantAbilityGate.CanCast(pawn);

        public static bool IsLuministAbility(AbilityDef def) =>
            def?.GetModExtension<AbilityExtension_Psycast>()?.path is LuministPathDef;
    }
}
