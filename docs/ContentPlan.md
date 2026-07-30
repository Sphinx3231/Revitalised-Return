# Project Return — Content Plan (Design-Level, Non-Engineering)

**Status:** Draft, 2026-07-30. This is a game-design/production plan, distinct from
`docs/DesignDoc.md` (which covers Steps 9-14's mechanical/systemic design against
Genshin/Elden Ring references) and the 14-step engineering pipeline in `CLAUDE.md`.
This doc tracks *content* that needs to be authored — beats, movesets, enemy
rosters, items — which no amount of engineering alone produces.

Cross-reference: `CLAUDE.md` (charter, world/acts, 14-step pipeline),
`docs/DesignDoc.md` (Steps 9-14 systemic design), `docs/Worklog.md` (engineering
progress log — Steps 1-6 implemented and QA'd as of this doc's writing, Step 6's
task file/Worklog/CLAUDE.md status entries are NOT yet closed out even though QA
passed clean — that close-out is still pending next time development resumes).

---

## 1. World & Story (Prologue + 5 Acts)
**Status: locked at the beat level only.** Jin Takakura, samurai returning to
Kaze-no-Tani. Prologue (Sekigahara, Boss: Captain Renzo) → Ashlands (Kuroda,
Stone Stance) → Sunken Pines (Soren, Water Stance) → Mount Shindai (Masato, Flame
Stance) → Outskirts (Madame Mei, Wind Stance) → Ancestral Estate (Lord Osamu,
final).

**Gap:** the connective tissue between boss fights doesn't exist. Only Act I has
2 placeholder landmark beats stubbed (`beat_ferry_toll_warning`-style content
does NOT exist in this repo — that was a different, unrelated local project;
this repo currently has zero authored story content beyond the charter's bullet
list).

**Needed before Step 12 (Narrative Engine) starts:** one beat sheet per region:
arrival → rising tension → pre-boss NPC scene → boss → stance reward → transition
to next region. Write these as actual short scenes/dialogue sketches, not just
labels, so Step 12 has real content to wire instead of inventing it mid-task.

---

