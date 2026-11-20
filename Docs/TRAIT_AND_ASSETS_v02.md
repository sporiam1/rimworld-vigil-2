# Luminant identification and VIGIL art — v0.2

## Visible Luminant identity

XML `Defs/Traits/LuminantTrait.xml` defines **luminant** with `commonality=0` and degree `0`. This is a genuine RimWorld trait, not a copied vanilla one. The `LumenWorldComponent` grants it by C# to the current recognized female newborn. It appears on the Bio tab when the character's story/traits tracker is available. A TraitDef alone cannot guarantee uniqueness: the WorldComponent pointer is the authoritative identity.

The `VGL_LuminantMantle` Hediff is also assigned and progresses through four age-driven visible labels:

| Age | Health stage | Gameplay |
|---|---|---|
| 0–6 | latent | Limited emergency protective burst |
| 7–12 | first light | First VPE Luminist tier available |
| 13–17 | shaping radiance | Planned mid-tier access |
| 18+ | mature lumen | Eligible for late tiers with story milestones |

**Critical testing question:** vanilla Biotech usually grants ordinary traits at age 7 and may count an early manually-added trait against child growth's ordinary trait slots. If the special trait disrupts growth choices, we will change the pre-age-7 presentation to a visible hediff / pawn inspection marker and inject the Bio trait after the vanilla growth milestone. No in-game behavior has yet been verified.

## New sword art
- Three textures were extracted and cleaned from a newly generated concept board inspired by the reference artwork supplied in chat.
- Palette: pearl ivory, antique gold, graphite core, pale cyan-white awakening effects.
- All are diagonal 512×512 transparent PNG equipment sprites (`Textures/Vigil/Weapons/Vigil_*.png`).
- Dormant is currently the only configured live XML texture. Dynamic stage switching requires runtime graphics work.
- The concept art is included in Docs as a **visual development reference**. The uploaded reference picture is deliberately not repackaged.

## VPE visual path art
- Two custom Luminist background PNGs are in `Textures/UI/Psycasts`.
- XML includes `background`, `altBackground`, `tab`, and `tooltip`, following common VPE path definitions.
- Existing 15-plus original icon concepts remain in `Textures/Vigil/Icons`.

## Anomaly
- For now, there are no new Horax NPCs, cult factions, or modified hostile faction relations.
- Specific anti-Void effect handling and Anomaly compatibility are future milestones for the core mod.
