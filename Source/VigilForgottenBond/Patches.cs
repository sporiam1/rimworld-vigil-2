using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using VEF.Abilities;
using VanillaPsycastsExpanded;

namespace VigilForgottenBond
{
    // Capture actual birth/spawn calls first. The conservative world scan remains
    // only as a fallback for mods which bypass this vanilla method.
    [HarmonyPatch(typeof(PawnUtility), "TrySpawnHatchedOrBornPawn")]
    internal static class Patch_NaturalBirth
    {
        public static void Postfix(bool __result, object[] __args)
        {
            if (!__result || LumenWorldComponent.Instance == null) return;
            var newborn = __args.OfType<Pawn>().FirstOrDefault(p => p?.ageTracker != null &&
                p.ageTracker.AgeBiologicalTicks < 300 && p.RaceProps.Humanlike);
            if (newborn != null) LumenWorldComponent.Instance.TryAssign(newborn);
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    internal static class Patch_GradualAwakening
    {
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        public static void Prefix(Pawn_HealthTracker __instance, ref DamageInfo dinfo)
        {
            Pawn victim = PawnRef(__instance);
            if (victim == null || dinfo.Amount <= 0) return;
            Pawn attacker = dinfo.Instigator as Pawn;

            // Trial witnesses: only unbonded swords lying on the victim's map.
            if (victim.Spawned && victim.Map != null)
                foreach (Thing sword in victim.Map.listerThings.ThingsOfDef(VGL_DefOf.VGL_VigilSword))
                    sword.TryGetComp<CompVigilPersona>()?.RecordWitness(victim, attacker, dinfo.Amount);

            var equipped = victim.equipment?.Primary?.GetComp<CompVigilPersona>();
            if (equipped != null && equipped.CanWield(victim)) equipped.GainResonance(dinfo.Amount * 0.5f);

            if (victim.health?.hediffSet?.HasHediff(VGL_DefOf.VGL_LuminousWard) == true)
                dinfo.SetAmount(dinfo.Amount * 0.82f);

            if (dinfo.Amount < 8f || LumenWorldComponent.Instance?.TryChildShield(victim) != true) return;
            dinfo.SetAmount(dinfo.Amount * 0.6f);
            if (victim.Spawned) MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Lumen ward", 2f);
        }
    }

    [HarmonyPatch]
    internal static class Patch_CanEquipVigil
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(EquipmentUtility))
                .Where(m => m.Name == "CanEquip" && m.ReturnType == typeof(bool));
        public static void Postfix(object[] __args, ref bool __result)
        {
            var thing = __args.OfType<ThingWithComps>().FirstOrDefault();
            var pawn = __args.OfType<Pawn>().FirstOrDefault();
            var comp = thing?.GetComp<CompVigilPersona>();
            if (comp != null && !comp.CanWield(pawn)) __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "AddEquipment")]
    internal static class Patch_AddEquipment
    {
        private static readonly AccessTools.FieldRef<Pawn_EquipmentTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_EquipmentTracker, Pawn>("pawn");

        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            var comp = newEq?.GetComp<CompVigilPersona>();
            return comp == null || comp.CanWield(PawnRef(__instance));
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class Patch_BearerGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            var comp = __instance?.equipment?.Primary?.GetComp<CompVigilPersona>();
            if (comp != null) foreach (Gizmo gizmo in comp.BearerGizmos(__instance)) yield return gizmo;
            if (!Prefs.DevMode || __instance?.gender != Gender.Female ||
                __instance.Faction != Faction.OfPlayer || __instance.ageTracker == null ||
                __instance.ageTracker.AgeBiologicalTicks > 60000L) yield break;
            yield return new Command_Action {
                defaultLabel = "DEV: Assign Luminant mantle",
                defaultDesc = "Debug only: designate a female infant (no second living Luminant allowed).",
                action = () => LumenWorldComponent.Instance?.TryAssign(__instance)
            };
        }
    }

    /// <summary>
    /// Locks the Luminist path to the living Luminant. VPE calls CanPawnUnlock for the
    /// Psycasts-tab Unlock button, at cast time (the path sets
    /// ignoreLockRestrictionsForNeurotrainers=false) and in RecheckPaths (it sets
    /// ensureLockRequirement=true, so a pawn who loses the mantle loses the path).
    /// v0.8 used a Class= subclass for this; a plain Harmony postfix avoids any risk
    /// of the def landing outside DefDatabase&lt;PsycasterPathDef&gt;.
    /// </summary>
    [HarmonyPatch(typeof(PsycasterPathDef), nameof(PsycasterPathDef.CanPawnUnlock))]
    internal static class Patch_LuministPathLock
    {
        public static void Postfix(PsycasterPathDef __instance, Pawn pawn, ref bool __result)
        {
            if (__result && LuministAbilities.IsLuministPath(__instance))
                __result = LuminantAbilityGate.CanCast(pawn);
        }
    }

    // Belt-and-braces: refuse to hand ANY Luminist ability (all 15 + Dawnbringer, not
    // just the three tier-I powers) to a pawn who is not the living Luminant, whether
    // the grant comes from VPE, a neurotrainer, another mod, or a debug tool.
    // Path unlocking and cast-time checks are handled by Patch_LuministPathLock.
    [HarmonyPatch(typeof(CompAbilities), nameof(CompAbilities.GiveAbility))]
    internal static class Patch_LuministAbilityGrant
    {
        public static bool Prefix(CompAbilities __instance, VEF.Abilities.AbilityDef abilityDef)
        {
            if (!LuministAbilities.IsLuministAbility(abilityDef)) return true;
            if (LuminantAbilityGate.CanCast(__instance.parent as Pawn)) return true;
            if (Prefs.DevMode)
                Log.Message("[VIGIL] Blocked Luminist ability " + abilityDef.defName + " for " +
                    (__instance.parent?.LabelShort ?? "null") + ": not the living Luminant.");
            return false;
        }
    }
}