## 2. Combat System — Open Design Question
Stances/hitbox/parry/block resolution implemented (Step 5). **Unresolved:** how
does the player actually enter a blocking state? No InputMap action exists for
it (a real gap Step 5's research caught). Options to decide between:
- Universal block bound to a new input (scope creep into Step 3's "complete
  input surface," but simplest).
- Stance-specific (e.g. Wind's "high deflection" identity suggests blocking
  might BE Wind's parry-adjacent mechanic, not a separate universal action).
- Some other trigger entirely (hold `parry` past its window?).
This needs a decision before Step 10 (interactables referencing combat state)
or Step 13 (block animation) build on top of it.

---

## 3. AI & Enemy Roster (Step 7, not started)
FSM (Idle→Investigate→Telegraph→Attack→Recovery) and perception cone are
mechanically speced in `CLAUDE.md` Section 7. **No enemy variety is planned
anywhere.** Needed: 2-3 enemy archetypes per region so combat doesn't feel like
one reskinned dummy across 5 acts. Draft roster (placeholder names, refine
later):
- **Prologue/Ashlands:** rank-and-file ronin (baseline), a spear-wielder that
  punishes dodge-spam with reach/tracking, an archer (per Step 9.1's existing
  bandit-archer precedent in a different, unrelated repo — NOT yet built here).
- **Sunken Pines:** an ambush-type using water/reeds for stealth (tests
  perception-cone/acoustic-detection edge cases), a slower "brute" that
  punishes over-aggression.
- **Mount Shindai:** something that exploits verticality/ice terrain (knockback
  risk near cliff edges).
- **Outskirts:** the widest region — most NPC-adjacent enemy variety, maybe a
  duelist-type that mirrors player stance-swapping in miniature (a preview of
  Osamu's final-boss gimmick).
- **Estate (Act V):** elite legacy-dungeon enemies, no new archetypes needed —
  reuse/upgrade prior ones (Elden-Ring "legacy dungeon" pacing per DesignDoc).

**Hard blocker, not just a nice-to-have:** Step 5 found every `Combatant`
instance independently consumes the shared global `InputBuffer`'s `"parry"`
action. If Step 7's enemy AI extends `combatant.gd` unchanged, every enemy on
screen reacts to the player's own parry key-press. **Step 7's task intake must
resolve this** (e.g. an `is_player_controlled` gate) before any enemy-AI
implementation starts.

---

## 4. Boss Design (Step 8, not started)
6 named bosses, phase logic mechanically speced (HP/posture thresholds, arena
locks, Camera3D tracking). **No boss has an actual moveset or identity yet.**
Needed, one short moveset doc per boss before Step 8 starts:
- **Captain Renzo (Prologue):** teaching boss — should be beatable with default
  stance, no gimmick, establishes parry/posture rhythm.
- **Kuroda (Act I, unlocks Stone):** suggest a stance-punish gimmick — maybe
  rewards patience/heavy poise, punishing pure-aggression play, foreshadowing
  why Stone (heavy posture damage, high poise) matters.
- **Soren (Act II, unlocks Water):** already has a real branching pre-fight
  beat locked in DesignDoc §12 (a "lower your blade" choice that's allowed to
  fail) — moveset should support that emotional beat, not just be a generic
  fight; consider a first phase that's winnable non-lethally if the dialogue
  branch is taken.
- **General Masato (Act III, unlocks Flame):** crowd-adjacent or multi-hit
  moveset that rewards Flame's "wide arc cleave" identity once acquired
  (ironic that you don't have it yet for THIS fight — consider whether Masato
  should instead reward WATER, acquired previously, to close a loop).
- **Madame Mei (Act IV, unlocks Wind):** "high deflection/anti-spear counters"
  suggests Mei should be a parry-check fight — lots of parriable openings,
  punishing button-mashing.
- **Lord Osamu (Act V, final):** charter explicitly says "Dynamic Stance
  Mirror" — needs actual design for what mirroring DOES moment to moment.
  Draft idea: Osamu adopts whatever stance the player is CURRENTLY in and
  gains that stance's strength, forcing the player to think about stance
  choice as a real tactical question in the final fight rather than a
  preference — needs playtesting once built, flag as a design risk.

Design goal across all 6: each of the 4 stances should have at least one boss
fight where it's clearly the best tool, not just flavor.

---

## 5. Itemization — Omamori Charms (Step 10, structurally planned, content missing)
DesignDoc §10 locks the system (3 slots, ~12 hand-authored charms, flat
deterministic effects, no rolls) but **no charm is actually named or designed.**
Draft a list of ~12 before Step 10 implementation starts, e.g.:
- A few generic/always-useful ones (posture-damage-taken reduction, stamina
  regen speed).
- A few stance-synergy charms (one per stance, encouraging a build-around).
- A few narrative/region-flavored ones tied to the region beat sheets (#1) —
  e.g. a charm found via Soren's NPC thread that reflects the choice made
  there.
- 1-2 build-defining "signature" charms found late (Act IV/V) as a power
  spike.

---

## 6. HUD, UI, Narrative Structure, Art/Audio, Balancing (Steps 11-14)
Already deeply and mechanically planned in `docs/DesignDoc.md` — no content
gaps beyond what's noted above (region beats feed Step 12; nothing else is
missing at the planning level for these steps).

**One unresolved feasibility question, never actually tested:** whether this
environment has outbound web access to source CC0 asset packs (Kenney.nl,
Quaternius, ambientCG) for Step 13's art pass, or whether it's procedural-only.
Worth resolving early since it changes the entire art plan/budget.

---

## Priority order for future content-planning sessions
1. Region beat sheets (#1) — blocks Step 12, informs Step 7/8 content.
2. Boss movesets (#4) — blocks Step 8, the highest-impact single content gap.
3. Enemy roster (#3) — blocks Step 7.
4. Omamori charm list (#5) — blocks Step 10, lowest urgency (latest step).
5. Block-input design decision (#2) — cheap to resolve, blocks nothing urgent
   yet but should be settled before Step 10/13 assume an answer.
