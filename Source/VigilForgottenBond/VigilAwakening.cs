using RimWorld;
using UnityEngine;
using Verse;

namespace VigilForgottenBond
{
    public enum VigilStage { Dormant = 0, Bound = 1, Awakened = 2, Ascendant = 3 }

    /// <summary>
    /// Gradual awakening. The silhouette never changes; the light does.
    ///   Dormant    no bearer                                   grey sprite
    ///   Bound      passed the Three Witnesses                  grey sprite, bond +2 melee hit
    ///   Awakened   Accord 50+ and 100 Resonance spent          cyan runes, +4 hit, ×1.15 melee damage
    ///   Ascendant  Accord 90+ and (oath sworn, or 400 spent)   bright edge, +6 hit, ×1.3 damage, +4 dodge,
    ///                                                          Final Mandate unlocked
    /// Stages never go backwards while the bearer lives; low Accord only limits abilities.
    /// </summary>
    public sealed partial class CompVigilPersona
    {
        public const int AwakenedAccord = 50, AscendantAccord = 90;
        public const float AwakenedSpent = 100f, AscendantSpentWithoutOath = 400f;

        public VigilStage Stage => bearer == null ? VigilStage.Dormant
            : (VigilStage)Mathf.Clamp(awakening, 1, 3);

        public static string StageLabel(VigilStage stage)
        {
            switch (stage)
            {
                case VigilStage.Dormant: return "Dormant";
                case VigilStage.Bound: return "Bound";
                case VigilStage.Awakened: return "Awakened";
                default: return "Ascendant";
            }
        }

        public string NextAwakeningHint()
        {
            switch (Stage)
            {
                case VigilStage.Bound:
                    return "To awaken: Accord " + accord + "/" + AwakenedAccord +
                           ", Resonance spent " + (int)lifetimeResonanceSpent + "/" + (int)AwakenedSpent + ".";
                case VigilStage.Awakened:
                    return "To ascend: Accord " + accord + "/" + AscendantAccord + ", and either the guardianship oath (" +
                           (oathSworn ? "sworn" : "not sworn") + ") or Resonance spent " +
                           (int)lifetimeResonanceSpent + "/" + (int)AscendantSpentWithoutOath + ".";
                case VigilStage.Ascendant:
                    return "Ascendant. Final Mandate is available.";
                default:
                    return "";
            }
        }

        private void CheckAwakening()
        {
            if (bearer == null || bearer.Dead) return;
            if (awakening < 2 && accord >= AwakenedAccord && lifetimeResonanceSpent >= AwakenedSpent)
                SetStage(2);
            if (awakening == 2 && accord >= AscendantAccord &&
                (oathSworn || lifetimeResonanceSpent >= AscendantSpentWithoutOath))
                SetStage(3);
        }

        private void SetStage(int stage)
        {
            awakening = Mathf.Clamp(stage, 1, 3);
            if (parent.Spawned) parent.DirtyMapMesh(parent.Map);
            if (IsWieldedBy(bearer)) UpdateBondHediff(bearer);

            string text = awakening == 2
                ? "Light runs through the fuller of the blade. VIGIL has Awakened.\n\n" +
                  "'I had forgotten what it is like to want to be used.'\n\n" +
                  "Melee damage ×1.15, hit chance +4. Abilities hit harder and Resonance builds faster."
                : "For a heartbeat the broken halo is whole. VIGIL has Ascended.\n\n" +
                  "'I was built to decide. I choose to stand with you.'\n\n" +
                  "Melee damage ×1.3, hit chance +6, dodge +4. Final Mandate is unlocked.";
            Find.LetterStack.ReceiveLetter("VIGIL " + StageLabel(Stage), text,
                LetterDefOf.PositiveEvent, parent.SpawnedOrAnyParentSpawned ? new LookTargets(parent) : new LookTargets(bearer));
        }

        // Bond hediff: present only while the bearer holds VIGIL; severity = stage.
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (CanWield(pawn)) UpdateBondHediff(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            Hediff h = pawn?.health?.hediffSet?.GetFirstHediffOfDef(VGL_DefOf.VGL_VigilBond);
            if (h != null) pawn.health.RemoveHediff(h);
        }

        private void UpdateBondHediff(Pawn pawn)
        {
            if (pawn?.health == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(VGL_DefOf.VGL_VigilBond)
                       ?? pawn.health.AddHediff(VGL_DefOf.VGL_VigilBond);
            h.Severity = Mathf.Clamp(awakening, 1, 3);
        }

        public void DevAdvanceStage()
        {
            if (bearer == null || awakening >= 3) return;
            SetStage(awakening + 1);
        }
    }

    /// <summary>VIGIL's thing class: picks the sprite for the current awakening stage.</summary>
    public class VigilWeapon : ThingWithComps
    {
        private const string AwakenedPath = "Vigil/Weapons/Vigil_Awakened";
        private const string AscendantPath = "Vigil/Weapons/Vigil_Ascendant";
        private static Graphic awakenedGraphic, ascendantGraphic;
        private CompVigilPersona persona;

        private CompVigilPersona Persona => persona ?? (persona = GetComp<CompVigilPersona>());

        public override Graphic Graphic
        {
            get
            {
                VigilStage stage = Persona?.Stage ?? VigilStage.Dormant;
                if (stage == VigilStage.Ascendant)
                    return ascendantGraphic ?? (ascendantGraphic = StageGraphic(AscendantPath));
                if (stage == VigilStage.Awakened)
                    return awakenedGraphic ?? (awakenedGraphic = StageGraphic(AwakenedPath));
                return base.Graphic;
            }
        }

        private Graphic StageGraphic(string path)
        {
            GraphicData gd = def.graphicData;
            Shader shader = gd.shaderType?.Shader ?? ShaderDatabase.Cutout;
            return GraphicDatabase.Get<Graphic_Single>(path, shader, gd.drawSize, Color.white);
        }
    }
}
