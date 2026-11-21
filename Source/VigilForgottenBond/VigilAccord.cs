using RimWorld;
using UnityEngine;
using Verse;

namespace VigilForgottenBond
{
    public enum AccordTier { Fractured, Contested, Cooperative, Trusted, TrueAccord }

    /// <summary>
    /// Accord (0–100) is VIGIL's trust in its bearer; Resonance (0–100) is combat power.
    ///
    /// Tier effects (low trust is unpleasant but never breaks the bond):
    ///   Fractured  0–24   VIGIL refuses Sever, Verdict and Final Mandate.
    ///   Contested  25–49  Everything works.
    ///   Cooperative 50–74 Last Stand passive enabled. Needed to reach Awakened.
    ///   Trusted    75–89  Ability costs −20%.
    ///   True Accord 90+   Needed to reach Ascendant.
    ///
    /// Accord moves on deeds, not on brutality: daily counsel, the guardianship oath,
    /// shielding several allies, ending a threat (once a day), and losing an ally the
    /// bearer was close enough to protect.
    /// </summary>
    public sealed partial class CompVigilPersona
    {
        public int Accord => accord;
        public float Resonance => resonance;

        public AccordTier Tier => TierFor(accord);

        public static AccordTier TierFor(int value) =>
            value < 25 ? AccordTier.Fractured :
            value < 50 ? AccordTier.Contested :
            value < 75 ? AccordTier.Cooperative :
            value < 90 ? AccordTier.Trusted : AccordTier.TrueAccord;

        public static string TierLabel(AccordTier tier)
        {
            switch (tier)
            {
                case AccordTier.Fractured: return "Fractured";
                case AccordTier.Contested: return "Contested";
                case AccordTier.Cooperative: return "Cooperative";
                case AccordTier.Trusted: return "Trusted";
                default: return "True Accord";
            }
        }

        public void AdjustAccord(int delta, string reason)
        {
            if (bearer == null || delta == 0) return;
            AccordTier before = Tier;
            accord = Mathf.Clamp(accord + delta, 0, 100);
            AccordTier after = Tier;
            Messages.Message("VIGIL: Accord " + (delta > 0 ? "+" : "") + delta + " (" + reason + ").",
                bearer, delta > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent, false);
            if (after != before) AnnounceTier(before, after);
            CheckAwakening();
        }

        private void AnnounceTier(AccordTier before, AccordTier after)
        {
            bool rising = after > before;
            string line;
            switch (after)
            {
                case AccordTier.Fractured:
                    line = "'I will not lend my edge to this. Not until I understand you again.'\n\n" +
                           "VIGIL now refuses Sever, Verdict and Final Mandate. Aegis still answers.";
                    break;
                case AccordTier.Contested:
                    line = rising
                        ? "'Very well. I will fight beside you. I have not yet decided whether I trust you.'"
                        : "'You are not the man I thought you were. Prove me wrong.'\n\nLast Stand will no longer answer.";
                    break;
                case AccordTier.Cooperative:
                    line = rising
                        ? "'You keep your word more often than you break it. That is rarer than you think.'\n\n" +
                          "Last Stand: when you are badly hurt, VIGIL will hold you up once every two days."
                        : "'I still stand with you. Less certainly than I did.'\n\nAbility costs return to normal.";
                    break;
                case AccordTier.Trusted:
                    line = rising
                        ? "'I have stopped counting your mistakes. I count your choices now.'\n\nAbility costs −20%."
                        : "'Something has changed between us. I will wait to see what.'";
                    break;
                default:
                    line = "'Not bearer. Not candidate. " + bearer.LabelShortCap + ".'\n\n" +
                           "VIGIL speaks to him as an equal. Ascendance is within reach.";
                    break;
            }
            Find.LetterStack.ReceiveLetter("VIGIL: " + TierLabel(after),
                "VIGIL's trust in " + bearer.LabelShortCap + " is now " + TierLabel(after) + ".\n\n" + line,
                rising ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent, bearer);
        }

        public void GainResonance(float amount)
        {
            if (bearer == null || amount <= 0f) return;
            resonance = Mathf.Clamp(resonance + amount, 0f, 100f);
        }

        // --- Events (from Patches.cs) ------------------------------------------------

        public void Notify_BearerTookDamage(float amount) => GainResonance(amount * 0.5f);

        public void Notify_BearerDealtDamage(Pawn victim, float amount)
        {
            if (victim == null || !victim.HostileTo(bearer)) return;
            GainResonance(amount * 0.4f * StageResonanceFactor);
        }

        private int lastThreatAccordTick = -999999;

        public void Notify_BearerKilled(Pawn victim)
        {
            if (victim == null || bearer == null) return;
            int now = Find.TickManager.TicksGame;
            GainResonance(10f);
            // Executioner: ending a real threat earns trust, at most once per day.
            bool threat = victim.RaceProps.Humanlike || victim.RaceProps.IsMechanoid ||
                          victim.RaceProps.IsAnomalyEntity;
            if (threat && now >= lastThreatAccordTick + GenDate.TicksPerDay)
            {
                lastThreatAccordTick = now;
                AdjustAccord(1, "a threat ended");
            }
        }

        public void Notify_AllyDied(Pawn ally)
        {
            if (bearer == null || ally == null || ally == bearer) return;
            if (!bearer.Spawned || ally.MapHeld != bearer.Map) return;
            if (!ally.PositionHeld.InHorDistOf(bearer.Position, 15f)) return;
            AdjustAccord(-2, ally.LabelShortCap + " died within reach");
        }

        // --- Dialogue ----------------------------------------------------------------

        private static readonly string[][] ConsultLines =
        {
            new[] { // Fractured
                "'Speak, if you must. I am listening for the lie.'",
                "'You carry me. That does not mean I agree with you.'",
                "'I have outlived better men than you. I intend to outlive you too.'",
            },
            new[] { // Contested
                "'Do not mistake my silence for approval, Bearer.'",
                "'You hesitate before you strike. Good. Keep that.'",
                "'Your colony is fragile. So were the others.'",
            },
            new[] { // Cooperative
                "'The Guardian in me approves of your choices this week. The Executioner is less impressed.'",
                "'Rest. I will keep watch. I do not sleep, and you clearly need to.'",
                "'You asked what I was before. Ask again some other day.'",
            },
            new[] { // Trusted
                "'I remember the bearer before you. You laugh more than he did.'",
                "'The Concordance thought it could protect everyone by deciding for them. Do not make its mistake.'",
                "'If I am wrong about you, I would like to be wrong slowly.'",
            },
            new[] { // True Accord
                "'Ask me anything. I will answer truthfully, even where it costs me.'",
                "'I was made to judge. With you I would rather understand.'",
                "'Whatever comes for her, it comes through both of us.'",
            },
        };

        private string RandomConsultLine() => ConsultLines[(int)Tier].RandomElement();
    }
}
