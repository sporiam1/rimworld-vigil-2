using RimWorld;
using Verse;
using VanillaPsycastsExpanded;

namespace VigilForgottenBond
{
    // Resolved once at startup instead of a DefDatabase lookup on every damage event.
    [DefOf]
    public static class VGL_DefOf
    {
        public static ThingDef VGL_VigilSword;
        public static TraitDef VGL_LuminantTrait;
        public static HediffDef VGL_LuminantMantle;
        public static HediffDef VGL_LuminousWard;
        public static HediffDef VGL_Aegis;
        public static HediffDef VGL_Judged;
        public static HediffDef VGL_LastStand;
        public static HediffDef VGL_VigilBond;
        public static PsycasterPathDef VGL_Luminist;
        public static VEF.Abilities.AbilityDef VGL_LuminousWardCast;

        static VGL_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VGL_DefOf));
    }
}
