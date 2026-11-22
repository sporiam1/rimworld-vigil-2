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

    /// <summary>
    /// VIGIL's persona core. This file: bearer, the Three Witnesses trial, saving.
    /// Other parts of the class:
    ///   VigilAccord.cs     – Accord tiers, Resonance gain, dialogue
    ///   VigilAbilities.cs  – Aegis, Sever, Verdict, Final Mandate, Last Stand, gizmos
    ///   VigilAwakening.cs  – Dormant → Bound → Awakened → Ascendant, bond hediff
    /// </summary>
    public sealed partial class CompVigilPersona : ThingComp
    {
        private Pawn bearer;
        private Pawn candidate;
        private bool guardianWitness;
        private bool executionerWitness;
        private int candidateTick = -1;
        private int accord = 50;
        private float resonance = 40f;
        private int awakening = 0;
        private bool oathSworn;
        private string verdict = "";
        // Names, not Pawn references: dead pawns can be cleaned out of the world,
        // and a dangling reference would just log a load error.
        private List<string> rememberedBearers = new List<string>();

        public Pawn Bearer => bearer;
        public Pawn Candidate => candidate;
        public bool OathSworn => oathSworn;
        public bool HasGuardianWitness => guardianWitness;
        public bool HasExecutionerWitness => executionerWitness;
        public IReadOnlyList<string> RememberedBearers => rememberedBearers;

        public bool CanWield(Pawn pawn)
        {
            ReleaseLostBearer();
            return pawn != null && pawn == bearer && !pawn.Dead && pawn.gender == Gender.Male;
        }

        /// <summary>True when this pawn is the bearer AND is holding VIGIL right now.</summary>
        public bool IsWieldedBy(Pawn pawn) => CanWield(pawn) && pawn.equipment?.Primary == parent;

        /// <summary>
        /// A dead bearer releases the sword. Checked lazily wherever the sword is
        /// inspected, offered, or equipped. If the bearer is resurrected before anyone
        /// handles the sword, he simply keeps it, the same rule as the Luminant mantle.
        /// </summary>
        private void ReleaseLostBearer()
        {
            if (bearer == null || !bearer.Dead) return;
            Pawn fallen = bearer;
            RemoveBondHediff(fallen);
            string name = fallen.Name?.ToStringFull ?? fallen.LabelShort;
            if (!rememberedBearers.Contains(name)) rememberedBearers.Add(name);
            bearer = null;
            candidate = null;
            guardianWitness = false;
            executionerWitness = false;
            oathSworn = false;
            verdict = "";
            awakening = 0;
            resonance = 0f;
            accord = 50;
            lifetimeResonanceSpent = 0f;
            ResetCooldowns();
            if (parent.Spawned) parent.DirtyMapMesh(parent.Map);
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
            Scribe_Values.Look(ref awakening, "vigilAwakening", 0);
            Scribe_Values.Look(ref oathSworn, "vigilOathSworn", false);
            Scribe_Values.Look(ref verdict, "vigilVerdict", "");
            Scribe_Collections.Look(ref rememberedBearers, "vigilRememberedBearers", LookMode.Value);
            ExposeAbilityData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (rememberedBearers == null) rememberedBearers = new List<string>();
                // v0.8 saves stored a bound sword as awakening = 1 already; an unbound one
                // may say 1 too, which Stage ignores when there is no bearer.
                if (bearer != null && awakening < 1) awakening = 1;
            }
        }

        public override string CompInspectStringExtra()
        {
            ReleaseLostBearer();
            string memory = rememberedBearers.Count == 0 ? "" :
                "\nRemembers: " + string.Join(", ", rememberedBearers);
            if (bearer != null)
                return "VIGIL (" + StageLabel(Stage) + ") — Bearer: " + bearer.LabelShortCap +
                       (oathSworn ? ", Oathbound" : "") +
                       "\nAccord " + accord + "/100 (" + TierLabel(Tier) + ") | Resonance " + (int)resonance + "/100" +
                       (verdict == "" ? "" : " | Principle: " + verdict) +
                       "\n" + NextAwakeningHint() + memory;
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
                icon = VigilTex.Oath,
                action = OfferCandidate
            };
            if (candidate != null && guardianWitness && executionerWitness)
                yield return new Command_Action { defaultLabel = "The Arbiter's Judgment",
                    defaultDesc = "The candidate states his purpose. The sword chooses whether to bond.",
                    icon = VigilTex.Verdict, action = FinalVerdict };
            if (Prefs.DevMode && candidate != null)
                yield return new Command_Action { defaultLabel = "DEV: Complete witnesses",
                    defaultDesc = "Test-only: complete the Guardian and Executioner witness flags.", action = () => {
                        guardianWitness = true; executionerWitness = true;
                        Messages.Message("VIGIL: both witnesses marked for testing.", MessageTypeDefOf.PositiveEvent, false);
                    }};
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
            awakening = 1;
            lifetimeResonanceSpent = 0f;
            ResetCooldowns();
            if (parent.Spawned) parent.DirtyMapMesh(parent.Map);
            UpdateBondHediff(bearer, IsWieldedBy(bearer));
            Find.LetterStack.ReceiveLetter("VIGIL: The First Judgment",
                bearer.LabelShortCap + " has passed all three witnesses. The persona core awakens. " +
                "'You may carry me, Bearer. Let your actions give your oath meaning.'\n\n" +
                "Chosen principle: " + oath + ".\n\nWhile he carries VIGIL he gains its abilities. " +
                "Resonance builds in combat and powers them; Accord is VIGIL's trust in him. " +
                "Both shape how far the sword awakens.", LetterDefOf.PositiveEvent, bearer);
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

        private void SwearOath()
        {
            Pawn child = LumenWorldComponent.Instance?.Current;
            if (bearer == null || child == null || child.Dead || child.Faction != Faction.OfPlayer) return;
            oathSworn = true;
            Find.LetterStack.ReceiveLetter("The Forgotten Bond",
                "VIGIL responds to " + child.LabelShortCap + "'s latent light. " + bearer.LabelShortCap +
                " swears to protect her. 'Guardian. Executioner. Arbiter. And now ... a reason.'",
                LetterDefOf.PositiveEvent, child);
            AdjustAccord(10, "the oath of guardianship");
        }

        // Shared by the debug menu: finds the one VIGIL, equipped or on the ground.
        public static CompVigilPersona FindAny()
        {
            ThingDef def = VGL_DefOf.VGL_VigilSword;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    var c = p.equipment?.Primary?.GetComp<CompVigilPersona>();
                    if (c != null) return c;
                }
                var t = map.listerThings.ThingsOfDef(def).FirstOrDefault();
                if (t != null) return t.TryGetComp<CompVigilPersona>();
            }
            return null;
        }
    }
}
