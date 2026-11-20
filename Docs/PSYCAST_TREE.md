# The Luminist — VPE path design

Three branching disciplines share one exclusive path. **Only the current female Luminant** may unlock it; ordinary psycasts remain available separately.

| Tier | Sanctuary | Revelation | Judgment |
|---|---|---|---|
| 1 | Luminous Ward (prototype) | Second Sight (prototype, sight buff only) | Radiant Spark (prototype) |
| 2 | Merciful Light | Cleansing Pulse | Luminous Lance |
| 3 | Hallowed Ground | Radiant Seal | Sundering Light |
| 4 | Guardian's Grace | Beacon of Dawn | Celestial Verdict |
| 5 | Eternal Sanctuary | Light Unbound | Dawnfall |
| Capstone | Dawnbringer (ultimate, requires a story trial and mastery across all three branches) |||

## Costs and tactical roles (provisional)
- Tier 1: 3–5% psyfocus and 5–10 neural heat. Defined as XML abilities.
- Tier 2: 6–10% focus; one direct and one area ability per tier.
- Tier 3: 10–18% focus; mechanics begin interacting with Anomaly when DLC installed.
- Tier 4: 18–25% focus and moderate cooldown.
- Tier 5: 30–40% focus and high neural heat.
- Capstone Dawnbringer: 70% focus, long cooldown, only late-game; no instant story-boss deletion.

## Age gates
- 0–6: latent, occasional damage burst; no castable tree.
- 7–12: tier 1.
- 13–17: tiers 1–3.
- 18+: all tiers and story-gated capstone.

## Prototype vs plan
- The *three* tier-1 VPE ability defs and a path def are in XML, **not yet runtime-validated against a local copy of VPE**.
- Tier 2–5 and capstone have icon concepts but no implemented ability XML/C# yet.
- Birth and owner eligibility are world-level. The VPE unlock check is patched, but bypass paths such as direct ability injection must be tested and guarded.
- Second Sight currently increases Sight capacity; detecting invisible Anomaly entities is not implemented.
- Luminous Ward adds a temporary IncomingDamageFactor modifier; actual defense must be balance-tested.
- Radiant Spark is a vanilla Burn projectile proxy; bespoke anti-void tags/damage will come later.
