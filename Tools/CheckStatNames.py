"""CI helper: warn about stat names in our XML that RimWorld's StatDefOf doesn't contain.

RimWorld stat names live in Core XML (not available to CI), but StatDefOf in
Assembly-CSharp lists most of them. A name missing here MIGHT still be valid,
so this only emits GitHub warnings; check them by hand.
Usage: python Tools/CheckStatNames.py <path to Assembly-CSharp.dll>
"""
import re, sys
from pathlib import Path
import xml.etree.ElementTree as ET

dll = Path(sys.argv[1]).read_bytes()
root = Path(__file__).resolve().parents[1]
names = set()
for p in (root / 'Defs').rglob('*.xml'):
    for tag in ('statOffsets', 'statFactors', 'equippedStatOffsets', 'statBases'):
        for node in ET.parse(p).getroot().iter(tag):
            names.update(child.tag for child in node)
# The metadata string heap shares suffixes between names, so match 'name\0' without
# requiring a leading NUL.
missing = sorted(n for n in names if n.encode() + b'\0' not in dll)
for n in missing:
    print(f'::warning title=Unverified stat::{n} is not in StatDefOf; confirm it exists in RimWorld 1.6 Core defs.')
print(f'Checked {len(names)} stat names: {sorted(names)}; unverified: {missing or "none"}')
