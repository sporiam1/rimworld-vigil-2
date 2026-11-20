# VIGIL alpha v0.7 — Development quest tests

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
5. Draft a DIFFERENT pawn and try to give it `Luminous Ward` by dev mode: its cast action must remain disabled even if the ability is present.
6. Equip the bonded male with the sword, run `The Forgotten Bond [TEST]`.
7. Save, reload and repeat test (5), verifying trait, mantle and oath state survive.

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

No full game compile or runtime test was possible in the authoring environment: the exact
RimWorld 1.6 / VPE / VEF installed assemblies were not available. Consult `Player.log` and
supply compiler errors if any C# API signature differs from your installed builds.
