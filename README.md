# ALPHA v0.7 IMPORTANT

**The game-mechanic DLL is not included. This is source for local compilation; see Tools/Build-Windows.ps1.**

New: three Luminist ability classes block unrecognized casters at casting time. Five debug `QuestScriptDef`s appear under Generate quest after compilation, plus VIGIL: Tests debug actions. See `Docs/DEV_TESTS.md`.

# VIGIL: The Forgotten Bond — Alpha v0.6 source

**Source prototype only. There is NO compiled DLL in this archive.** The original sword sprites and VPE concept assets are preserved. The separate Weapon Preview ZIP can be installed to inspect the sword art; it does *not* enable any story mechanics.

## What's new compared with v0.5

- The **Three Witnesses** now requires actual combat events before an Arbiter oath can create a male VIGIL bearer. A developer shortcut is available on the sword only while Dev Mode is active.
- Birth-based Luminant assignment now has a direct spawn hook, plus a conservative fallback scanner for modded births; the world-level registry still guarantees only one currently designated female mantle bearer.
- Candidate progress, relationship state, Luminant references, and the succession cooldown are saved.
- VPE integration still proposes granting the first Luminist ability at age seven.

## What is NOT done

No compiled assembly, verified in-game UI, fully tested Luminist psycast, complete questline, VIGIL visual state switching, or separate Horax faction is included. See Docs/ALPHA_V06_CHANGELOG.md.

## Building on Windows

1. Install RimWorld **1.6**, Royalty and Biotech, plus Harmony, Vanilla Expanded Framework, and Vanilla Psycasts Expanded.
2. Install Visual Studio Build Tools with **.NET Framework 4.7.2 Developer Pack** and C# support, or compatible `dotnet` build tools with reference assemblies.
3. Start PowerShell in this folder, then run `./Tools/Build-Windows.ps1 -RimWorldDir 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld' -ModsDir 'C:\Program Files (x86)\Steam\steamapps\workshop\content\294100'`. Update the paths for your installation.
4. If dependency discovery fails, set env vars `RIMWORLD_MANAGED`, `HARMONY_DLL`, `VEF_DLL`, and `VPE_DLL` manually then run `dotnet build Source/VigilForgottenBond/VigilForgottenBond.csproj -c Release`.
5. Copy this folder **after compilation** to RimWorld's Mods folder; don't enable both source mod and separate Weapon Preview at once.
6. Open a **new test save** with the dependencies. See Docs/ALPHA_V06_CHANGELOG.md for Three Witnesses testing.

## Static verification

Run `python Tools/VerifyFiles.py` to check XML parsing, duplicate DefNames, texture paths, presence of an uncompiled DLL placeholder, and rough source invariants. This cannot replace actual compile or game testing.
