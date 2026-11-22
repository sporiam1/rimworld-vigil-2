# Changelog

## v0.9.3 (2026-11-21)

### Fixed
- **A dead bearer kept "VIGIL's bond (apart)" on his corpse** (#24). The bond is now removed the moment he dies. A bearer resurrected while still holding VIGIL gets the bond back automatically.

## v0.9.2 (2026-11-21)

### Changed
- **VIGIL's bond stays in the Health tab for as long as he's the bearer** (#22). It now appears the moment he's bound. In hand, it shows the stage (bound, awakened, ascendant) and its bonuses. With the sword put away, it shows as "VIGIL's bond (apart)" with no bonuses.

### Fixed
- Startup warning "VigilWeapon probably needs a StaticConstructorOnStartup attribute": the stage sprites are now built on the main thread at startup.

## v0.9.1 (2026-11-21)

### Fixed
- Three config errors on the sword (#21), present since the original prototype:
  - It listed the WeaponsMelee category twice.
  - It inherited a crafting recipe, so it could have been craftable at a smithy.
  - It was smeltable but gave nothing back.
  It's now never craftable or smeltable.

## v0.9.0 (2026-11-21): Milestone 2a, VIGIL comes alive

### Added
- **Sword abilities.** These are buttons on the bearer while he holds VIGIL:
  - **Aegis** (Guardian, 30 Resonance, 1-day cooldown): he and allies within 5 tiles take 40% less damage for 30 seconds.
  - **Sever** (Executioner, 25 Resonance, 1-hour cooldown): an armor-piercing cut against an adjacent enemy.
  - **Verdict** (Arbiter, 40 Resonance, 12-hour cooldown): stuns an enemy in sight, then **Judges** it for 40 seconds. A Judged enemy is slowed, less accurate, and takes 15% more damage.
  - **Final Mandate** (Ascendant only, 100 Resonance, 3-day cooldown): a shockwave that strikes and stuns enemies within 6 tiles and gives allies inside it Aegis.
  - **Last Stand** (passive, Cooperative Accord or higher, once per 2 days): below 45% health, one big hit is cut by 70%, and VIGIL steadies him with less pain and better focus.
- **Resonance in combat.** It builds when the bearer deals or takes damage, and from kills. A status panel shows Resonance and Accord next to the ability buttons.
- **Accord tiers, with effects:**
  - Fractured (below 25): VIGIL refuses Sever, Verdict and Final Mandate.
  - Cooperative (50+): Last Stand turns on.
  - Trusted (75+): abilities cost 20% less.
  - True Accord (90+): Ascendant becomes possible.
  - Accord rises with counsel (+2 a day), the oath (+10), shielding 2+ allies with Aegis (+1), and ending a threat (+1, once a day). It falls by 2 when an ally dies within 15 tiles of the bearer.
  - Each tier change sends a letter with VIGIL's reaction, and **Consult VIGIL** has different lines for each tier.
- **Awakening stages**, which never go backwards:
  - **Bound**
  - **Awakened**: Accord 50+ and 100 Resonance spent.
  - **Ascendant**: Accord 90+, plus either the oath or 400 Resonance spent.
  - The sword's sprite changes for Awakened and Ascendant. While he holds it, the bearer gets **VIGIL's bond**: +2/+4/+6 melee hit chance, and ×1.15/×1.3 melee damage from Awakened on.
- Six ability icons.
- Debug menu, VIGIL: Tests: Accord ±25, Fill Resonance, Reset ability cooldowns, Advance awakening stage.
- CI warns about any stat name that isn't in RimWorld's `StatDefOf`.

All numbers are first-pass balance and need playtesting.

## v0.8.1 (2026-11-21)

**Playtested in RimWorld 1.6.4871 (2026-11-21): every test in `Docs/DEV_TESTS.md` passes.** First confirmed-working build.

### Fixed
- **Possible load crash with VPE.** The Luminist path was a `Class=` subclass of VPE's path def. If RimWorld files a subclassed def separately, VIGIL's psycasts can't attach to the path. VPE then calls `Max()` on an empty ability list ("Sequence contains no elements"), and that exception stops loading (`KeyBindingDef` null). The path is now a plain VPE path, and a Harmony postfix on `PsycasterPathDef.CanPawnUnlock` locks it to the Luminant. The lock behaves the same.
- About.xml lists Steam Workshop links for Harmony, VEF and VPE, which clears three load warnings.

## v0.8 (2026-11-19)

All VEF/VPE calls were checked against the public VEF and VPE source. It compiles but hasn't been playtested.

### Fixed
- **Non-Luminants could unlock and cast Luminist psycasts.** The likely cause was VPE's Dev Mode, which lets any pawn press Unlock on any path. v0.7's fix guessed at a VPE method through reflection. v0.8 uses VPE's built-in lock instead: a `LuministPathDef` subclass overrides `CanPawnUnlock`. The path also sets `ignoreLockRestrictionsForNeurotrainers=false`, so VPE blocks the cast, and `ensureLockRequirement=true`, so VPE removes the path from anyone who isn't the Luminant. The ability-class check stays as a second layer.
- **The ability-grant block covered only 3 powers.** It now covers any ability on the Luminist path, so all 15 planned powers and Dawnbringer are protected.
- **A dead bearer locked VIGIL forever.** The sword now releases a dead bearer, sends a "VIGIL Remembers" letter, keeps his name, and reopens the Three Witnesses. If he's resurrected before anyone handles the sword, he keeps it.
- **One failed patch disabled all of them.** `Harmony.PatchAll()` stops at the first patch it can't apply, so every later patch was skipped without a message. Patches now apply one class at a time, and failures are logged by name.

### Build
- **First successful compile.** GitHub Actions now builds every push and publishes a ready-to-play zip as the `dev-build` release.
- Compile fixes: target .NET 4.8 to match VPE; add missing `LudeonTK` and `RimWorld.Planet` usings; the debug quest part derives from `QuestPartActivable` and waits for the player to accept the quest.

### Changed
- Def lookups are cached in `VGL_DefOf` instead of looked up on every damage event.
- Former Luminants are saved by name, not as pawn references, so cleaned-up dead pawns no longer cause load errors. Old alpha saves lose their predecessor list.
- Succession uses `GenDate.TicksPerYear`, which is the same one-year value.
- Build: RimWorld and Harmony references come from NuGet (`Krafs.Rimworld.Ref`, `Lib.Harmony`), so building only needs the VEF/VPE DLLs from the Workshop.
- `VerifyFiles.py` now also checks that C# classes named in XML exist, that `VGL_DefOf` names match defs, and that braces balance. It no longer fails after a build.

## v0.1 – v0.7
Planned and prototyped in ChatGPT; see `Docs/History/`.
