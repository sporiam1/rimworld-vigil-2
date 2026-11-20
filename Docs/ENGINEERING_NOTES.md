# Technical status — alpha source v0.2

**This is SOURCE and ART, not a compiled tested full mod.** Build the DLL before enabling the main mod. No local RimWorld managed assemblies or C# compiler were available during production, so no claim of successful compilation or game testing is made.

## Implemented as source
- Male-only bound-pawn check; comp saves binder, Accord, oath, Resonance, petition and consult commands.
- First usable VIGIL Guardian's Ward: bearer gains Resonance when hit; spends 30 to apply a short defensive buff to nearby colonists; one-day cooldown. Needs balancing and in-game verification.
- First-colony discovery event after day 15; there is no ancient ruin yet.
- One active female Luminant controller, birth assignment, death, one-year succession, pre-succession resurrection, 7-year awakening letter.
- Emergency childhood shield reducing one incoming hit 40%, on 2-day cooldown (120k ticks); it does not grant invulnerability.
- Three starter VPE ability defs and corresponding transparent icons; rest of the path is designed only.
- XML visual sprites for dormant, awakened, ascendant versions, though the runtime **currently uses the dormant texture only**.

## Critical validation points after local compile
1. Validate 600-tick home-map newborn scanning, assignment notification, and daughters born immediately after succession eligibility; note off-map birth coverage remains missing.
2. Check `Pawn_HealthTracker.PreApplyDamage` signature, equipment checks and `Pawn_EquipmentTracker.AddEquipment` overload. Adjust patch target signatures and field lookup if required.
3. Verify VPE `PsycasterPathDef` mandatory fields and `CanPawnUnlock` patch parameters against installed VPE build.
4. Verify VEF `AbilityExtension_Hediff` and `Ability_ShootProjectile` target setups.
5. Test hediff stat modification, projectile, spell use on 7-year Luminant, and correct target selection.
6. Test newborn trait injection at ages under 7, age-7 vanilla growth trait selection, pregnancies, growth vats, multiple births, death on caravans, resurrection and off-map pawns.
7. Confirm exactly one sword for generated and imported saves; alpha only auto-spawns one, and dev-spawn can duplicate it.
8. Validate all compatibility with Combat Extended, VPE add-ons and character editors separately.

## Why a world component?
The only-Luminant condition must not be stored only on a gene, hediff, pawn list in one map, or current colony membership. Off-map pawns, prisoners, and caravans persist. A single WorldComponent owns the mantle state.

## Major planned implementation
- Three Witnesses tracked through a pawn-specific component, with event hooks for protection/combat/quest decisions.
- Proper quest sites using QuestScriptDef rather than an item magically appearing.
- Three VIGIL combat powers, Resonance, weapon-awakening graphics, final combined knight-and-Luminant abilities.
- A first-time Luminant acquisition option in existing colonies without birth; configurable foundling path.
- Options UI for succession duration, starting event timing, auto-birth assignment, child burst strength, and player alerts.
- VPE-level age gating at *each ability unlock*, not only at path unlock.
- Save migration/versioning and reference cleanup.

## Source layout and build
`Source/VigilForgottenBond/VigilForgottenBond.csproj` expects MSBuild `net472` and references supplied through environment variables or `/p:` arguments: `RIMWORLD_MANAGED`, `HARMONY_DLL`, `VPE_DLL`, `VEF_DLL`.

Example in Windows PowerShell (paths are examples):
```
dotnet build .\Source\VigilForgottenBond\VigilForgottenBond.csproj -c Release `
 /p:RIMWORLD_MANAGED="C:\Games\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed" `
 /p:HARMONY_DLL="C:\...\Harmony\1.6\Assemblies\0Harmony.dll" `
 /p:VPE_DLL="C:\...\VanillaPsycastsExpanded.dll" `
 /p:VEF_DLL="C:\...\VEF.dll"
```
Do not upload RimWorld or VPE DLLs into a public release. They are dependencies, not redistributed source.

## v0.2 Art and UI
- White-and-gold sword states replaced prior graphite-focused sprite concepts; actual dynamic stage switching is not yet coded.
- `Defs/Traits/LuminantTrait.xml` is intended to show on Bio immediately after the new mantle is assigned, plus a four-stage Health status. This needs Biotech growth regression testing.
- VPE Luminist path now includes a dedicated category and background textures based on existing add-on patterns; load-time validation is still needed.
- `Docs/QUESTLINE_DESIGN_v02.md` now details how VIGIL's Third Directive and Luminant's agency converge without requiring the shelved Horax faction mod.
