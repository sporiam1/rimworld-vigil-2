# Alpha v0.6 — Three Witnesses and the Continuance

## Source changes

- Bonding now requires a male candidate to demonstrate two observable events: the Guardian witness when an ally is harmed by a hostile within six cells of the candidate, and the Executioner witness when the candidate damages a hostile. A final Arbiter choice records the oath and establishes the bearer.
- A birth-related Harmony hook (`PawnUtility.TrySpawnHatchedOrBornPawn`) attempts immediate mantle assignment to a qualifying newborn. A periodic map scan remains as a fallback for mods that bypass vanilla spawning. Pre-existing infants are tracked before fallback selection; this is still not a fully verified birth-only detector.
- Reincarnation cooldown remains 3,600,000 ticks (60 in-game days). Save state now includes observed infants and trial progress. A resurrected former Luminant is not allowed to overwrite a newly assigned successor.
- VIGIL's full rescue and anti-Void quests are *not* implemented. The 15-day relic appearance remains an explicitly temporary test event.
- Luminous Ward and first-tier VPE grant are still awaiting real DLL compile and in-game validation; no claim of working gameplay is made.

## How to complete the prototype Three Witnesses

1. Start the compiled mod on a **new throwaway save**. Use Dev Mode to spawn `VIGIL` on the ground, or wait 15 in-game days for the alpha event.
2. Click the sword, select **Petition VIGIL**, and choose a male colonist age 13+.
3. Keep the sword on the ground. For **Guardian**, have that candidate within six tiles when a hostile injures a fellow colonist.
4. For **Executioner**, have the candidate personally damage a hostile pawn.
5. Check the sword inspect pane: both witnesses should read `witnessed`. Click **The Arbiter's Judgment**, choose an oath, and equip the weapon on the chosen bearer.
6. In Dev Mode only, **DEV: Complete witnesses** allows quick UI testing of the final oath dialog. This bypasses combat and is not a normal gameplay action.

## Known hard blockers

The build environment does not contain .NET, RimWorld's `Assembly-CSharp.dll`, the Harmony, Vanilla Expanded Framework, or Vanilla Psycasts Expanded DLLs. XML validation cannot prove the C# is compatible. Run `Tools/VerifyFiles.py`, compile locally using `Tools/Build-Windows.ps1`, then inspect `Player.log`. API and XML variations between dependency versions remain possible.

### Gameplay limits

- The Guardian witness tracks *proximity to endangered allies*, not whether a hit was actually intercepted. It is a first-generation trial mechanic.
- The on-ground weapon is currently required for trial progress; an actual altar/quest map comes later.
- The scanner covers player home maps, and birth hooks can be bypassed by major childbirth overhauls. The mantle is guarded by the world registry but compatibility testing is still required.
- Uniqueness applies to the world mantle, not illicit duplicates debug-spawned into the world. The relic's legendary quest spawn uses a saved flag only.
