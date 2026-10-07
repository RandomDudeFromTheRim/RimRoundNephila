# RimRound: Nephila

A RimWorld 1.6 port of the **Nephila HPreg Mod** (HPregAnon; 1.3–1.5 update by BiasNil),
rebuilt as a companion to [RimRound](https://github.com/RandomDudeFromTheRim/RimRound).

The Nephila are gelatinous, nanite-infested slime women locked into a cycle of swelling
broods. A maiden's brood swells her into a **handmaiden**, a handmaiden's into a **matron**;
the path to ascension turns a matron into a **grand matron**. Their clutches hatch the
cherubim and seraphim that serve them, and their fog turns the women it touches.

## Requirements
- Harmony, Humanoid Alien Races, **RimRound** (and what RimRound needs).
- Optional: **Intimacy – Friends n' Lovers** / **Gender Works** (organs, traits, lovin'),
  **Lactation Expansion** (milking), **Vanilla Psycasts Expanded**, Royalty (titles, quests), Ideology.

## What the port changes
- **No RimJobWorld.** Organs come from Intimacy – Gender Works: every caste gets a Nephila
  **gel womb** (a proper Gender Works organ, handed out by Gender Works itself), the grand
  matron an ovipositor too. The castes' breasts are their own hediffs and boost Lactation
  Expansion's milk stats; grown Nephila lactate constantly (vanilla lactation, which
  Lactation Expansion makes milkable). The lactation psycast induces lactation and swells
  breasts (`NephilaSwollenBreasts`). RimJobWorld stats map onto Intimacy's (lovin'
  frequency) or are dropped; its non-consent content is gone.
- **Their own weight scale.** The castes eat like anyone else - no RimRound fullness or food
  bar - and carry a *mass* of their own instead of RimRound weight. Each caste is drawn to
  scale against RimRound's bodies and settles at the weight RimRound gives a body that big:

  | caste | draw size | settles at |
  |---|---|---|
  | maiden | x1.3 | 140 kg |
  | handmaiden | x1.75 | 315 kg |
  | queen's guard | x2.0 | 400 kg |
  | matron | x2.2 | 880 kg |
  | grand matron | x4.5 | 1900 kg |

  Her gel drifts towards that over the days (a new handmaiden fills out into her body, a
  starving one wastes away), and her brood (+90 / +250 / +450 / +700 kg at full), clutch
  and swollen breasts weigh on top. The mass has its own stages - wasting, thinned,
  settled, swelling, brimming, overflowing - judged against her caste's mass, not
  RimRound's. A transformed woman keeps what she weighed and grows into the new caste.
  The human-equivalent race is drawn like a human and keeps RimRound's whole weight system.
- **Drawn to scale.** Bigger castes, heads placed on their necks, portraits zoomed out to
  match; the grand matron's rider brings her own head. (RimRound sets every alien body's
  sprite and size; the castes get their own back.)
- **Adults only, by life stage.** Transformations, the fog, the auras (all lust hazes),
  milking, lactation and breast swelling only ever affect grown pawns — judged by life
  stage, so an android that is adult at one year old counts and a slow race's teenager
  doesn't.
- **Transformations keep the person.** Name, age, skills and passions, traits, backstory,
  ideoligion, relations, faction and prisoner/slave status, work settings and weight all
  carry over; the caste keeps its own body type; unusable gear is dropped, not deleted.
- **Vanilla Psycasts Expanded.** With VPE, the Nephilim psycasts (induce lactation, call of
  lust, serpent pulse) are a VPE path of their own, open to Nephila and nephilim psy amp
  bearers (psytrainers still teach anyone). The psy amp becomes a plain levelled implant, so
  VPE doesn't give its bearer a second psycast tree. The Nephila psytrainer recipes and quest
  rewards hand out VPE's psytrainers.
- **Rewritten code.** No per-tick mod-list scans or missing-hediff lookups, no endless
  loop in the fog, a pathfinding patch that only works on Nephilitic ground (and actually
  gets applied now), seeded Eden placement, keyed letters, 1.6 APIs throughout.

## The overgrown broodmother

A rare biome where the land *is* a Nephila broodmother who never stopped growing - never a
pawn, the whole biome. Warm and wet, and rarer than a Nephilitic Eden.

- **Cramped.** Every tile of her is mountainous, and her folds of **condensed goo** fill
  most of the map, leaving narrow creases between them, the odd soft chamber and a hollow
  in the middle to settle in. Condensed goo is the only stone (soft, weak, poor building
  material); mined out, it stays mined. No insect hives in her caves.
- **Milk.** Her rivers and pools run with iridescent milk, and every tile of her gets a
  milk river on the world map. With Odyssey, milk also wells up in her caves, and she has
  two landmarks: a **weeping fold** (a great milk lake) and the **navel** (the one open
  hollow on her).
- **Drinking and bathing.** Pawns drink straight from a stream for recreation, getting a
  little food and RimRound fullness; wading or swimming in milk (Odyssey's swimming works
  in it) is a pleasant warm bath.
- **Milk siphon.** Once the colony has found her milk, *milk siphoning* research appears
  (it's "???" until then). The siphon stands in shallow milk and pumps it into RimRound's
  feed lines - more from a stream than a still pool. It never bottles anything.

Textures are procedural (`Source/TextureGen/broodmother_tex.py`).

## Building
`Source/build.sh` builds `1.6/Assemblies/Nephila.dll` against the game's own assemblies
(it expects RimRound, Harmony and HAR next to it in `Mods/`), and the VPE support
(`1.6/Mods/VPE/Assemblies/NephilaVPE.dll`, against VPE and the Vanilla Expanded Framework
from the Workshop).
