# Alpha v0.7 — Cast Gate & Quest Test Harness

## Bug: non-Luminants casting Luminist powers

**v0.6** guarded `PsycasterPathDef.CanPawnUnlock` and `CompAbilities.GiveAbility`, but
these controls did not prohibit an ability **already present** on another pawn from casting.

**v0.7** uses custom VEF ability classes:
- `Ability_LuministSupport` (Ward, Second Sight),
- `Ability_LuministProjectile` (Radiant Spark).

Both reject non-Luminant casts using `IsEnabledForPawn` and early-return in `Cast`,
checking the world registry and age >= 7. Path unlock and grant checks remain.
This provides a defense even when a dev tool or another mod grants the ability.

## Dev quest launchers

5 `QuestScriptDef` entries with `rootSelectionWeight=0` + `isRootSpecial=true`.
Generate quest menu -> accept -> execute one scenario and finish the test quest.
Additional `DebugAction` category `VIGIL: Tests` for direct launches and adult test assignment.

## Safety and limitations

* `DevAssign` cannot override an existing living Luminant.
* No automatic duplicating of relic from debug launchers.
* Actual narrative quests have **not** been shipped.
* C# source still must be compiled against the user's 1.6 game and VPE/VEF DLLs.
