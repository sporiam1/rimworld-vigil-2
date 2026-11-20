from pathlib import Path
import sys, xml.etree.ElementTree as ET, collections, zipfile
from PIL import Image
root=Path(__file__).resolve().parents[1]
xml=list((root/'Defs').rglob('*.xml'))
assert xml, 'No definitions found'
seen={}
for p in xml:
    tree=ET.parse(p)
    for elem in tree.getroot():
        name=elem.findtext('defName')
        if not name: continue
        if name in seen: raise AssertionError(f'Duplicate DefName {name}: {seen[name]} and {p}')
        seen[name]=p
assert {'VGL_VigilSword','VGL_LuminantTrait','VGL_LuminantMantle','VGL_Luminist','VGL_LuminousWardCast'} <= set(seen)
paths=[]
for p in xml:
    node=ET.parse(p)
    for t in node.iter('texPath'):
        if not t.text: continue
        paths.append((p,t.text))
    for t in node.iter('iconPath'):
        if not t.text:continue
        paths.append((p,t.text))
for p,rel in paths:
    tex=root/'Textures'/f'{rel}.png'
    if not tex.exists(): raise AssertionError(f'Missing {tex}, referenced by {p}')
    with Image.open(tex) as im:
        im.verify()
for p in (root/'Textures').rglob('*.png'):
    with Image.open(p) as im:
        im.verify()
assert not list((root/'Assemblies').glob('*.dll')) if (root/'Assemblies').exists() else True, 'Development source unexpectedly bundles DLL'
cs='\n'.join(p.read_text(encoding='utf-8') for p in (root/'Source').rglob('*.cs'))
for token in ('TrySpawnHatchedOrBornPawn','vigilTrialCandidate','guardianWitness','executionerWitness',
              'LumenWorldComponent','IsLuminant','Scribe_References.Look','VGL_LuminousWardCast'):
    assert token in cs, f'Missing source invariant: {token}'
print(f'PASS: {len(xml)} XML files, {len(seen)} unique defs, {len(paths)} validated texture references, '+
      f'{len(list((root/"Textures").rglob("*.png")))} PNG files, source invariants present.')
print('WARNING: This is NOT a C# compilation or RimWorld/VPE runtime test.')
