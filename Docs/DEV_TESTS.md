# VIGIL alpha v0.9 — Development tests

This build adds **development launchers**, not the full Silent Vault or Anomaly story campaign.
The test quests exist to speed up repeatable manual testing of core mechanics.

## Developer menu: Generate quest

1. Enable Development mode under RimWorld Options.
2. Load a disposable RimWorld 1.6 colony with compiled VIGIL installed.
3. Open **Debug actions > Quests > Generate quest**.
4. Search `VGL_Q_` or `VIGIL`, and pick one of these root definitions:

| Quest def | When accepted, what it does | Precondition |
| --- | --- | --- |
| VGL_Q_TheSilentVault_TEST | Spawns ONE VIGIL sword on the player map | No previously spawned sword |
| VGL_Q_ThreeWitnesses_TEST | Sets Guardian and Executioner witness flags | Sword on ground with nominated male candidate |
| VGL_Q_FirstLight_TEST | Grants/rechecks first Luminist power | Current female Luminant at least age 7 |
| VGL_Q_ForgottenBond_TEST | Swears the knight's oath | Bound sword equipped and living Luminant |
| VGL_Q_Continuance_TEST | Ends current one-year wait immediately | Previous Luminant dead; waiting for succession |

These are quest-system *test fixtures*. They end without rewards immediately after their action. Their names explicitly contain `[TEST]`.

## Direct debug actions

Under **Debug actions > VIGIL: Tests**, find the same launchers, plus:

* `Assign Luminant to existing female pawn`: choose a female colonist on the map. Used to avoid waiting for birth. Works on an adult too; only one living Luminant is allowed.

**Recommended fast sequence:**
1. Run `The Silent Vault [TEST]`, click the sword, petition a male colonist.
2. Run `The Three Witnesses [TEST]`, click sword > Arbiter's Judgment > choose a verdict.
3. Select a female colonist at least age 7 with `Assign Luminant to existing female pawn`.
4. Run `The First Light [TEST]`; open the pawn's Psycasts tab and check `luminist`.
5. **The v0.6 playtest bug.** Pick a DIFFERENT psycaster and open her Psycasts tab with Dev Mode ON.
   VPE's dev mode lets you press Unlock on any path, so press it on `luminist`. Expected in v0.8:
   * the path is moved aside again as soon as her health changes (VPE's `ensureLockRequirement`), and
   * any Luminist power she was given shows greyed out with "Locked: only the living Luminant…", and won't cast.
   * With Dev Mode OFF, the Unlock button should not appear at all for her.
   * Dev Mode on: the log shows `[VIGIL] Blocked Luminist ability …` when something tries to grant one.
6. Equip the bonded male with the sword, run `The Forgotten Bond [TEST]`.
7. Save, reload and repeat test (5), verifying trait, mantle and oath state survive.
8. **Dead bearer (fixed in v0.8).** Kill the bonded male (dev: Kill). Strip the sword from his corpse.
   Expected: a "VIGIL Remembers" letter; the sword's inspect panel says Dormant and lists him under
   "Remembers"; `Petition VIGIL` is available again for another male colonist.
9. Check the log on startup for `[VIGIL] Forgotten Bond alpha v0.8 loaded: N patches applied`.
   Any `Patch … failed` line names exactly which hook doesn't match your game version.

## v0.9: sword abilities, Accord, awakening

Setup: spawn VIGIL, bond a male colonist (Witnesses → Arbiter's Judgment) and equip it. Spawn some enemies with
Debug actions → Spawn pawn (e.g. a raider or a scyther), or trigger a small raid.

10. **Status panel and bond.** With the bearer selected, there's a VIGIL panel with Resonance and Accord bars
    next to Aegis, Sever, Verdict and Consult. His Health tab shows "VIGIL's bond (bound)". Unequip the sword
    and the bond disappears; re-equip and it returns.
11. **Resonance.** Fight in melee. Resonance should rise when he hits or is hit, and jump by 10 on a kill.
12. **Aegis.** Use it with 2+ colonists nearby. They get "aegis" for 30 s, take visibly less damage, and Accord
    gains +1. The button then shows a 1-day cooldown.
13. **Sever.** Target an adjacent enemy: a big cut. Try a non-adjacent target and you get a "must be adjacent" message.
14. **Verdict.** Target an enemy within 8 tiles: stunned, then "judged" in its Health tab (slower, less accurate).
15. **Accord tiers.** Use VIGIL: Tests → `Sword: Accord -25` until Fractured. You get a letter, and Sever and Verdict
    are greyed out ("VIGIL refuses"); Aegis still works. Raise it back with `Accord +25`.
    Consult at different tiers and the lines change.
16. **Last Stand.** At Cooperative (50+), injure the bearer below ~45% health (dev: Damage) and hit him again.
    "Last Stand" appears, the hit is much smaller, and he gets the "last stand" hediff. It won't fire again for 2 days.
17. **Awakening.** `Sword: Fill Resonance` and use abilities (`Reset ability cooldowns` helps) until 100 is spent
    with Accord ≥ 50: you get the "VIGIL Awakened" letter, the sword sprite gains cyan runes (on the ground and in
    hand), and the bond becomes "awakened". Raise Accord to 90+ and swear the oath (or spend 400): Ascendant, with
    a brighter sprite and the **Final Mandate** button. `Sword: Advance awakening stage` skips ahead.
18. **Final Mandate.** Surrounded by enemies: all within 6 tiles are struck and stunned, and nearby allies get Aegis.
19. **Ally death.** Kill a colonist (dev: Kill) standing near the bearer: Accord −2.
20. **Save/reload** keeps Accord, Resonance, stage, cooldowns and the sprite.
21. **Old save from v0.8.1**: load it and a bonded sword should show as Bound, with nothing broken.

## What is NOT implemented

* The Silent Vault's actual ruins/map generation and lore investigation.
* A full questline with world-map sites and selectable story outcomes.
* Birth/resurrection/succession beyond the alpha logic.
* Complete Luminist psycast tree (only Ward, Second Sight and Radiant Spark are defined).

## If the quest doesn't appear

Verify the C# DLL exists at `Assemblies/VigilForgottenBond.dll`, there are no red XML errors,
all dependencies are enabled, and the **source** mod (not the art-only Weapon Preview) is loaded.
The XML quest roots reference C# classes; without the compiled DLL they cannot initialize.

## Testing limits

No compile or runtime test was possible in the authoring environment. v0.8's VEF/VPE calls were
checked by hand against the public VEF and VPE source on GitHub (VPE's 1.6 DLL contains the same
lock fields), but that is not the same as compiling. Consult `Player.log` and
supply compiler errors if any C# API signature differs from your installed builds.
