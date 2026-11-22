using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VigilForgottenBond
{
    /// <summary>
    /// VIGIL's abilities. Gizmos appear on the bearer while he holds the sword.
    ///
    ///   Aegis          Guardian    30 Res, 1 day    Allies within 5 tiles take −40% damage for 30 s.
    ///   Sever          Executioner 25 Res, 1 hour   Adjacent hostile: heavy armor-piercing cut.
    ///   Verdict        Arbiter     40 Res, 12 hours Hostile in sight (≤ 8): stunned, then Judged
    ///                                               (slowed, clumsy, takes +15% damage).
    ///   Final Mandate  Ascendant  100 Res, 3 days   Radiant shockwave: hostiles within 6 are struck
    ///                                               and stunned, allies gain Aegis.
    ///   Last Stand     passive (Cooperative+), once per 2 days: below 45% health, a big hit is
    ///                  reduced by 70% and VIGIL steadies him (pain −80%, +consciousness).
    ///
    /// Damage scales with awakening (Bound ×1, Awakened ×1.25, Ascendant ×1.5).
    /// Trusted Accord: costs −20%. Fractured Accord: Sever, Verdict, Final Mandate refused.
    /// Numbers are first-pass balance; tune after playtesting.
    /// </summary>
    public sealed partial class CompVigilPersona
    {
        public const float AegisCost = 30f, SeverCost = 25f, VerdictCost = 40f, MandateCost = 100f;
        public const int AegisCooldown = GenDate.TicksPerDay;
        public const int SeverCooldown = GenDate.TicksPerHour;
        public const int VerdictCooldown = GenDate.TicksPerDay / 2;
        public const int MandateCooldown = GenDate.TicksPerDay * 3;
        public const int ConsultCooldown = GenDate.TicksPerDay;
        public const int LastStandCooldown = GenDate.TicksPerDay * 2;

        private int lastAegisTick = -999999;
        private int lastSeverTick = -999999;
        private int lastVerdictTick = -999999;
        private int lastMandateTick = -999999;
        private int lastStandTick = -999999;
        private int lastConsultTick = -999999;
        private float lifetimeResonanceSpent;

        public float LifetimeResonanceSpent => lifetimeResonanceSpent;

        private void ExposeAbilityData()
        {
            Scribe_Values.Look(ref lastAegisTick, "vigilWardTick", -999999); // v0.8 key kept
            Scribe_Values.Look(ref lastSeverTick, "vigilSeverTick", -999999);
            Scribe_Values.Look(ref lastVerdictTick, "vigilVerdictTick", -999999);
            Scribe_Values.Look(ref lastMandateTick, "vigilMandateTick", -999999);
            Scribe_Values.Look(ref lastStandTick, "vigilLastStandTick", -999999);
            Scribe_Values.Look(ref lastConsultTick, "vigilLastConsult", -999999);
            Scribe_Values.Look(ref lastThreatAccordTick, "vigilThreatAccordTick", -999999);
            Scribe_Values.Look(ref lifetimeResonanceSpent, "vigilResonanceSpent", 0f);
        }

        private void ResetCooldowns()
        {
            lastAegisTick = lastSeverTick = lastVerdictTick = lastMandateTick =
                lastStandTick = lastConsultTick = lastThreatAccordTick = -999999;
        }

        private static int Now => Find.TickManager.TicksGame;
        private float CostFactor => Tier >= AccordTier.Trusted ? 0.8f : 1f;
        private float Cost(float baseCost) => Mathf.Round(baseCost * CostFactor);

        private float StageDamageFactor =>
            Stage == VigilStage.Ascendant ? 1.5f : Stage == VigilStage.Awakened ? 1.25f : 1f;

        private float StageResonanceFactor =>
            Stage == VigilStage.Ascendant ? 1.3f : Stage == VigilStage.Awakened ? 1.15f : 1f;

        private void Spend(float amount)
        {
            resonance = Mathf.Max(0f, resonance - amount);
            lifetimeResonanceSpent += amount;
            CheckAwakening();
        }

        /// <summary>Returns why an ability can't be used right now, or null if it can.</summary>
        private string BlockReason(float baseCost, int lastTick, int cooldown, bool executionerOrArbiter)
        {
            if (bearer == null || !bearer.Spawned || bearer.Downed) return "The bearer can't act.";
            if (executionerOrArbiter && Tier == AccordTier.Fractured)
                return "VIGIL refuses. Accord is Fractured (below 25).";
            if (Now < lastTick + cooldown)
                return "Recovering: " + (lastTick + cooldown - Now).ToStringTicksToPeriod() + " left.";
            if (resonance < Cost(baseCost))
                return "Needs " + Cost(baseCost) + " Resonance (has " + (int)resonance + ").";
            return null;
        }

        // --- Gizmos ------------------------------------------------------------------

        public IEnumerable<Gizmo> BearerGizmos(Pawn pawn)
        {
            if (!IsWieldedBy(pawn)) yield break;
            // Self-heal: e.g. a bearer resurrected still holding VIGIL never "re-equips" it.
            if (!pawn.health.hediffSet.HasHediff(VGL_DefOf.VGL_VigilBond)) UpdateBondHediff(pawn, true);

            yield return new Gizmo_VigilStatus(this);

            var aegis = new Command_Action {
                defaultLabel = "Aegis",
                defaultDesc = "Guardian. VIGIL projects a ward: " + pawn.LabelShort +
                    " and allies within 5 tiles take 40% less damage for 30 seconds.\n\nCost: " +
                    Cost(AegisCost) + " Resonance. Recovers in 1 day.",
                icon = VigilTex.Aegis,
                action = Aegis
            };
            DisableIf(aegis, BlockReason(AegisCost, lastAegisTick, AegisCooldown, false));
            yield return aegis;

            var sever = new Command_Target {
                defaultLabel = "Sever",
                defaultDesc = "Executioner. A single cut that ignores most armor, against an adjacent enemy.\n\n" +
                    "Damage " + (int)(32f * StageDamageFactor) + ". Cost: " + Cost(SeverCost) +
                    " Resonance. Recovers in 1 hour.",
                icon = VigilTex.Sever,
                targetingParams = HostileTargeting(pawn),
                action = t => Sever(t.Pawn)
            };
            DisableIf(sever, BlockReason(SeverCost, lastSeverTick, SeverCooldown, true));
            yield return sever;

            var verdictCmd = new Command_Target {
                defaultLabel = "Verdict",
                defaultDesc = "Arbiter. VIGIL passes judgment on an enemy in sight within 8 tiles: stunned for " +
                    (int)(4f * StageDamageFactor) + " seconds, then Judged for 40 seconds: slowed, clumsy, and taking 15% more damage.\n\n" +
                    "Cost: " + Cost(VerdictCost) + " Resonance. Recovers in 12 hours.",
                icon = VigilTex.Verdict,
                targetingParams = HostileTargeting(pawn),
                action = t => Verdict(t.Pawn)
            };
            DisableIf(verdictCmd, BlockReason(VerdictCost, lastVerdictTick, VerdictCooldown, true));
            yield return verdictCmd;

            if (Stage == VigilStage.Ascendant || Prefs.DevMode)
            {
                var mandate = new Command_Action {
                    defaultLabel = "Final Mandate",
                    defaultDesc = "The broken halo closes, for a moment. A radiant shockwave strikes and stuns every enemy " +
                        "within 6 tiles; allies inside it gain Aegis.\n\nRequires Ascendant VIGIL. Cost: " +
                        Cost(MandateCost) + " Resonance. Recovers in 3 days.",
                    icon = VigilTex.Mandate,
                    action = FinalMandate
                };
                string reason = Stage != VigilStage.Ascendant ? "Requires Ascendant VIGIL." :
                    BlockReason(MandateCost, lastMandateTick, MandateCooldown, true);
                DisableIf(mandate, reason);
                yield return mandate;
            }

            var consult = new Command_Action {
                defaultLabel = "Consult VIGIL",
                defaultDesc = "Talk with VIGIL. Once per day; Accord +2.",
                icon = VigilTex.Consult,
                action = Consult
            };
            if (Now < lastConsultTick + ConsultCooldown) consult.Disable("VIGIL has said enough for today.");
            yield return consult;

            if (!oathSworn && LumenWorldComponent.Instance?.Current != null)
                yield return new Command_Action {
                    defaultLabel = "Swear guardianship oath",
                    defaultDesc = "Become the Luminant's sworn knight, the Warden of the Dawn. Accord +10.",
                    icon = VigilTex.Oath,
                    action = SwearOath
                };
        }

        private static void DisableIf(Command cmd, string reason)
        {
            if (reason != null) cmd.Disable(reason);
        }

        private static TargetingParameters HostileTargeting(Pawn bearer) => new TargetingParameters {
            canTargetPawns = true,
            canTargetBuildings = false,
            canTargetItems = false,
            canTargetLocations = false,
            validator = t => t.Thing is Pawn p && !p.Dead && p.HostileTo(bearer)
        };

        // --- Abilities ---------------------------------------------------------------

        private void Aegis()
        {
            if (BlockReason(AegisCost, lastAegisTick, AegisCooldown, false) != null) return;
            Map map = bearer.Map;
            var allies = map.mapPawns.AllPawnsSpawned
                .Where(p => !p.Dead && (p == bearer || (!p.HostileTo(bearer) && p.Faction == bearer.Faction)) &&
                            p.Position.InHorDistOf(bearer.Position, 5f))
                .ToList();
            foreach (Pawn p in allies) ApplyHediff(p, VGL_DefOf.VGL_Aegis);
            Spend(Cost(AegisCost));
            lastAegisTick = Now;
            FleckMaker.Static(bearer.Position, map, FleckDefOf.PsycastAreaEffect, 10f);
            MoteMaker.ThrowText(bearer.DrawPos, map, "Aegis", new Color(0.5f, 0.9f, 1f));
            if (allies.Count >= 2) AdjustAccord(1, "shielded " + (allies.Count - 1) + " allies");
        }

        private void Sever(Pawn target)
        {
            if (target == null || BlockReason(SeverCost, lastSeverTick, SeverCooldown, true) != null) return;
            if (!bearer.Position.AdjacentTo8WayOrInside(target.Position))
            {
                Messages.Message("Sever: the target must be adjacent.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            var dinfo = new DamageInfo(DamageDefOf.Cut, 32f * StageDamageFactor, 1.4f, -1f, bearer, null, parent.def);
            target.TakeDamage(dinfo);
            Spend(Cost(SeverCost));
            lastSeverTick = Now;
            FleckMaker.ThrowMicroSparks(target.DrawPos, bearer.Map);
            MoteMaker.ThrowText(target.DrawPos, bearer.Map, "Sever", new Color(1f, 0.75f, 0.3f));
        }

        private void Verdict(Pawn target)
        {
            if (target == null || BlockReason(VerdictCost, lastVerdictTick, VerdictCooldown, true) != null) return;
            Map map = bearer.Map;
            if (!target.Position.InHorDistOf(bearer.Position, 8f) ||
                !GenSight.LineOfSight(bearer.Position, target.Position, map))
            {
                Messages.Message("Verdict: the target must be in sight within 8 tiles.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            target.stances?.stunner?.StunFor((int)(240 * StageDamageFactor), bearer);
            ApplyHediff(target, VGL_DefOf.VGL_Judged);
            Spend(Cost(VerdictCost));
            lastVerdictTick = Now;
            FleckMaker.Static(target.Position, map, FleckDefOf.PsycastAreaEffect, 2f);
            MoteMaker.ThrowText(target.DrawPos, map, "Judged", new Color(0.75f, 0.55f, 1f));
        }

        private void FinalMandate()
        {
            if (Stage != VigilStage.Ascendant ||
                BlockReason(MandateCost, lastMandateTick, MandateCooldown, true) != null) return;
            Map map = bearer.Map;
            IntVec3 center = bearer.Position;
            var inRange = map.mapPawns.AllPawnsSpawned
                .Where(p => !p.Dead && p.Position.InHorDistOf(center, 6f)).ToList();
            int struck = 0;
            foreach (Pawn p in inRange)
            {
                if (p.HostileTo(bearer))
                {
                    p.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 22f, 0.8f, -1f, bearer, null, parent.def));
                    p.stances?.stunner?.StunFor(300, bearer);
                    struck++;
                }
                else if (p.Faction == bearer.Faction)
                {
                    ApplyHediff(p, VGL_DefOf.VGL_Aegis);
                }
            }
            Spend(Cost(MandateCost));
            lastMandateTick = Now;
            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, 14f);
            MoteMaker.ThrowText(bearer.DrawPos, map, "FINAL MANDATE", new Color(1f, 0.9f, 0.5f), 4f);
            Messages.Message("VIGIL: 'By my verdict, this ends.' Final Mandate struck " + struck + " enemies.",
                bearer, MessageTypeDefOf.PositiveEvent, false);
        }

        private void Consult()
        {
            if (bearer == null || bearer.Dead || Now < lastConsultTick + ConsultCooldown) return;
            lastConsultTick = Now;
            Messages.Message("VIGIL: " + RandomConsultLine(), bearer, MessageTypeDefOf.NeutralEvent, false);
            AdjustAccord(2, "counsel");
        }

        /// <summary>Called from the damage prefix when the bearer is hit. Returns true if it fired.</summary>
        public bool TryLastStand(Pawn victim, ref DamageInfo dinfo)
        {
            if (!IsWieldedBy(victim) || Tier < AccordTier.Cooperative) return false;
            if (dinfo.Amount < 5f || Now < lastStandTick + LastStandCooldown) return false;
            if (victim.health.summaryHealth.SummaryHealthPercent > 0.45f) return false;
            lastStandTick = Now;
            dinfo.SetAmount(dinfo.Amount * 0.3f);
            ApplyHediff(victim, VGL_DefOf.VGL_LastStand);
            if (victim.Spawned)
                MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Last Stand", new Color(1f, 0.85f, 0.4f), 3f);
            Messages.Message("VIGIL: 'Not yet, " + victim.LabelShort + ". Stand.'", victim,
                MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private static void ApplyHediff(Pawn p, HediffDef def)
        {
            Hediff existing = p.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null) p.health.RemoveHediff(existing); // refresh duration
            p.health.AddHediff(def);
        }

        // --- Dev ---------------------------------------------------------------------

        public void DevAdjustAccord(int delta) => AdjustAccord(delta, "debug");
        public void DevFillResonance() { if (bearer != null) resonance = 100f; }
        public void DevResetCooldowns() => ResetCooldowns();
    }

    /// <summary>Resonance and Accord bars shown with the bearer's ability buttons.</summary>
    [StaticConstructorOnStartup]
    public class Gizmo_VigilStatus : Gizmo
    {
        private static readonly Texture2D ResonanceFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.8f, 0.95f));
        private static readonly Texture2D AccordFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.9f, 0.75f, 0.35f));
        private static readonly Texture2D BarBack = SolidColorMaterials.NewSolidColorTexture(new Color(0.08f, 0.08f, 0.1f));

        private readonly CompVigilPersona comp;

        public Gizmo_VigilStatus(CompVigilPersona comp)
        {
            this.comp = comp;
            Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 150f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);
            Rect inner = rect.ContractedBy(6f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 16f),
                "VIGIL — " + CompVigilPersona.StageLabel(comp.Stage));

            Rect res = new Rect(inner.x, inner.y + 18f, inner.width, 20f);
            Widgets.FillableBar(res, comp.Resonance / 100f, ResonanceFill, BarBack, false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(res, "Resonance " + (int)comp.Resonance);

            Rect acc = new Rect(inner.x, inner.y + 42f, inner.width, 20f);
            Widgets.FillableBar(acc, comp.Accord / 100f, AccordFill, BarBack, false);
            Widgets.Label(acc, "Accord " + comp.Accord + " · " + CompVigilPersona.TierLabel(comp.Tier));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(rect,
                "Resonance powers VIGIL's abilities. It builds when " + (comp.Bearer?.LabelShort ?? "the bearer") +
                " deals or takes damage in combat.\n\nAccord is VIGIL's trust. It rises with counsel, " +
                "protecting allies, the oath and ending threats, and falls when allies die within reach.\n\n" +
                comp.NextAwakeningHint());
            return new GizmoResult(GizmoState.Clear);
        }
    }

    [StaticConstructorOnStartup]
    public static class VigilTex
    {
        public static readonly Texture2D Aegis = ContentFinder<Texture2D>.Get("Vigil/Icons/Abilities/Aegis");
        public static readonly Texture2D Sever = ContentFinder<Texture2D>.Get("Vigil/Icons/Abilities/Sever");
        public static readonly Texture2D Verdict = ContentFinder<Texture2D>.Get("Vigil/Icons/Abilities/Verdict");
        public static readonly Texture2D Mandate = ContentFinder<Texture2D>.Get("Vigil/Icons/Abilities/FinalMandate");
        public static readonly Texture2D Consult = ContentFinder<Texture2D>.Get("Vigil/Icons/Abilities/Consult");
        public static readonly Texture2D Oath = ContentFinder<Texture2D>.Get("Vigil/Icons/Abilities/Oath");
    }
}
