using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VigilForgottenBond
{
    // One mantle across the game world. The trait/hediff are display-only, and
    // the saved pawn reference remains authoritative through caravans and death.
    public sealed class LumenWorldComponent : WorldComponent
    {
        private Pawn currentLuminant;
        // Names, not Pawn references, so a cleaned-up dead pawn can't break loading.
        private List<string> predecessors = new List<string>();
        private List<Pawn> observedInfants = new List<Pawn>();
        private bool awaitingSuccession;
        private int successionEligibleTick;
        private int lastShieldTick = -999999;
        private bool firstLightAnnounced;
        private bool vigilSpawned;
        private bool initialized;
        private int initializedTick;

        public LumenWorldComponent(World world) : base(world) { }
        public static LumenWorldComponent Instance => Find.World?.GetComponent<LumenWorldComponent>();
        public Pawn Current => currentLuminant;
        public bool IsLuminant(Pawn p) => p != null && p == currentLuminant && !p.Dead;
        public bool AwaitingSuccession => awaitingSuccession;
        public int SuccessionEligibleTick => successionEligibleTick;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref currentLuminant, "lumenHolder");
            Scribe_Collections.Look(ref predecessors, "lumenPredecessorNames", LookMode.Value);
            Scribe_Collections.Look(ref observedInfants, "lumenObservedInfants", LookMode.Reference);
            Scribe_Values.Look(ref awaitingSuccession, "lumenAwaitingSuccessor");
            Scribe_Values.Look(ref successionEligibleTick, "lumenEligibleAtTick");
            Scribe_Values.Look(ref lastShieldTick, "lumenLastShieldTick", -999999);
            Scribe_Values.Look(ref firstLightAnnounced, "lumenFirstLightAnnounced");
            Scribe_Values.Look(ref vigilSpawned, "vigilRelicSpawned");
            Scribe_Values.Look(ref initialized, "lumenInitialized");
            Scribe_Values.Look(ref initializedTick, "lumenInitializedTick");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                if (predecessors == null) predecessors = new List<string>();
                if (observedInfants == null) observedInfants = new List<Pawn>();
            }
        }

        public override void WorldComponentTick()
        {
            if (Find.TickManager == null) return;
            int now = Find.TickManager.TicksGame;
            if (now % 120 != 0) return;
            if (!initialized) { initialized = true; initializedTick = now; }
            if (currentLuminant != null) {
                if (currentLuminant.Dead) {
                    if (!awaitingSuccession) StartSuccession(now);
                } else {
                    if (awaitingSuccession) { awaitingSuccession = false; successionEligibleTick = 0; }
                    EnsureMantleAppearance(currentLuminant);
                    TryFirstLight(currentLuminant);
                }
            }
            // Fallback for modded birth paths, not our main birth hook.
            DetectNewborns(now);
            TrySpawnRelic(now);
        }

        private void DetectNewborns(int now)
        {
            // Avoid assigning the mantle to an infant already present when the mod is installed.
            // All newborns are registered while the succession timer runs.
            bool canSelect = now - initializedTick > 60000 &&
                (currentLuminant == null || currentLuminant.Dead) &&
                (!awaitingSuccession || now >= successionEligibleTick);
            foreach (Map map in Find.Maps) {
                if (!map.IsPlayerHome) continue;
                foreach (Pawn baby in map.mapPawns.AllPawnsSpawned) {
                    if (baby?.ageTracker == null || !baby.RaceProps.Humanlike ||
                        baby.ageTracker.AgeBiologicalTicks > 2000L) continue;
                    if (observedInfants.Contains(baby)) continue;
                    observedInfants.Add(baby);
                    if (canSelect && TryAssign(baby)) return;
                }
            }
            // Bounded reference tracking so a long-running colony does not accumulate infants.
            if (observedInfants.Count > 80)
                observedInfants.RemoveAll(p => p == null || p.Dead || p.ageTracker?.AgeBiologicalTicks > 60000L);
        }

        public bool TryAssign(Pawn baby)
        {
            if (baby == null || baby.Dead || baby.Faction != Faction.OfPlayer ||
                baby.gender != Gender.Female || !baby.RaceProps.Humanlike ||
                baby.ageTracker == null || baby.ageTracker.AgeBiologicalTicks > 60000L) return false;
            if (currentLuminant != null && !currentLuminant.Dead) return false;
            // A newborn can arrive in the exact tick a bearer dies, before the next
            // WorldComponentTick. Do not allow that to bypass the one-year delay.
            if (currentLuminant != null && currentLuminant.Dead && !awaitingSuccession) {
                StartSuccession(Find.TickManager.TicksGame);
                return false;
            }
            if (awaitingSuccession && Find.TickManager.TicksGame < successionEligibleTick) return false;
            if (currentLuminant != null) {
                RememberPredecessor(currentLuminant);
                RemoveMantleAppearance(currentLuminant);
            }
            currentLuminant = baby;
            if (!observedInfants.Contains(baby)) observedInfants.Add(baby);
            awaitingSuccession = false; successionEligibleTick = 0; firstLightAnnounced = false;
            lastShieldTick = -999999;
            EnsureMantleAppearance(baby);
            Find.LetterStack.ReceiveLetter("A Light Returns",
                baby.LabelShortCap + " carries the world's sole Luminant mantle. Her presence is marked " +
                "in the Bio and Health tabs. Her first deliberate psycast awakens at age seven.",
                LetterDefOf.PositiveEvent, baby);
            return true;
        }


        // Developer-only: test an EXISTING female colonist (including adults) rather
        // than waiting for a pregnancy. Never creates a second simultaneous holder.
        public bool DevAssign(Pawn pawn)
        {
            if (!Prefs.DevMode || pawn == null || pawn.Dead || pawn.gender != Gender.Female ||
                !pawn.RaceProps.Humanlike || pawn.Faction != Faction.OfPlayer ||
                pawn.ageTracker == null) return false;
            if (currentLuminant != null && !currentLuminant.Dead && currentLuminant != pawn)
                return false;
            if (currentLuminant != null && currentLuminant != pawn)
            {
                RememberPredecessor(currentLuminant);
                RemoveMantleAppearance(currentLuminant);
            }
            currentLuminant = pawn;
            awaitingSuccession = false;
            successionEligibleTick = 0;
            firstLightAnnounced = false;
            lastShieldTick = -999999;
            EnsureMantleAppearance(pawn);
            TryFirstLight(pawn);
            Find.LetterStack.ReceiveLetter("DEV: Luminant mantle assigned",
                pawn.LabelShortCap + " is the sole living Luminant for this test. " +
                "This action intentionally bypasses the newborn/one-year requirements.",
                LetterDefOf.PositiveEvent, pawn);
            return true;
        }

        public bool DevFirstLight()
        {
            if (!Prefs.DevMode || currentLuminant == null || currentLuminant.Dead ||
                currentLuminant.ageTracker.AgeBiologicalYears < 7) return false;
            firstLightAnnounced = false;
            TryFirstLight(currentLuminant);
            return firstLightAnnounced;
        }

        public bool DevFinishSuccessionWait()
        {
            if (!Prefs.DevMode || !awaitingSuccession ||
                (currentLuminant != null && !currentLuminant.Dead)) return false;
            successionEligibleTick = Find.TickManager.TicksGame;
            return true;
        }

        public bool DevSpawnRelic(Map map)
        {
            if (!Prefs.DevMode || map == null || !map.IsPlayerHome) return false;
            ThingDef def = VGL_DefOf.VGL_VigilSword;
            // One authentic copy: do not debug-duplicate a spawned weapon.
            foreach (Map m in Find.Maps)
                if (m.listerThings.ThingsOfDef(def).Count > 0) return false;
            if (vigilSpawned) return false;
            var cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            Thing sword = ThingMaker.MakeThing(def);
            GenSpawn.Spawn(sword, cell, map);
            vigilSpawned = true;
            Find.LetterStack.ReceiveLetter("The Silent Vault — test relic",
                "A sealed archotech blade has appeared for testing. Select it and petition " +
                "a male candidate for the Three Witnesses.", LetterDefOf.PositiveEvent, sword);
            return true;
        }

        public IReadOnlyList<string> Predecessors => predecessors;

        private void RememberPredecessor(Pawn p)
        {
            string name = p?.Name?.ToStringFull ?? p?.LabelShort;
            if (!name.NullOrEmpty() && !predecessors.Contains(name)) predecessors.Add(name);
        }

        private void StartSuccession(int now)
        {
            awaitingSuccession = true;
            successionEligibleTick = now + GenDate.TicksPerYear; // one RimWorld year (60 days)
            Find.LetterStack.ReceiveLetter("The Light Has Fallen",
                "The Luminant has died. After one RimWorld year, the next eligible newborn girl " +
                "may inherit the mantle. Resurrection before transfer restores the original bearer.",
                LetterDefOf.NegativeEvent);
        }
        public bool TryChildShield(Pawn p)
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            if (!IsLuminant(p) || p.ageTracker.AgeBiologicalYears >= 18 ||
                now < lastShieldTick + 120000) return false;
            lastShieldTick = now;
            return true;
        }
        public void TryFirstLight(Pawn p)
        {
            if (!IsLuminant(p) || p.ageTracker.AgeBiologicalYears < 7) return;
            if (!VigilLuministPsycasts.EnsureFirstAbility(p) || firstLightAnnounced) return;
            firstLightAnnounced = true;
            Find.LetterStack.ReceiveLetter("The First Light",
                p.LabelShortCap + " dreams of a broken halo. Luminous Ward is now available " +
                "through the Luminist path of Vanilla Psycasts Expanded.",
                LetterDefOf.PositiveEvent, p);
        }
        private static void EnsureMantleAppearance(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;
            TraitDef trait = VGL_DefOf.VGL_LuminantTrait;
            if (pawn.story?.traits != null && !pawn.story.traits.HasTrait(trait))
                pawn.story.traits.GainTrait(new Trait(trait, 0));
            HediffDef def = VGL_DefOf.VGL_LuminantMantle;
            if (pawn.health?.hediffSet == null) return;
            var h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (h == null) h = pawn.health.AddHediff(def);
            int age = pawn.ageTracker?.AgeBiologicalYears ?? 0;
            h.Severity = age < 7 ? 0.1f : age < 13 ? 1.1f : age < 18 ? 2.1f : 3.1f;
        }
        private static void RemoveMantleAppearance(Pawn pawn)
        {
            if (pawn == null) return;
            Trait t = pawn.story?.traits?.allTraits?.FirstOrDefault(x => x.def == VGL_DefOf.VGL_LuminantTrait);
            if (t != null) pawn.story.traits.RemoveTrait(t);
            Hediff h = pawn.health?.hediffSet?.GetFirstHediffOfDef(VGL_DefOf.VGL_LuminantMantle);
            if (h != null) pawn.health.RemoveHediff(h);
        }
        private void TrySpawnRelic(int now)
        {
            // Narrative relic delivery remains a test substitute until Silent Vault
            // is a real generated location; debug quest calls DevSpawnRelic directly.
            if (vigilSpawned || now < 900000) return;
            if (!Prefs.DevMode)
            {
                // Ordinary play is unaffected: no dev artifact automatically spawned.
                return;
            }
        }
    }
}
