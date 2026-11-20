using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using VEF.Abilities;

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
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");
        public static void Prefix(Pawn_HealthTracker __instance, ref DamageInfo dinfo)
        {
            var victim = PawnField?.GetValue(__instance) as Pawn;
            if (victim == null || dinfo.Amount <= 0) return;
            Pawn attacker = dinfo.Instigator as Pawn;
            // Small scan: only ground swords at the victim's location (during an active trial).
            if (victim.Spawned && victim.Map != null) {
                var vigil = DefDatabase<ThingDef>.GetNamedSilentFail("VGL_VigilSword");
                if (vigil != null)
                    foreach (Thing sword in victim.Map.listerThings.ThingsOfDef(vigil))
                        sword.TryGetComp<CompVigilPersona>()?.RecordWitness(victim, attacker, dinfo.Amount);
            }
            var equipped = victim.equipment?.Primary?.GetComp<CompVigilPersona>();
            if (equipped != null && equipped.CanWield(victim)) equipped.GainResonance(dinfo.Amount * 0.5f);
            var ward = DefDatabase<HediffDef>.GetNamedSilentFail("VGL_LuminousWard");
            if (ward != null && victim.health?.hediffSet?.HasHediff(ward) == true)
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
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_EquipmentTracker), "pawn");
        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            var comp = newEq?.GetComp<CompVigilPersona>();
            return comp == null || comp.CanWield(PawnField?.GetValue(__instance) as Pawn);
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
    [HarmonyPatch]
    internal static class Patch_LuministUnlock
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(VanillaPsycastsExpanded.PsycasterPathDef))
                .Where(m => m.Name == "CanPawnUnlock" && m.ReturnType == typeof(bool));
        public static void Postfix(VanillaPsycastsExpanded.PsycasterPathDef __instance,
            object[] __args, ref bool __result)
        {
            if (__instance.defName != "VGL_Luminist") return;
            Pawn pawn = __args.OfType<Pawn>().FirstOrDefault();
            __result = __result && LuminantAbilityGate.CanCast(pawn);
        }
    }
    [HarmonyPatch]
    internal static class Patch_LuministAbilityGrant
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(CompAbilities))
                .Where(m => m.Name == "GiveAbility" && m.GetParameters().Length > 0 &&
                            m.GetParameters()[0].ParameterType == typeof(VEF.Abilities.AbilityDef));
        public static bool Prefix(CompAbilities __instance, VEF.Abilities.AbilityDef __0)
        {
            if (__0?.defName == null || !__0.defName.StartsWith("VGL_")) return true;
            if (__0.defName != "VGL_LuminousWardCast" && __0.defName != "VGL_SecondSightCast" &&
                __0.defName != "VGL_RadiantSparkCast") return true;
            return LuminantAbilityGate.CanCast(__instance.parent as Pawn);
        }
    }
}
