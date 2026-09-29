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
  **Lactation Expansion** (milking), Royalty (titles, quests), Ideology.

## What the port changes
- **No RimJobWorld.** Organs come from Intimacy – Gender Works: every caste gets a Nephila
  **gel womb** (a proper Gender Works organ, handed out by Gender Works itself), the grand
  matron an ovipositor too. The castes' breasts are their own hediffs and boost Lactation
  Expansion's milk stats; grown Nephila lactate constantly (vanilla lactation, which
  Lactation Expansion makes milkable). The lactation psycast induces lactation and swells
  breasts (`NephilaSwollenBreasts`). RimJobWorld stats map onto Intimacy's (lovin'
  frequency) or are dropped; its non-consent content is gone.
- **RimRound weight.** Nephila carry RimRound weight, fullness and diet; every brood adds
  weight as it swells, changing caste adds a lump more, and gathering a clutch sheds some.
  The castes keep their own sprites (RimRound's bodies would replace them); the
  human-equivalent race gets RimRound's human bodies.
- **Adults only, by life stage.** Transformations, the fog, the auras (all lust hazes),
  milking, lactation and breast swelling only ever affect grown pawns — judged by life
  stage, so an android that is adult at one year old counts and a slow race's teenager
  doesn't.
- **Transformations keep the person.** Name, age, skills and passions, traits, backstory,
  ideoligion, relations, faction and prisoner/slave status, work settings and weight all
  carry over; the caste keeps its own body type; unusable gear is dropped, not deleted.
- **Rewritten code.** No per-tick mod-list scans or missing-hediff lookups, no endless
  loop in the fog, a pathfinding patch that only works on Nephilitic ground (and actually
  gets applied now), seeded Eden placement, keyed letters, 1.6 APIs throughout.

## Building
`Source/build.sh` builds `1.6/Assemblies/Nephila.dll` against the game's own assemblies
(it expects RimRound, Harmony and HAR next to it in `Mods/`).
