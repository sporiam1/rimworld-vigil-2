# VIGIL — The Forgotten Bond

A RimWorld 1.6 mod about a sentient archotech sword that judges who may carry it, and the one girl in the world born with the Lumen.

> **Status: alpha v0.9.0.** v0.8.1's core systems are playtested; v0.9's sword abilities are new and waiting for playtest.

## Play it

1. Download **VIGIL_ForgottenBond.zip** from the [dev-build release](https://github.com/sporiam1/rimworld-vigil-2/releases/tag/dev-build). It's rebuilt automatically on every update.
2. Unzip it into `RimWorld/Mods`, so you end up with `Mods/VIGIL_ForgottenBond/About/About.xml`. Delete any older VIGIL folder first.
3. Enable in this order: Harmony → Royalty → Biotech → Vanilla Expanded Framework → Vanilla Psycasts Expanded → VIGIL.
4. Turn on Dev Mode and follow `Docs/DEV_TESTS.md` (in this repo).

## What's in it

- **VIGIL**, a unique sword: only one male colonist can carry it, chosen through the **Three Witnesses** trial (Guardian, Executioner, Arbiter). Its bearer uses four abilities (**Aegis, Sever, Verdict, Final Mandate**) and has a **Last Stand** passive. These run on **Resonance**, built in combat, and **Accord**, VIGIL's trust in him. The sword awakens from Bound to Awakened to Ascendant, and its sprite changes as it does. When its bearer dies, it goes dormant, remembers him, and can be petitioned again.
- **The Luminant**: only one living at a time in the whole world. A newborn girl inherits the mantle, it passes on one in-game year after her death, and she shows a visible trait and health condition.
- **The Luminist path**: an exclusive Vanilla Psycasts Expanded tree that only the living Luminant can unlock or cast. Three tier-I powers are defined so far; 15 are planned (see `Docs/PSYCAST_TREE.md`).
- **The Forgotten Bond**: the knight's guardianship oath.
- **Dev tools**: five `[TEST]` quests and a *VIGIL: Tests* debug menu (see `Docs/DEV_TESTS.md`).

Not built yet: the remaining 12 Luminist psycasts and Dawnbringer (Milestone 2b), the Silent Vault site, the Censor, and the story questline (Milestone 3).

## Requirements

RimWorld 1.6 · Royalty · Biotech · Harmony · Vanilla Expanded Framework · Vanilla Psycasts Expanded

## Building it yourself (optional)

You only need this to change the code. Every push to `main` is compiled by GitHub Actions (`.github/workflows/build.yml`).

### Windows

1. Install the [.NET SDK](https://dotnet.microsoft.com/download).
2. Subscribe to VEF and VPE on Steam.
3. **Recommended:** clone this repo straight into your RimWorld `Mods` folder, so each build lands where the game loads it:
   ```
   cd "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods"
   git clone https://github.com/sporiam1/rimworld-vigil-2
   ```
4. From the repo folder, in PowerShell:
   ```
   ./Tools/Build-Windows.ps1
   ```
   Pass `-ModsDir "D:\...\workshop\content\294100"` if Steam isn't in the default place.
   RimWorld and Harmony references come from NuGet. The script finds the VEF/VPE DLLs in your Workshop folder.
5. Enable the mods in this order: Harmony → Royalty → Biotech → VEF → VPE → VIGIL. Start a new save with Dev Mode on.

Before committing, run `python Tools/VerifyFiles.py` for static checks: XML, textures, class references, DefOf names and brace balance. It does not compile anything.

## Layout

```
About/        mod metadata
Defs/         XML: sword, Luminant trait/hediffs, Luminist path + psycasts, test quests
Source/       C# project → builds to Assemblies/ (git-ignored)
Textures/     sword sprites, psycast icons, path backgrounds
Docs/         story bible, psycast tree, questline, art direction, dev tests
Docs/History/ original ChatGPT planning log and v0.6–v0.7 changelogs
Tools/        build/install scripts, static verifier
```

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
