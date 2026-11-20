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
cs='\n'.join(p.read_text(encoding='utf-8') for p in (root/'Source').rglob('*.cs'))
for token in ('TrySpawnHatchedOrBornPawn','vigilTrialCandidate','guardianWitness','executionerWitness',
              'LumenWorldComponent','IsLuminant','Scribe_References.Look','VGL_LuminousWardCast','LuministPathDef','ReleaseLostBearer'):
    assert token in cs, f'Missing source invariant: {token}'
import re
# Every VigilForgottenBond.* class named in XML must exist in the C# source.
classes=set(re.findall(r'\b(?:class|struct)\s+(\w+)', cs))
for p in xml:
    for m in re.findall(r'VigilForgottenBond\.(\w+)', p.read_text(encoding='utf-8')):
        assert m in classes, f'{p.name} references VigilForgottenBond.{m}, which is not defined in Source/'
# Every field in VGL_DefOf must match a defName (DefOf fields resolve by name at startup).
defof=(root/'Source'/'VigilForgottenBond'/'VGL_DefOf.cs').read_text(encoding='utf-8')
for name in re.findall(r'public static [\w.]+ (VGL_\w+);', defof):
    assert name in seen, f'VGL_DefOf.{name} has no matching defName'
# Cheap structural check: braces and parentheses balance in each C# file.
for p in (root/'Source').rglob('*.cs'):
    code=re.sub(r'"(?:\\.|[^"\\])*"|//[^\n]*|/\*.*?\*/', '', p.read_text(encoding='utf-8'), flags=re.S)
    for o,c in ('{}','()','[]'):
        assert code.count(o)==code.count(c), f'Unbalanced {o}{c} in {p.name}'
print(f'PASS: {len(xml)} XML files, {len(seen)} unique defs, {len(paths)} validated texture references, '+
      f'{len(list((root/"Textures").rglob("*.png")))} PNG files, source invariants present.')
print('WARNING: This is NOT a C# compilation or RimWorld/VPE runtime test.')
