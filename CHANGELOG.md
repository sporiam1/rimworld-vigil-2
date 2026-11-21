# Changelog

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
