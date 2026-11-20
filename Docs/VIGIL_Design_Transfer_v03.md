# VIGIL art transfer — v0.3

Source image: the approved updated VIGIL concept sheet without the decorative arcs behind the grip.

The main sword was isolated and converted to right-pointing transparent 512x512 PNGs for RimWorld. Wide royal gold/blue crossguard and dark grip were preserved. Cyan rune light is darker in Dormant, lit in Awakened, and brighter with limited blade-local bloom in Ascendant.

Textures: Textures/Vigil/Weapons/Vigil_{Dormant,Awakened,Ascendant}.png

Vanilla-style XML rendering: Graphic_Single / drawSize (1.65,1.65) / equippedAngleOffset -65.

These 3 stages share one underlying weapon silhouette. The complete core code still needs compilation and a runtime graphics-state controller to switch them without spawning separate sword items.

For visual test use the separately packaged XML-only mod. The full source pack is NOT an installable gameplay build and contains no compiled DLL. Do not enable full source and preview mod together.
