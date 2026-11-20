using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VigilForgottenBond
{
    public sealed class CompProperties_VigilPersona : CompProperties
    {
        public CompProperties_VigilPersona() { compClass = typeof(CompVigilPersona); }
    }

    // The Three Witnesses (v0.6). v0.8: a dead bearer releases the sword (see ReleaseLostBearer).
    // Candidates witness a threatened ally and personally strike a hostile target,
    // then decide which principle guides their final oath. All state is saved.
    public sealed class CompVigilPersona : ThingComp
    {
        private Pawn bearer;
        private Pawn candidate;
        private bool guardianWitness;
        private bool executionerWitness;
        private int candidateTick = -1;
        private int accord = 50;
        private float resonance = 40f;
        private int lastWardTick = -999999;
        private int awakening = 1;
        private bool oathSworn;
        private int lastConsultTick = -999999;
        private string verdict = "";
        // Names, not Pawn references: dead pawns can be cleaned out of the world,
        // and a dangling reference would just log a load error.
        private List<string> rememberedBearers = new List<string>();

        public Pawn Bearer => bearer;
        public Pawn Candidate => candidate;
        public bool HasGuardianWitness => guardianWitness;
        public bool HasExecutionerWitness => executionerWitness;
        public IReadOnlyList<string> RememberedBearers => rememberedBearers;

        public bool CanWield(Pawn pawn)
        {
            ReleaseLostBearer();
            return pawn != null && pawn == bearer && !pawn.Dead && pawn.gender == Gender.Male;
        }

        /// <summary>
        /// v0.7 bug: a dead bearer stayed in 'bearer' forever, so nobody could ever
        /// petition or equip VIGIL again. Checked lazily wherever the sword is
        /// inspected, offered, or equipped. If the bearer is resurrected before anyone
        /// handles the sword, he simply keeps it, the same rule as the Luminant mantle.
        /// </summary>
        private void ReleaseLostBearer()
        {
            if (bearer == null || !bearer.Dead) return;
            Pawn fallen = bearer;
            string name = fallen.Name?.ToStringFull ?? fallen.LabelShort;
            if (!rememberedBearers.Contains(name)) rememberedBearers.Add(name);
            bearer = null;
            candidate = null;
            guardianWitness = false;
            executionerWitness = false;
            oathSworn = false;
            verdict = "";
            awakening = 1;
            resonance = 0f;
            accord = 50;
            Find.LetterStack.ReceiveLetter("VIGIL Remembers",
                "VIGIL has lost its bearer, " + fallen.LabelShortCap + ". The persona core falls " +
                "silent, then speaks once: 'I will not forget him.'\n\nThe sword is dormant again. " +
                "A new male candidate may petition it and face the Three Witnesses.",
                LetterDefOf.NeutralEvent,
                parent.SpawnedOrAnyParentSpawned ? new LookTargets(parent) : LookTargets.Invalid);
        }
        public bool CanOffer(Pawn pawn) => pawn != null && !pawn.Dead && pawn.RaceProps.Humanlike &&
            pawn.gender == Gender.Male && pawn.ageTracker?.AgeBiologicalYears >= 13 && pawn.Faction == Faction.OfPlayer;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref bearer, "vigilBearer");
            Scribe_References.Look(ref candidate, "vigilTrialCandidate");
            Scribe_Values.Look(ref guardianWitness, "vigilGuardianWitness", false);
            Scribe_Values.Look(ref executionerWitness, "vigilExecutionerWitness", false);
            Scribe_Values.Look(ref candidateTick, "vigilCandidateTick", -1);
            Scribe_Values.Look(ref accord, "vigilAccord", 50);
            Scribe_Values.Look(ref resonance, "vigilResonance", 40f);
            Scribe_Values.Look(ref lastWardTick, "vigilWardTick", -999999);
            Scribe_Values.Look(ref awakening, "vigilAwakening", 1);
            Scribe_Values.Look(ref oathSworn, "vigilOathSworn", false);
            Scribe_Values.Look(ref lastConsultTick, "vigilLastConsult", -999999);
            Scribe_Values.Look(ref verdict, "vigilVerdict", "");
            Scribe_Collections.Look(ref rememberedBearers, "vigilRememberedBearers", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && rememberedBearers == null)
                rememberedBearers = new List<string>();
        }

        public override string CompInspectStringExtra()
        {
            ReleaseLostBearer();
            string memory = rememberedBearers.Count == 0 ? "" :
                "\nRemembers: " + string.Join(", ", rememberedBearers);
            if (bearer != null)
                return "VIGIL — Bearer: " + bearer.LabelShortCap + " | Accord " + accord +
                       "/100 | Resonance " + (int)resonance + "/100 | Stage " + awakening +
                       (oathSworn ? " | Oathbound" : "") + (verdict == "" ? "" : " | Verdict: " + verdict) + memory;
            if (candidate == null) return "VIGIL — Dormant. A male colonist must petition for the Three Witnesses." + memory;
            return "VIGIL — Candidate: " + candidate.LabelShortCap +
                   " | Guardian " + (guardianWitness ? "witnessed" : "pending") +
                   " | Executioner " + (executionerWitness ? "witnessed" : "pending") +
                   " | Arbiter " + (guardianWitness && executionerWitness ? "ready" : "locked") + memory;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
            if (!parent.Spawned || parent.Map == null) yield break;
            ReleaseLostBearer();
            if (bearer != null) yield break;
            yield return new Command_Action {
                defaultLabel = candidate == null ? "Petition VIGIL" : "Change candidate",
                defaultDesc = "Choose a male colonist to face the Guardian, Executioner, and Arbiter witnesses.",
                action = OfferCandidate
            };
            if (candidate != null && guardianWitness && executionerWitness)
                yield return new Command_Action { defaultLabel = "The Arbiter's Judgment",
                    defaultDesc = "The candidate states his purpose. The sword chooses whether to bond.", action = FinalVerdict };
            if (Prefs.DevMode && candidate != null)
                yield return new Command_Action { defaultLabel = "DEV: Complete witnesses",
                    defaultDesc = "Test-only: complete the Guardian and Executioner witness flags.", action = () => {
                        guardianWitness = true; executionerWitness = true;
                        Messages.Message("VIGIL: both witnesses marked for testing.", MessageTypeDefOf.PositiveEvent, false);
                    }};
        }

        public IEnumerable<Gizmo> BearerGizmos(Pawn pawn)
        {
            if (!CanWield(pawn)) yield break;
            yield return new Command_Action { defaultLabel = "Guardian's Ward",
                defaultDesc = "Spend 30 Resonance to shield nearby allies for 1,800 ticks; one-day cooldown.", action = GuardianWard };
            yield return new Command_Action { defaultLabel = "Consult VIGIL",
                defaultDesc = "Reflect with VIGIL, at most once per day.", action = Consult };
            if (!oathSworn && LumenWorldComponent.Instance?.Current != null)
                yield return new Command_Action { defaultLabel = "Swear guardianship oath",
                    defaultDesc = "Become the Luminant's sworn knight.", action = SwearOath };
        }

        public void RecordWitness(Pawn victim, Pawn instigator, float damage)
        {
            // Only a grounded, unbonded relic can witness trial combat.
            if (parent == null || !parent.Spawned || damage < 3f) return;
            ReleaseLostBearer();
            if (candidate == null || bearer != null) return;
            if (candidate.Dead || candidate.Map != parent.Map) return;
            bool victimIsFriendly = victim != null && victim != candidate &&
                                    victim.Faction == Faction.OfPlayer && victim.Spawned;
            bool instigatorHostile = instigator != null && instigator.Faction != null &&
                                     instigator.Faction.HostileTo(Faction.OfPlayer);
            if (!guardianWitness && victimIsFriendly && instigatorHostile &&
                victim.Position.InHorDistOf(candidate.Position, 6))
            {
                guardianWitness = true;
                Messages.Message("VIGIL: The Guardian has witnessed " + candidate.LabelShortCap +
                    " stand with a threatened ally.", MessageTypeDefOf.PositiveEvent, false);
            }
            if (!executionerWitness && victim != null && instigator == candidate &&
                victim.Faction != null && victim.Faction.HostileTo(Faction.OfPlayer))
            {
                executionerWitness = true;
                Messages.Message("VIGIL: The Executioner has witnessed " + candidate.LabelShortCap +
                    " face an enemy.", MessageTypeDefOf.PositiveEvent, false);
            }
        }

        private void OfferCandidate()
        {
            var options = parent.Map.mapPawns.FreeColonistsSpawned.Where(CanOffer)
                .Select(p => new FloatMenuOption(p.LabelShortCap, () => Petition(p))).ToList();
            if (options.Count == 0)
                options.Add(new FloatMenuOption("No eligible male colonists", null));
            Find.WindowStack.Add(new FloatMenu(options));
        }
        private void Petition(Pawn pawn)
        {
            if (bearer != null || !CanOffer(pawn)) return;
            candidate = pawn;
            candidateTick = Find.TickManager.TicksGame;
            guardianWitness = false; executionerWitness = false;
            Find.LetterStack.ReceiveLetter("VIGIL: The Three Witnesses",
                pawn.LabelShortCap + " petitions the sword. VIGIL will observe him while it lies on the ground. " +
                "Guardian: stand within six tiles when a colonist is harmed by a hostile. " +
                "Executioner: personally injure a hostile. Then choose the Arbiter's judgment. " +
                "No pledge can replace deeds.", LetterDefOf.PositiveEvent, pawn);
        }
        private void FinalVerdict()
        {
            if (candidate == null || candidate.Dead || !CanOffer(candidate) ||
                !guardianWitness || !executionerWitness || bearer != null) return;
            var options = new List<FloatMenuOption> {
                new FloatMenuOption("To defend those who depend on me", () => Bind("Preservation", 55)),
                new FloatMenuOption("To confront threats others cannot", () => Bind("Resolve", 55)),
                new FloatMenuOption("To decide when each is justified", () => Bind("Judgment", 60))
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }
        private void Bind(string oath, int initialAccord)
        {
            if (candidate == null || !CanOffer(candidate) || bearer != null ||
                !guardianWitness || !executionerWitness) return;
            bearer = candidate; candidate = null;
            accord = initialAccord; resonance = 40f; verdict = oath;
            Find.LetterStack.ReceiveLetter("VIGIL: The First Judgment",
                bearer.LabelShortCap + " has passed all three witnesses. The persona core awakens. " +
                "'You may carry me, Bearer. Let your actions give your oath meaning.' " +
                "Chosen principle: " + oath + ".", LetterDefOf.PositiveEvent, bearer);
        }
        public bool DevCompleteWitnesses()
        {
            if (!Prefs.DevMode || bearer != null || candidate == null || candidate.Dead) return false;
            guardianWitness = true;
            executionerWitness = true;
            return true;
        }

        public bool DevSwearOath()
        {
            if (!Prefs.DevMode || bearer == null || bearer.Dead ||
                LumenWorldComponent.Instance?.Current == null) return false;
            if (oathSworn) return true;
            SwearOath();
            return oathSworn;
        }

        public void GainResonance(float amount) => resonance = Mathf.Clamp(resonance + amount, 0f, 100f);
        private void GuardianWard()
        {
            if (bearer == null || bearer.Dead || bearer.equipment?.Primary != parent || !bearer.Spawned) return;
            int now = Find.TickManager.TicksGame;
            if (resonance < 30f || now < lastWardTick + 60000) {
                Messages.Message("Guardian's Ward requires 30 Resonance and one day of recovery.",
                    MessageTypeDefOf.RejectInput, false); return;
            }
            HediffDef def = VGL_DefOf.VGL_LuminousWard;
            int count = 0;
            foreach (Pawn p in bearer.Map.mapPawns.FreeColonistsSpawned)
                if (p.Position.InHorDistOf(bearer.Position, 4)) {
                    if (!p.health.hediffSet.HasHediff(def)) p.health.AddHediff(def);
                    count++;
                }
            if (count == 0) return;
            resonance -= 30f; lastWardTick = now;
            Messages.Message("VIGIL shields " + count + " allies.", MessageTypeDefOf.PositiveEvent, false);
        }
        private void Consult()
        {
            if (bearer == null || bearer.Dead) return;
            int now = Find.TickManager.TicksGame;
            if (now < lastConsultTick + 60000) {
                Messages.Message("VIGIL has said enough for today.", MessageTypeDefOf.RejectInput, false); return;
            }
            lastConsultTick = now;
            accord = Mathf.Clamp(accord + 2, 0, 100);
            Messages.Message("VIGIL: 'Do not mistake my silence for approval, Bearer.' (Accord +2)",
                MessageTypeDefOf.PositiveEvent, false);
        }
        private void SwearOath()
        {
            Pawn child = LumenWorldComponent.Instance?.Current;
            if (bearer == null || child == null || child.Dead || child.Faction != Faction.OfPlayer) return;
            oathSworn = true; accord = Mathf.Clamp(accord + 10, 0, 100);
            Find.LetterStack.ReceiveLetter("The Forgotten Bond",
                "VIGIL responds to " + child.LabelShortCap + "'s latent light. " + bearer.LabelShortCap +
                " swears to protect her. 'Guardian. Executioner. Arbiter. And now ... a reason.'",
                LetterDefOf.PositiveEvent, child);
        }
    }
}
