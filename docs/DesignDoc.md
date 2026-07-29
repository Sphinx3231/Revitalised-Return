# Project Return — Design Document: Steps 9–14

**Status:** LOCKED (2026-07-29) — Director decisions recorded in "Director Decisions" below resolve every Open Question this doc originally raised. The recommendations throughout the body are now binding design, condensed and promoted into CLAUDE.md's "STEP DETAIL SPECIFICATIONS (Steps 9–14)" section the same way Steps 1–8 are locked. This document remains the source of truth for full rationale/sources; CLAUDE.md carries the condensed, implementation-facing version.

**Method:** Every recommendation below is anchored to a named, concrete mechanic in either **Genshin Impact** or **Elden Ring** (never a vague "make it feel like an action-RPG"), explains why that mechanic does or does not fit Project Return's specific fiction (5-act linear campaign, single character, 4 stances, Sekiro-adjacent posture/parry combat — not an open-world live-service game, not a multi-character party), and where it touches Godot APIs, those claims were verified against current 4.7 docs/source rather than assumed.

**Scope note:** Steps 1–8 are treated as locked. Everything below either builds on them or explicitly flags a conflict for the Director rather than revising them. Godot API claims marked ✅ were verified against the 4.7 docs during this pass; anything unverified is marked ⚠️.

---

## Step 9 — World Generation, Terrain Greyboxing & Level Pacing

### 9.1 Region topology — recommendation: Elden Ring "Limgrave bowl", not Genshin's continent

**Recommendation: semi-open *per-region bowls* with an authored critical-path spine and 2–4 optional lobes, plus one fully-linear legacy dungeon (Kaze-no-Tani estate).** Do not build continuous open world.

Why, concretely:

- Genshin's Teyvat is one continuous mesh with **Teleport Waypoints** at ~200–400m density and a **Statue of the Seven** per region acting as map-reveal + respawn + attribute anchor. That density exists because Genshin is a daily-return live-service game where the player re-enters at arbitrary points to farm dailies/Domains. Project Return has a **5-act linear campaign with a fixed emotional arc** (Jin carrying a medallion home). A player never re-enters Act II for dailies, so waypoint density that assumes re-entry is wasted authoring cost.
- Elden Ring's **Limgrave** is the right reference: a bounded bowl with a legible silhouette landmark on the skyline (Stormveil / the Erdtree), a **critical path** you can walk in ~15 minutes, and optional lobes (Weeping Peninsula, Murkwater Cave, Groveside Cave) that hang off it. Navigation is driven by **sightline framing** — you see a thing on the horizon and walk to it — not by minimap icons. This maps directly: each of our 5 regions gets **one skyline anchor** (Ashlands: the burnt castle spire of Sekigahara behind you; Sunken Pines: a half-submerged torii; Mount Shindai: the summit shrine; Outskirts: Mei's teahouse lanterns; Estate: the manor roofline).
- The one Genshin borrow worth taking: **Domains** — self-contained instanced challenge rooms with a fixed entrance portal. Map these onto **optional side-arenas** (2 per region, elite mini-boss, one authored reward). They're cheap to build (a single sealed room reusing the Step 8.2 `CSGBox3D` arena-lock code) and give the branches a payoff without needing to author more open terrain.
- **Explicitly do NOT map:** Genshin's climb-anywhere + glider traversal with a *shared* stamina bar. Our stamina is the combat currency (Step 4 locks dodge at 20.0 stamina with a 1.2s regen pause). Sharing that bar with traversal is the single most-complained-about Genshin system and would directly corrupt the combat economy. Traversal costs zero stamina; keep sprint free or on a separate meter.

**Region shape budget:**

| Region | Topology | Spine length | Lobes | First-clear target |
|---|---|---|---|---|
| Prologue (Sekigahara) | Corridor, no branches | ~350 m | 0 | 8–12 min |
| Act I Ashlands | Bowl, wide + flat (teaching region) | ~1,400 m | 3 | 30–40 min |
| Act II Sunken Pines | Bowl, dense sightlines, verticality via water level | ~1,600 m | 4 | 35–45 min |
| Act III Mount Shindai | Ascending spiral (single-direction, ER Liurnia→Altus style) | ~1,800 m | 2 | 35–45 min |
| Act IV Outskirts | Widest bowl, most optional NPC content | ~1,700 m | 4 | 40–50 min |
| Act V Kaze-no-Tani Estate | **Legacy dungeon** — fully authored, vertical, no open terrain, unlockable shortcut loops (Stormveil model) | ~1,200 m | 0 (shortcuts instead) | 45–60 min |

### 9.2 Rest-shrine placement & spacing logic

Elden Ring's **Sites of Grace** are spaced roughly 60–120 seconds of unobstructed travel apart along the critical path, with hard rules that are worth copying verbatim:

1. **One grace at each region entrance** (ER: "The First Step"). → One shrine at every region's spawn point; resting here for the first time reveals the region map (this is the **Statue of the Seven** map-reveal function borrowed from Genshin, folded into the shrine so there's only *one* world-interaction verb, not two).
2. **One grace immediately before every fog wall** (ER: the grace ~20s outside Margit and Godrick). → Shrine within **15–25s walk** of every boss trigger `Area3D`. This is non-negotiable: with Step 8's arena lock and multi-phase bosses, a long runback is the fastest way to make a good boss feel bad.
3. **Grace Guidance** — ER's golden light beam pointing toward the critical path when you rest. → On shrine rest, emit a 4-second `GPUParticles3D` ribbon along the spine direction toward the next critical-path node. Costs almost nothing, solves "where do I go" without a quest marker, and stays tonally in-world.
4. **Stake of Marika** — ER's pre-boss respawn marker that bypasses the runback entirely. → Auto-place an invisible `GraveMarker` node at every boss arena entrance; on death inside an arena-locked fight, respawn there instead of at the shrine.

**Spacing formula (mechanically checkable):**

```
t_travel(A→B) = curve.get_baked_length_between(A, B) / S_speed
Constraint 1: 60s <= t_travel(shrine_i, shrine_i+1) <= 120s   (target 90s ± 30s)
Constraint 2: <= 2 scripted encounters between consecutive shrines
Constraint 3: t_travel(nearest_shrine → boss_trigger) <= 25s
```

At a typical `S_speed ≈ 5.5 m/s`, 90s ≈ **~500 m of spine**, so a 1,600 m region yields **~4 spine shrines + 2–3 lobe shrines = 6–7 per region, ~35 shrines total**. That is close to ER's density in Limgrave and far below Genshin's waypoint density — correct for our pacing.

**Shrine verb set (bonfire-adjacent, feeds Step 10):** full heal, refill the healing-flask charges (ER **Flask of Crimson Tears** analog — call them *Sakazuki* charges), respawn non-boss enemies **and gathering nodes**, spend EXP to level, set respawn point, **tick quest/NPC state** (ER's "rest to advance a questline" — see Step 12, this gives one deterministic tick point that is trivially testable), commit autosave, and open the fast-travel map to any *discovered* shrine. Fast travel is available from Act I between discovered shrines and is hard-disabled while `arena_locked == true`.

**Stance loadout is explicitly NOT a shrine verb** — stance swap is a real-time combat action on Q/E (Step 3). Do not gate it behind rest.

### 9.3 Godot data structure for critical path + branches

Represent the region as a **graph `Resource` + a `Path3D` spine**, not as raw scene hierarchy. The `Path3D` is doing double duty: it is the pacing-measurement instrument (`Curve3D.get_baked_length()`, `Curve3D.get_closest_offset()`) *and* it can be reused directly as the Step 7 patrol `Path3D` source.

```gdscript
# res://resources/region_graph.gd
class_name RegionGraph extends Resource
@export var region_id: StringName                 # &"ashlands"
@export var display_name: String
@export var skyline_anchor: Vector3               # the sightline landmark
@export var nodes: Array[RegionNode]
@export var edges: Array[RegionEdge]
@export var critical_path: Array[StringName]      # ordered node_ids along the spine
@export var default_surface: StringName = &"ash"  # footstep fallback, see Step 13
```

```gdscript
# res://resources/region_node.gd
class_name RegionNode extends Resource
enum Kind { ENTRANCE, SHRINE, ENCOUNTER, ARENA, VISTA, LOOT, NPC, GATE, BOSS, SIDE_DOMAIN }
@export var node_id: StringName
@export var kind: Kind
@export var world_position: Vector3
@export var on_critical_path: bool = false
@export var required_flags: Array[StringName]     # world_flags gate
@export var content: PackedScene                  # streamed chunk / prop set
@export var intensity: float = 0.0                # 0..1 pacing weight for the tension curve
```

```gdscript
# res://resources/region_edge.gd
class_name RegionEdge extends Resource
enum EdgeType { CRITICAL, BRANCH, SHORTCUT_ONEWAY, SIGHTLINE }
@export var from_id: StringName
@export var to_id: StringName
@export var edge_type: EdgeType
@export var traversal_seconds: float              # authored estimate, validated against Path3D
```

**Scene layout per region:**

```
RegionRoot (Node3D)  [script holds @export var graph: RegionGraph]
├── Terrain            (greybox: CSGCombiner3D → baked to MeshInstance3D before ship)
├── Spine              (Path3D, Curve3D — the critical path; also the AI patrol source)
├── POI                (Marker3D per RegionNode, named == node_id, in group "poi")
├── Shrines            (instanced shrine.tscn, group "shrine")
├── Encounters         (spawner nodes, pooled per Step 14)
├── Occluders          (OccluderInstance3D — hand-authored for interiors, see 14.3)
└── Dressing           (MultiMeshInstance3D per species per chunk — Stage D)
```

**Pacing validation tooling (cheap, high value):** an `@tool EditorScript` that walks `critical_path`, uses `Spine.curve.get_closest_offset(shrine.global_position)` to get each shrine's arc-length offset, divides consecutive gaps by `S_speed`, and prints a pass/fail table against Constraints 1–3 above, plus an intensity sawtooth check (no more than 3 consecutive `ENCOUNTER` nodes without a `VISTA` or `SHRINE` breather). This makes "level pacing" a QA-testable property rather than a vibe, which the Step 5 QA gate needs.

**Stage D caveat (verified ✅):** Godot's occluder baking only considers `MeshInstance3D`. `MultiMeshInstance3D`, `GPUParticles3D`, `CPUParticles3D`, and **CSG nodes are ignored**. So the greybox→ship transition *must* include a "CSG → MeshInstance3D" bake step before occlusion baking, and the estate interior needs hand-placed occluder boxes. Note this in Step 9's definition of done or Step 14 will inherit an unfixable perf problem.

---

## Step 10 — Interactive Objects, Inventory Data & Gathering Economy

### 10.1 Where to sit on the Genshin↔Elden Ring itemization spectrum

**Recommendation: ~80% Elden Ring, 20% Genshin.** Concretely:

Genshin's economy exists to consume player time on a daily cadence: **Original Resin** (40/day cap) gates **Artifact Domains**, artifacts have RNG main-stat + 4 RNG substats with an upgrade lottery, **Character Ascension Materials** are gated behind weekly boss respawns, and **Local Specialties** respawn on a 48-hour real-world timer. Every one of those mechanics is retention scaffolding for a gacha service. Project Return is a ~20-hour single-player campaign with **no daily return incentive**, so all of it is not just unnecessary but actively harmful — it converts authored pacing into grind.

Elden Ring's restraint is the correct model: **the Crafting Kit is fully optional** (you can beat the game without crafting a single item), recipes are **found Cookbooks** rather than level-unlocks, and **power comes from level allocation + weapon upgrade (Smithing Stones) + Talisman choice**, not from farming variance.

Concrete positions:

| System | Decision | Reference |
|---|---|---|
| Artifacts / RNG substat gear | **Cut entirely** | Replace with ER **Talismans** → 3 charm slots (*Omamori*), ~12 hand-authored, flat deterministic effects, no rolls |
| Weapon upgrade | **One linear line, +0 → +10**, single material (**Tamahagane Ore**) + **Mon** | ER Smithing Stones, simplified to one material tier |
| Act gating of power | Ore *tier* availability gated per act (Act I drops Ore I, etc.) | ER's Smithing Stone tier zoning |
| Crafting | **Optional**, at shrines only, ~10 recipes found as *scrolls* in the world | ER Crafting Kit + Cookbooks |
| Local specialties | **Keep** — exactly one per region, spawns nowhere else | Genshin's Philanemo Mushroom / Silk Flower pattern |
| Node respawn | **On shrine rest**, never on a real-world clock | ER enemy respawn; explicitly rejects Genshin's 48h timer |
| Resin / energy gating | **Cut** | Monetization scaffolding, no analog needed |
| Chest tiering | **Keep the visual-language idea, 3 tiers** (Plain / Lacquered / Ancestral) with **hand-authored contents** | Genshin's chest tiers signal expected value; ER's hand-placement guarantees meaning |

The one genuinely valuable Genshin borrow is the **local specialty as a soft region-lock on an upgrade line**: it makes each region's gathering feel distinct and gives a reason to sweep a lobe you skipped. Assign one per region: Ashlands → *Ash-Lily*; Sunken Pines → **Wild Ginseng** (already named in the charter); Mount Shindai → *Frost Moss*; Outskirts → *Rice Straw*; Estate → *Ancestral Incense*.

### 10.2 Resource-based item & inventory data structures

```gdscript
# res://resources/item_data.gd
class_name ItemData extends Resource
enum Category { MATERIAL, LOCAL_SPECIALTY, CONSUMABLE, CHARM, KEY_ITEM, RECIPE, UPGRADE_MAT }

@export var item_id: StringName                   # &"tamahagane_ore"
@export var display_name: String
@export_multiline var description: String         # ER-style: this is a narrative channel, not flavour filler
@export var category: Category
@export var icon: Texture2D
@export var max_stack: int = 99                   # KEY_ITEM/CHARM = 1
@export var value_mon: int = 0                    # sell/buy base
@export var region_tag: StringName                # &"sunken_pines" for local specialties
@export var mesh: Mesh                            # world pickup representation
@export var effect: ItemEffect                    # null for pure materials
```

```gdscript
# res://resources/item_stack.gd
class_name ItemStack extends Resource
@export var item: ItemData
@export var quantity: int = 1
```

```gdscript
# res://resources/inventory.gd
class_name Inventory extends Resource
@export var stacks: Array[ItemStack] = []
var _index: Dictionary = {}                       # StringName -> int, rebuilt on load, NOT exported

func add(item: ItemData, qty: int) -> void        # splits across max_stack, emits EventBus.item_acquired
func remove(item_id: StringName, qty: int) -> bool
func count(item_id: StringName) -> int
func _rebuild_index() -> void                     # call after ResourceLoader.load
```

**Design notes that matter:**
- **Mon is not an `ItemStack`.** It's a scalar `int` on `PlayerData`, exactly as Genshin treats **Mora** and ER treats **Runes**. Putting currency in the inventory array creates a special case everywhere.
- **Deliberate divergence from Elden Ring:** ER's Runes are *simultaneously* currency and EXP, which produces its signature death-stakes loop (you drop your runes; retrieve them or lose them). Our charter has a **separate EXP formula and a separate Mon currency**, so that tension isn't available. Recommendation: don't retrofit rune-loss — instead make death cost a **fraction of unbanked EXP** (e.g. 30% of EXP earned since last shrine rest, recoverable once at the death location). It preserves the stakes without breaking the locked two-resource model. *(Flagged as a Director call — see Open Questions.)*
- `description` should be treated as a first-class narrative surface (ER's item-description storytelling). Budget one authored paragraph on every non-material item; this is the cheapest lore delivery in the project and directly reduces Step 12's dialogue burden.
- Do not export `_index` — `Dictionary` round-trips through `ResourceSaver` fine but a derived index that can desync is a save-corruption vector. Rebuild it in `_rebuild_index()` after load.

### 10.3 Interaction node pattern & EventBus wiring

```
Interactable (Area3D)                     [layer = INTERACTABLE, mask = PLAYER]
├── CollisionShape3D (SphereShape3D, radius 1.8)
├── PromptAnchor (Marker3D)               → billboard prompt spawn point
├── Highlight (MeshInstance3D, rim shader, hidden by default)
└── interactable.gd
    @export var prompt_text: String = "Interact"
    @export var interact_once: bool = false
    @export var required_flags: Array[StringName]
    @export var unique_id: StringName      # persisted in PlayerData.looted_containers
    func can_interact() -> bool            # flags + once-check
    func _on_interact() -> void            # virtual; overridden by Shrine/Chest/NPC/HarvestNode
```

Subclasses: `Shrine`, `Chest`, `HarvestNode`, `NpcInteractable`, `DoorInteractable`. Each is also added to a **group** (`"shrine"`, `"chest"`, `"npc"`, `"harvest"`) — this matters because the locked `EventBus.interaction_triggered(interactable_node: Node3D)` signature carries **only the node**, so any global listener must discriminate by group or by script class.

**Resolution flow (concrete, and it respects the Step 2/3 conventions already implemented):**

1. Player carries an `InteractionResolver` node maintaining `_candidates: Array[Interactable]` via `area_entered` / `area_exited`.
2. Each frame, pick the best candidate by **camera-forward ranking**, not pure distance — Genshin resolves toward screen-centre, ER resolves by proximity; camera-forward is the right hybrid for a third-person/isometric 3D camera:
   ```
   score = (cam_forward.dot((c.global_position - player.global_position).normalized()) * 0.7)
         + ((1.0 - clamp(dist / 1.8, 0, 1)) * 0.3)
   ```
   Highest score wins; ties broken by distance.
3. On change, emit a **new** signal `EventBus.interaction_available(node, prompt)` / `interaction_unavailable()` so the HUD prompt is signal-driven (Step 11) rather than polling the player node. **This signal does not exist yet — see Open Questions #4.**
4. On `interact` press: bail immediately if `GameState.is_player_input_locked()` (the convention established in Step 2 — this is exactly what it's for). Otherwise call `best._on_interact()`, which emits `EventBus.interaction_triggered(self)`.
5. **Consume the input-buffer entry** (Step 3's rolling 0.15s array) on a successful interact so the buffered press doesn't re-fire on the next frame — this is a real double-fire bug otherwise, given the buffer's design.

**Collision-layer discipline (measurable perf win, ties to Step 14):** interactables get their own layer and the player's resolver masks *only* that layer. Godot's broadphase bitmask rejection is dramatically cheaper than a GDScript `is Interactable` check inside a signal handler — reported ~40% signal-handler cost reduction in a 30-character stress test. This same discipline is what keeps the Step 5 hitbox sweeps affordable.

---

## Step 11 — Reactive HUD, UI Systems & Persistence Engine

### 11.1 HUD philosophy — recommendation: Elden Ring skeleton + a compass, no minimap

Elden Ring shows **HP / FP / Stamina** top-left, a thin **compass strip** top-centre, and nothing else during normal play — no minimap, no quest tracker, no objective list. Genshin shows a **minimap with elemental sighting pins, a party switcher (4 portraits + cooldowns + elemental energy rings), a quest tracker, a resin counter, and an event banner**. Genshin needs that density because it has a 4-character party with 7 elements and simultaneous live-service systems; Project Return has **one character, four stances, and no party**.

**Recommendation:**

- **Take from ER:** minimalist always-on vitals, no minimap, **compass strip** at top-centre showing only cardinal bearing + discovered shrine markers + (if a quest is tracked) a single objective bearing. This preserves ER's sightline-driven navigation while acknowledging our regions have branches.
- **Take from Genshin:** a **full map screen** (M key) with pins, revealed per-region on first shrine rest (the **Statue of the Seven** unlock pattern), and a lightweight **objective tracker** — but see Step 12 for why the tracker shows a *hint*, not a GPS waypoint.
- **Explicitly reject:** minimap, party switcher, energy/resin counters, event banners, damage-number spam (use ER's silence; damage numbers actively work against a posture/parry read).
- **Posture placement — take from Sekiro, not from either reference game:** *self* posture is a slim bar directly under player HP; *target* posture is a centre-screen bar under the lock-on reticle, and the boss's posture bar sits under the boss HP bar bottom-centre (ER's boss bar position). Posture is the primary combat read in a Sekiro-adjacent game — it must be where the player's eyes already are (on the enemy), not in the corner.
- **Stance display:** a 4-icon diamond bottom-right, active icon at `scale 1.15` + full alpha, inactive at `0.55` alpha. On `stance_swapped`, tween scale/alpha over 0.18s and flash the icon's border in the stance's signature colour. Four items is exactly the count where a diamond beats a list.
- **Out-of-combat fade:** all vitals tween to `alpha 0.25` after 5s with no combat signal and no resource change; snap to `1.0` instantly on any vital signal or on entering combat. This is a small thing that does a lot for the funeral-march tone, and it's free given everything is already signal-driven.

### 11.2 Control node tree, wired to the locked EventBus signals

```
HUD (CanvasLayer, layer = 1, process_mode = PROCESS_MODE_ALWAYS)
└── Root (Control, anchors_preset = FULL_RECT, mouse_filter = IGNORE)
    ├── VitalsPanel (MarginContainer, top-left)
    │   └── VBoxContainer
    │       ├── HealthBar     (Control: ChaseBar behind + TextureProgressBar front)
    │       ├── PostureBar    (TextureProgressBar, centre-fill)
    │       └── StaminaBar    (TextureProgressBar)
    ├── TargetPanel (Control, bottom-centre, hidden by default)
    │   ├── TargetName (Label)
    │   ├── TargetHealth (TextureProgressBar)
    │   └── TargetPosture (TextureProgressBar)
    ├── StanceDiamond (Control, bottom-right)  → 4 × TextureRect (stone/water/flame/wind)
    ├── CompassStrip (Control, top-centre)     → TextureRect + marker container
    ├── InteractPrompt (Control, centre-bottom, hidden)
    ├── NoticeQueue (VBoxContainer, centre)    ← EventBus.show_notice
    ├── ObjectiveTracker (Control, right-mid)  ← EventBus.quest_state_updated
    └── DamageVignette (ColorRect + shader)    ← EventBus.entity_damaged (target == player)
```

`hud.gd` connects everything in `_ready()`; **no child ever does `get_node("../../Player")`** — Call Down / Signal Up (charter §2) means the HUD is a pure sink.

```gdscript
func _ready() -> void:
    EventBus.player_health_changed.connect(_on_health)
    EventBus.player_stamina_changed.connect(_on_stamina)
    EventBus.player_posture_changed.connect(_on_posture)
    EventBus.stance_swapped.connect(_on_stance)
    EventBus.player_died.connect(_on_died)
    EventBus.entity_damaged.connect(_on_entity_damaged)
    EventBus.posture_broken.connect(_on_posture_broken)
    EventBus.parry_executed.connect(_on_parry)
    EventBus.show_notice.connect(_on_notice)
    EventBus.quest_state_updated.connect(_on_quest_state)
    EventBus.interaction_triggered.connect(_on_interaction)

func _on_health(current: float, max_health: float) -> void:
    health_bar.max_value = max_health   # ORDER MATTERS: set max first
    health_bar.value = current          # or the setter clamps against a stale max
    _wake_hud()
```

Two implementation details worth locking now because they're easy to get wrong later:

- **Set `max_value` before `value`.** All three vital signals carry `(current, max)`; if max grows on level-up and you set value first, `TextureProgressBar` clamps against the old max and the bar visibly stutters.
- **Damage-chase bar:** two stacked `TextureProgressBar`s. The front bar snaps to the new value instantly; the rear (desaturated red) bar tweens to the same value over **0.35s after a 0.20s hold**. This is the cheapest single piece of Step 6 juice available and it reads at a glance in a posture game.
- **HUD must be `PROCESS_MODE_ALWAYS`** — Step 2 already establishes that `PAUSED` sets `get_tree().paused = true`, so any UI that must animate while paused needs this. The existing autoloads already do it; keep the convention.

**Missing signals.** The locked EventBus has no way to drive the EXP bar, item pickup toasts, interaction prompts, or save confirmation. Recommended additions (a charter amendment, Director's call):

```gdscript
signal player_exp_changed(current_exp: int, level: int, exp_to_next: int)
signal player_level_up(new_level: int)
signal item_acquired(item_id: StringName, quantity: int)
signal interaction_available(interactable_node: Node3D, prompt: String)
signal interaction_unavailable()
signal shrine_rested(shrine_id: StringName)
signal game_saved(slot: int)
signal target_locked(target: Node3D)
signal target_released()
```

### 11.3 Save data shape and policy

**Policy recommendation: Elden Ring model — a single live save per playthrough, autosaved at checkpoints, no manual save slots, no save-scumming.** Genshin's account-bound cloud save is irrelevant (no server, no account). ER's "quit to desktop to save" is a legacy artifact of its always-autosaving design — take the autosave, skip the ritual. Offer **3 playthrough slots** at the main menu (three characters), each with one live save.

**Autosave triggers:** shrine rest · boss defeat · region transition · any `quest_state_updated` · key-item acquisition · stance unlock · quit-to-menu.

```gdscript
# res://resources/player_data.gd
class_name PlayerData extends Resource

# --- meta ---
@export var save_version: int = 1
@export var slot_id: int = 0
@export var playtime_seconds: float = 0.0
@export var saved_at_unix: int = 0
@export var current_act: int = 0

# --- progression ---
@export var level: int = 1
@export var exp_total: int = 0
@export var exp_unbanked: int = 0          # at-risk EXP since last rest (see 10.2)
@export var mon: int = 0
@export var stat_points_unspent: int = 0
@export var stats: Dictionary = {}         # &"body"/&"breath"/&"blade"/&"spirit" -> int
@export var max_health: float = 100.0
@export var max_stamina: float = 100.0
@export var max_posture: float = 100.0
@export var unlocked_stances: Array[StringName] = [&"stone_base"]
@export var active_stance_id: StringName = &"stone_base"

# --- world ---
@export var current_region_id: StringName
@export var respawn_shrine_id: StringName
@export var discovered_shrines: Array[StringName] = []
@export var revealed_regions: Array[StringName] = []
@export var bosses_defeated: Array[StringName] = []
@export var looted_containers: Array[StringName] = []   # Interactable.unique_id, once-only
@export var harvest_cooldowns: Dictionary = {}          # node_id -> rest_count_when_taken
@export var world_flags: Dictionary = {}                # StringName -> bool/int

# --- inventory ---
@export var inventory: Inventory
@export var equipped_charms: Array[StringName] = []     # max 3
@export var weapon_upgrade_level: int = 0
@export var flask_charges_max: int = 4

# --- narrative ---
@export var quest_states: Dictionary = {}               # quest_id -> QuestData.State (int)
@export var quest_objectives: Dictionary = {}           # quest_id -> { objective_id: bool }
@export var tracked_quest_id: StringName
@export var dialogue_seen: Array[StringName] = []
@export var npc_states: Dictionary = {}                 # npc_id -> int (relocation stage)

# --- settings that belong to the run, not the machine ---
@export var difficulty_flags: Dictionary = {}
```

**Two engineering notes:**

1. **Atomic writes.** `ResourceSaver.save()` directly over the live save file will corrupt it on a mid-write crash or power loss. Write to `user://saves/slot_N.tmp`, verify the load round-trips, then `DirAccess.rename_absolute()` over the real file. Keep one rolling backup (`slot_N.bak`).
2. **Security caveat (verified ✅, and it conflicts with the charter).** Godot `Resource` files can embed scripts, so `ResourceLoader.load()` on a user-editable `.tres` is an **arbitrary-code-execution vector** — a malicious "save file" shared online executes on load. The charter locks `ResourceSaver`/`ResourceLoader` on `PlayerData.tres`. Mitigations, in order of preference: (a) keep `PlayerData` as the in-memory schema but serialize via `to_dict()`/`from_dict()` + `FileAccess.store_var(dict, false)` — `full_objects = false` disables object serialization entirely and removes the vector; (b) save as binary `.res` and load with `ResourceLoader.load(path, "PlayerData", CACHE_MODE_IGNORE)` plus a type assertion; (c) use a safe-resource-loader wrapper that scans for embedded scripts before loading. **Recommend (a)** — it's strictly safer and costs one method pair. Flagged for the Director since it touches a locked charter line.
3. **Migration:** `save_version` + a `_migrate(data: Dictionary, from: int, to: int)` chain. Add it at version 1, not at version 3 when it's already painful.

---

## Step 12 — Narrative Engine, Dialogue Trees & Quest State Machine

### 12.1 Quest structure — recommendation: "Archon spine, Grace threads"

Genshin splits narrative into **Archon Quests** (the main spine — linear, unmissable, fully voiced, high production, act-gated by Adventure Rank), **World Quests** (optional, regional, often multi-stage, NPC-driven, tracked in the journal), and **Story/Hangout Quests** (character-focused, branching). Elden Ring does almost the opposite: the "main quest" is a handful of fog walls and a Two Fingers monologue, and essentially all characterisation lives in **item descriptions** and **NPC questlines** (Ranni, Millicent, Blaidd) that are sparse, geographically scattered, and infamously easy to permanently fail without ever knowing they existed.

Project Return can't be pure ER, because **the fiction has explicit emotional beats that require dialogue**: Soren the Hollowed is Jin's former brother-in-arms; that scene doesn't land via an item description. It also can't be pure Genshin, because the tone is a funeral march and Genshin's quest presentation is chatty, high-frequency, and warm.

**Recommendation — concrete hybrid:**

| Layer | Model | Count | Production |
|---|---|---|---|
| **Act Quests** | Genshin **Archon Quest** — linear, unmissable, gated by boss defeat, camera-framed | 5 (+ Prologue) | Full dialogue trees, authored camera shots, VO slots |
| **Boss beats** | FromSoft pre-fight/post-fight monologue + fog-wall gravity | 6 (Renzo, Kuroda, Soren, Masato, Mei, Osamu) | Full trees; **Soren gets real branching** — a "lower your blade" option that the player can take and that *fails*, because that's the point |
| **NPC threads** | ER **questline** structure (multi-stage, relocating NPCs, advanced by resting) with Genshin **journal tracking** | 8–12 total, 2–3 per region | Medium; hint-tracked, not waypoint-tracked |
| **Environmental / item lore** | ER **item descriptions** verbatim as a model | ~60 items | One authored paragraph each |

**The one thing to deliberately NOT copy from Elden Ring: the opacity.** ER's questlines require a wiki. Keep the *tone* (ambiguous, sparse, no exclamation marks over heads) but add Genshin's **Adventure Handbook**-lite affordance: a Journal screen listing active threads with the **last hint the NPC actually gave you** ("The fisherman said he'd wait by the sunken torii until the water rose") — a hint string, not a GPS pin. This is one field on `QuestData` and it converts an infamous ER pain point into a strength.

**The one thing to copy from Elden Ring exactly: rest advances the world.** ER progresses NPC states when you rest at a grace. Our shrines already do everything else (§9.2); making them the **single deterministic quest tick point** means (a) NPC relocation is legible to the player, (b) QA has one reproducible trigger to test against, and (c) save-state consistency is trivial because world mutation happens at exactly the same moments as autosave.

### 12.2 Quest data & FSM

```gdscript
# res://resources/quest_data.gd
class_name QuestData extends Resource
enum State { UNSTARTED = 0, ACTIVE = 1, OBJECTIVE_COMPLETE = 2, COMPLETED = 3 }  # locked by charter

@export var quest_id: StringName
@export var title: String
@export var is_main: bool = false
@export var region_id: StringName
@export var prerequisites: Array[StringName] = []      # quest_ids that must be COMPLETED
@export var required_flags: Dictionary = {}
@export var objectives: Array[QuestObjective] = []
@export_multiline var journal_hint: String             # the Genshin-borrowed affordance
@export var rewards: Array[ItemStack] = []
@export var reward_mon: int = 0
@export var reward_exp: int = 0
@export var on_complete_flags: Dictionary = {}
```

```gdscript
# res://resources/quest_objective.gd
class_name QuestObjective extends Resource
enum Kind { KILL, REACH, TALK, DELIVER, COLLECT, REST, FLAG }
@export var objective_id: StringName
@export var kind: Kind
@export var target_id: StringName          # enemy_id / node_id / npc_id / item_id / flag
@export var count: int = 1
@export var optional: bool = false
@export var hidden: bool = false           # not shown until a prior objective completes
@export_multiline var description: String
```

`QuestManager` (a node under `GameState`, or a fourth autoload) owns `Dictionary quest_id -> State` and is the only writer:

```gdscript
func set_quest_state(quest_id: StringName, new_state: QuestData.State) -> void:
    if _states.get(quest_id, QuestData.State.UNSTARTED) == new_state: return
    _states[quest_id] = new_state
    EventBus.quest_state_updated.emit(quest_id, int(new_state))   # matches locked signature
```

The locked signature `quest_state_updated(quest_id: String, state: int)` takes `String` while everything else here uses `StringName` — GDScript will coerce, but pick one convention and note the coercion so the HUD's dictionary lookups don't silently miss. **Recommend `StringName` everywhere and amending the signal signature** (trivial, but it's a charter line).

**Gap: the locked FSM has no FAILED state.** Several threads are genuinely failable — killing an NPC, choosing the wrong branch with Soren, missing a timed relocation. Options: (a) add `FAILED = 4`; (b) express failure as `COMPLETED` plus a `world_flag` outcome. Recommend **(a)** — it keeps the journal honest and lets the UI grey out a dead thread instead of lying that it's done. Director's call since the enum is charter-named.

### 12.3 Dialogue tree structure

**Recommendation: a custom `Resource` graph stored as an ID-keyed `Dictionary`, not a node-reference graph, not JSON, not a third-party plugin.**

- Not JSON: the charter locks Resource-based data, and `.tres` gets editor tooling, type safety, and `Texture2D`/`AudioStream` references for free.
- Not a node-reference graph (each node holding `next: DialogueNode`): cyclic resource references round-trip badly through `ResourceSaver`, and dialogue graphs *are* cyclic (hub-and-spoke "ask about…" menus).
- Not Dialogic or similar: it brings its own autoload, its own state model, and its own save system, all of which fight the EventBus/GameState conventions already implemented in Step 2.

```gdscript
# res://resources/dialogue_tree.gd
class_name DialogueTree extends Resource
@export var dialogue_id: StringName
@export var start_node: StringName
@export var nodes: Dictionary = {}          # StringName -> DialogueNode   (O(1) jump, acyclic storage)
@export var one_shot: bool = false          # recorded in PlayerData.dialogue_seen
```

```gdscript
# res://resources/dialogue_node.gd
class_name DialogueNode extends Resource
@export var node_id: StringName
@export var speaker_id: StringName          # &"jin", &"soren"
@export_multiline var text: String
@export var portrait: Texture2D
@export var voice_clip: AudioStream
@export var camera_shot: StringName         # named Marker3D in the scene → cinematic framing
@export var anim_cue: StringName            # AnimationTree state on the speaker (Step 13)
@export var choices: Array[DialogueChoice] = []   # empty => auto-advance via next_id
@export var next_id: StringName
@export var on_enter_flags: Dictionary = {}       # world_flags mutations
@export var on_enter_quests: Array[QuestMutation] = []   # quest_id + new State
@export var conditions: Array[DialogueCondition] = []    # gate this node's reachability
```

```gdscript
# res://resources/dialogue_choice.gd
class_name DialogueChoice extends Resource
@export_multiline var text: String
@export var next_id: StringName
@export var conditions: Array[DialogueCondition] = []
@export var hide_if_unavailable: bool = false      # false => show greyed with a reason
@export var consume_item: StringName
@export var consume_qty: int = 0
```

```gdscript
# res://resources/dialogue_condition.gd
class_name DialogueCondition extends Resource
enum Source { WORLD_FLAG, QUEST_STATE, ITEM_COUNT, LEVEL, STANCE_UNLOCKED, BOSS_DEFEATED }
enum Op { EQ, NEQ, GTE, LTE }
@export var source: Source
@export var key: StringName
@export var op: Op = Op.EQ
@export var value: Variant
```

**Use a whitelisted condition Resource, not `Expression`.** `Expression.parse()`/`execute()` on authored strings is both a code-execution surface and untestable; a typed condition struct is validatable by an `@tool` linter that can walk every tree and assert every `next_id` resolves.

**Runtime (`DialogueRunner`, `CanvasLayer` + `Control`), wired to the existing Step 2 conventions:**

1. `GameState.set_state(GameState.State.DIALOGUE)` — per charter §2.2 this keeps `get_tree().paused = false` (so ambience, cloth physics, and the AnimationTree keep ticking, which is exactly right for an in-world conversation) while `is_player_input_locked()` returns `true`, blocking movement/attacks. **This is the already-implemented convention doing its job — no new plumbing needed.**
2. Typewriter reveal via `RichTextLabel.visible_ratio` tweened at **35 chars/s**; first `interact` press completes the line, second advances. Standard, and it must respect the Step 3 buffer's consumption rule so one press doesn't do both.
3. `on_enter_flags` / `on_enter_quests` apply **on node display**, not on node exit — otherwise a player who alt-F4s mid-conversation loses the state change they visibly triggered.
4. Ducking: `SoundManager` drops the Music bus −4 dB while in `DIALOGUE`.
5. On exit: `GameState.set_state(State.PLAYING)`, append to `PlayerData.dialogue_seen` if `one_shot`, trigger an autosave if any quest state changed.

---

## Step 13 — Production Art, Animation Blend Trees, Shaders & Audio

### 13.1 AnimationTree structure — verified against Godot 4.7 docs ✅

Confirmed from the current docs (not assumed):
- Root node types available: `AnimationNodeAnimation`, `AnimationNodeBlendTree`, `AnimationNodeBlendSpace1D`, `AnimationNodeBlendSpace2D`, `AnimationNodeStateMachine`. ✅
- `AnimationNodeBlendSpace2D` does **linear blending between three animation nodes** via automatic **Delaunay triangulation** of the placed points. ✅
- `AnimationNodeStateMachine` transitions come in **Immediate / Sync / At End** flavours, and `AnimationNodeStateMachinePlayback.travel()` uses **A\*** to path through intermediate states. ✅
- **Masking is done with filters on `Blend2`/`Blend3`**, which "support filters to control individually which tracks get blended" — this is the documented mechanism for layering upper-body animation over lower-body. There is no separate layer-mask resource. ✅
- `AnimationNodeBlendTree` inner nodes include `Transition`, `Blend2`, `Blend3`, `OneShot`, `Output`. ✅
- Root motion via `root_motion_track` + `get_root_motion_position()` etc., designed to feed `CharacterBody3D.move_and_slide()`. ✅

**So the proposed structure in the task brief is correct for Godot 4.7**, with one addition (the OneShot layer for full-body actions that must bypass the mask):

```
AnimationTree.tree_root = AnimationNodeBlendTree "Root"

  Locomotion   : AnimationNodeBlendSpace2D
                 x = strafe (-1..1), y = forward (-1..1), blend_mode = INTERPOLATED
                 points: idle(0,0) walk_f(0,0.5) run_f(0,1) walk_b(0,-0.6)
                         strafe_l(-1,0) strafe_r(1,0) run_diag_l(-0.7,0.7) run_diag_r(0.7,0.7)
                 → driven from Step 4's V_horizontal projected into mesh-local space,
                   NOT from raw input, so the lerped inertia reads in the animation

  StanceLoco   : AnimationNodeTransition (4 inputs, xfade 0.25s)
                 one Locomotion blendspace per stance (Stone = wide/heavy, Water = light/forward,
                 Flame = broad guard, Wind = high/loose). Driven by EventBus.stance_swapped.

  CombatSM     : AnimationNodeStateMachine
                 Idle · LightA1→A2→A3 · Heavy1→Heavy2 · Parry · ParryCounter · Block · BlockHit
                 · HitLight · HitHeavy · PostureBreak · Deathblow · Death · Draw · Sheathe · GripChange
                 transitions: AtEnd for combo links (A1→A2), Immediate for interrupts
                 (Parry / HitLight can interrupt anything), xfade 0.08–0.12s
                 driven from GDScript: playback.travel(&"LightA1")

  AtkSpeed     : AnimationNodeTimeScale   (between CombatSM and UpperBlend)
                 scale = active_stance.attack_speed_scalar    ← the clean home for the locked
                 0.85x etc. from StanceData, instead of poking speed_scale ad hoc

  UpperBlend   : AnimationNodeBlend2, filter_enabled = true
                 in0 = StanceLoco, in1 = AtkSpeed(CombatSM)
                 filters: Skeleton3D:Spine1..Spine3, Neck, Head, both Clavicles/Shoulders/
                          Arms/Hands, WeaponAttachment
                 amount: 0.0 out of combat → tween to 1.0 over 0.12s on attack start

  FullBody     : AnimationNodeOneShot   (AFTER UpperBlend, so it overrides the mask)
                 for Dodge, PostureBreak, Deathblow, Death, Draw, Sheathe — these must not be
                 masked to the upper body

  Output
```

**Two consistency notes with locked steps:**
- **Hit-stop is free.** Step 6 sets `Engine.time_scale = 0.0`; `AnimationTree` is process-driven, so it freezes automatically. The resume timer must be `get_tree().create_timer(d, true, false, true)` (process_always + **ignore_time_scale**) — already correctly specified in §6.1. No extra animation work.
- **Root motion vs Step 4's velocity lerp.** Step 4 locks `V_horizontal = lerp(V_horizontal, V_target, alpha*delta)`. Root motion would fight that. **Recommendation: root motion OFF for locomotion, ON for attack/dodge states only** — during `CombatSM` attack states, override `V_horizontal` with `get_root_motion_position() / delta` rather than lerping toward input. Step 4's formula governs *locomotion*; attacks are a different state, so this doesn't contradict the locked spec — but it is an interpretation, so flagged.

### 13.2 How stance switching should read visually — recommendation: no sheathe/draw beat

The task brief asks whether stance switching needs a visible sheathe/draw beat "like Sekiro's tool switch." Worth being precise about the reference: **Sekiro's prosthetic switch is near-instant and cancel-friendly** precisely because it happens mid-combat; the sheathe/draw beat in Sekiro belongs to *combat entry/exit* and to the sheathed Ashina Cross art, not to tool selection. **Nioh's** stance switching — the closest real analog to a 4-stance system — is roughly 10 frames and cancelable *inside a combo*, which is the whole reason the system has depth.

**Recommendation:**
- **Stance swap costs no sheathe.** It's bound to Q/E and must be usable inside a combo; a 0.5s sheathe/draw would kill the system.
- Instead: a **0.18s upper-body-only "grip change" flourish** routed through `CombatSM → GripChange` with `UpperBlend` active, so **locomotion is never interrupted**.
- Reinforce the read with cheap, high-signal cues rather than animation time: (a) the Step 6 `Trail3D` ribbon material swaps to the stance's signature colour immediately; (b) a distinct 0.3s SFX per stance (Stone = low iron scrape, Water = a wet ring, Flame = a hard rasp, Wind = a whisper); (c) the HUD diamond tween (§11.1); (d) a one-frame `flash_intensity` pulse on the blade material (reuses the Step 6 hit-flash shader for free).
- **Commitment window = 0.15s**, matching the Step 3 input buffer exactly. The new stance's multipliers apply immediately on swap, so an attack buffered during the swap lands in the new stance — predictable, and it makes buffered stance-into-attack a deliberate skill expression rather than a lottery.
- **Reserve the full sheathe/draw for:** entering/leaving combat (out-of-combat idle holds the sword sheathed at the waist — supports the charter's "waist scabbard facing direction" silhouette rule), and the post-boss sheathe flourish (FromSoft's "Great Enemy Felled" beat, our biggest earned moment × 6).

### 13.3 Material-tagged footstep system

Godot's `PhysicsMaterial` has no surface-type field, so tag the collider directly:

```gdscript
# On every ground collider / MeshInstance3D StaticBody3D:
#   set_meta(&"surface", &"snow")
# Sampled at the exact contact frame:

func _footstep(foot: StringName) -> void:      # called from an AnimationPlayer method-call track
    var origin := (left_foot if foot == &"L" else right_foot).global_position + Vector3.UP * 0.3
    _surface_ray.global_position = origin
    _surface_ray.force_raycast_update()
    var surf := region_default_surface
    var c := _surface_ray.get_collider()
    if c and c.has_meta(&"surface"):
        surf = c.get_meta(&"surface")
    SoundManager.play_footstep(surf, origin, _gait_intensity)
```

Using an `AnimationPlayer` **method-call track** at the contact frame is the same technique Step 5 already uses for `enable_hitbox()`/`disable_hitbox()` — one convention, one thing for QA to verify.

**Surface set per region:** Ashlands → `ash`, `cinder`, `stone`; Sunken Pines → `mud`, `shallow_water`, `wet_wood`, `moss`; Mount Shindai → `snow`, `ice`, `stone`, `gravel`; Outskirts → `dirt`, `grass`, `gravel`, `plank`; Estate → `wood`, `tatami`, `stone`, `gravel_garden`. Tatami is worth its own sample — it's the sound of arriving home, and it should be the softest footstep in the game.

`SoundManager.play_footstep(surface, position, intensity)` pulls from a **pool of 8 `AudioStreamPlayer3D`** (per Step 14's pooling mandate), randomises pitch ±0.08, and uses an `AudioStreamRandomizer` per surface for sample variation ⚠️ *(class name to confirm against 4.7 docs at implementation time)*.

**Bus layout:** `Master → Music`, `SFX → {Combat, World, Footsteps}`, `Ambience`, `UI`, `Voice`. Sidechain-duck `Music` and `Ambience` under `Voice` during `DIALOGUE`, and under `Combat` on hit-stop frames.

### 13.4 Adaptive music — recommendation: Elden Ring sparseness, not Genshin layering

Genshin runs continuous melodic exploration score that layers into combat variants on aggro. Elden Ring runs **no music at all** across most of the open world and most combat — ambience only — and reserves full orchestral/choral scoring for **boss fights and a handful of set-piece locations**. The silence is what makes the boss themes land.

Project Return's fiction is a somber walk home carrying a dead lord's medallion. **Recommendation: Elden Ring model, near-total.**

| Context | Music | Implementation |
|---|---|---|
| Exploration | **No melody.** 2–3 crossfading ambience beds per region (wind through cinder, dripping water, cicadas, distant temple bells), blended by altitude/interior/weather | `AudioStreamPlayer` per bed, tween `volume_db` |
| Normal combat | **No music.** Duck ambience −6 dB, add a low percussive pulse layer on the Combat bus | The "music" is the swordplay; posture reads better in silence |
| Boss fights | **Full FromSoft treatment** — taiko + shakuhachi + choir, 6 named themes | `AudioStreamInteractive` ✅ |
| Shrine rest | An 8–12s solo shakuhachi/koto motif, then silence | The one warm moment in the loop; ER's grace hum analog |
| Act transitions | Full cue over the fade | — |

**Boss music implementation (verified ✅):** `AudioStreamInteractive` supports `clip_count`, `set_clip_stream()`, `set_clip_name()`, `add_transition()` with `CLIP_ANY` wildcards, fade modes `FADE_DISABLED / FADE_IN / FADE_OUT / FADE_CROSS / FADE_AUTOMATIC`, **auto-advance**, **filler clips**, and **hold-previous**. Per boss:

```
clip 0: intro_sting     (auto-advance → clip 1)
clip 1: phase1_loop
clip 2: transition_filler   ← the FILLER clip, fires on the locked 50% HP threshold (§8.1)
clip 3: phase2_loop
transitions: 0→1 FADE_AUTOMATIC auto-advance
             1→3 FADE_CROSS via filler clip 2
             CLIP_ANY→(stop) FADE_OUT on boss death or player death
```

The phase-2 transition is triggered from the boss FSM at the exact same moment as the §8.1 invulnerability + AoE knockback, so the music swell and the mechanical phase change are frame-aligned. That's the single highest-value audio moment in the project.

**Leitmotif note (free emotional yield):** Lord Osamu's theme should quote Soren's motif. Soren is "Jin's former brother-in-arms"; the final boss echoing the friend Jin had to kill costs the composer nothing extra and does more narrative work than a cutscene.

---

## Step 14 — Economy Balancing, Profiling, Optimization & QA

### 14.1 EXP curve sanity check — formula unchanged, but it needs a companion gate

**First, an ambiguity to resolve:** `EXP = 100 × Level^1.5` doesn't state whether it's the **cost of the next level** or the **cumulative total to reach a level**. Both readings are common. Numbers under each:

| Level | Per-level cost `100·L^1.5` | Cumulative (`Σ`, ≈ `40·L^2.5`) |
|---|---|---|
| 2 | 100 | ~230 |
| 10 | 3,162 | ~12,600 |
| 25 | 12,500 | ~125,000 |
| 50 | 35,355 | ~707,000 |
| 75 | 64,952 | ~1,950,000 |
| 99 | 98,499 | ~3,900,000 |

**Comparison to the reference curves:**

- **Elden Ring's rune cost** is effectively **cubic** (≈ `0.02L³ + 3.06L² + 105.6L − 895` for L ≥ 12): L10 ≈ 487 runes, L50 ≈ 14,535, L100 ≈ 60,265. The **L10→L100 cost ratio is ~124×**.
- **Our curve at exponent 1.5** gives a **L10→L100 ratio of ~31.6×** — roughly **4× flatter than a Soulslike**.
- **Genshin** doesn't solve this with curve steepness at all; it solves it with **hard Ascension caps** (20/40/50/60/70/80) that simply refuse to let you level past a phase boundary.

**The concrete concern: `L^1.5` is meaningfully back-loaded-*insufficiently*.** Because each level's marginal cost grows slowly while enemy EXP yields will necessarily grow act-over-act (an Act V soldier can't be worth what an Act I one is), **the player's level will outrun the content** unless enemy yields are tuned with unusual discipline. This is the classic flat-exponent failure mode: the player over-levels Act III, Acts IV–V trivialise, and the emotional climax lands against a boss that dies in nine seconds.

**Recommendation (does not touch the locked formula): borrow Genshin's Ascension cap directly.** Impose an **act-gated level cap that raises on boss defeat**, alongside the stance reward that already exists there:

| Act | Boss | Stance reward | **Level cap after** |
|---|---|---|---|
| Prologue | Renzo | — | 8 |
| I Ashlands | Kuroda | Stone | 20 |
| II Sunken Pines | Soren | Water | 35 |
| III Mount Shindai | Masato | Flame | 50 |
| IV Outskirts | Mei | Wind | 68 |
| V Estate | Osamu | — | 85 |

This preserves the charter formula exactly while fixing its pacing weakness, and it reuses a reward moment that already exists in the fiction (each boss already grants a stance; granting a cap raise at the same beat costs zero design surface).

**Derived enemy-EXP budget (cumulative reading, ~70% of cap from a full clear):**

- End of Act I, target L20 → cumulative ≈ 71,600 EXP. With ~120 trash + 12 elites, ~70% from a full clear ⇒ **~385 EXP average per Act I kill.**
- End of Act V, target L85 → cumulative ≈ 2,660,000; delta from L68 (≈1,525,000) = **1,135,000 over ~180 kills ⇒ ~6,300 EXP per Act V kill.**

**That's a ~16× spread in per-kill yield across the campaign with roughly flat enemy counts** — which is a lot, and it means `EventBus.enemy_killed(enemy_node, exp_reward)` must carry a **per-archetype, per-region authored value** (a field on an `EnemyData` resource), never a global runtime formula. Flag for the balancing pass.

### 14.2 Damage formula sanity check

`Damage = (Base + Weapon) × (1 − Armor/(Armor + 100))` is the standard hyperbolic mitigation curve (League/Dota family). It's well-behaved: monotonic, asymptotic to 100%, never negative, no discontinuities. Mitigation table:

| Armor | 0 | 25 | 50 | 100 | 150 | 200 | 300 | 500 | 1000 |
|---|---|---|---|---|---|---|---|---|---|
| Mitigation | 0% | 20% | 33% | **50%** | 60% | 66.7% | 75% | 83.3% | 90.9% |

**The knee is at Armor = 100 (50%), which is aggressive.** Recommendation for the balancing pass: **budget armor 0–250 for regular enemies and 150–300 for bosses**, keeping effective mitigation in the 0–75% band. Above ~500 the curve flattens so hard that further armor stops mattering while player damage scaling has to compensate multiplicatively — that's how TTK curves collapse.

**Four gaps to flag (none require changing the locked formulas):**

1. **No level term anywhere, and the charter never states what a level *grants*.** EXP → Level is locked; Level → power is undefined. Recommend the ER model — levels grant **stat points the player allocates** (Body/Breath/Blade/Spirit → HP / stamina+posture / damage / posture-damage), not automatic growth. This preserves ER's "power comes from build choices, not grinding" position that §10.1 already commits to, and it makes the flat EXP curve less dangerous because points spread across four stats instead of all going to damage.
2. **Posture is the real TTK dial and the damage formula doesn't govern it at all.** Posture damage rides on `StanceData.posture_damage_multiplier` (locked). What's *not* specified is **posture regeneration**, which is where Sekiro actually hides its difficulty. Recommend adding: `R(hp) = R_base × (0.35 + 0.65 × hp_ratio)` — enemies regain posture more slowly as their HP drops, which is precisely why Sekiro's "chip HP to enable a deathblow" loop feels good. This is a new formula and needs approval.
3. **Block ordering is undefined.** §5.2 specifies 80% block mitigation and full posture damage, but not whether block applies before or after armor. Recommend `final = (Base+Weapon) × (1 − Armor/(Armor+100)) × 0.20`. At Armor 300 + block that's 5% of raw — effectively zero chip, comparable to ER behind a good greatshield. Fine, but it should be written down.
4. **`entity_damaged(..., is_critical: bool)` implies crits, but no crit formula is locked.** Recommend **crits are not a probability roll** — they're **deathblow/posture-break executions only** (Sekiro model). A percentage crit chance injects RNG into a parry-timing game, which is exactly the wrong genre for it. This also means `is_critical` becomes a clean, deterministic flag the HUD and Step 6 juice engine can key off.

### 14.3 Profiling categories and budgets

**Frame budget at 60 FPS = 16.6 ms.** Suggested split:

| Bucket | Budget | Notes |
|---|---|---|
| GDScript `_process` | ≤ 4.0 ms | AI FSMs, HUD, camera |
| `_physics_process` | ≤ 4.0 ms | 60 ticks/s; movement, hitbox sweeps, perception |
| Render CPU (culling + draw submission) | ≤ 3.5 ms | Draw-call bound |
| GPU | ≤ 13.0 ms | Overlaps CPU; the hard ceiling |
| Headroom | ~2 ms | For hitches, streaming, GC |

**Monitors to chart (exact `Performance.Monitor` names, verified ✅ against 4.7):**

`TIME_PROCESS` · `TIME_PHYSICS_PROCESS` · `TIME_FPS` · `RENDER_TOTAL_DRAW_CALLS_IN_FRAME` · `RENDER_TOTAL_OBJECTS_IN_FRAME` · `RENDER_TOTAL_PRIMITIVES_IN_FRAME` · `RENDER_VIDEO_MEM_USED` · `RENDER_TEXTURE_MEM_USED` · `PHYSICS_3D_ACTIVE_OBJECTS` · `PHYSICS_3D_COLLISION_PAIRS` · `PHYSICS_3D_ISLAND_COUNT` · `OBJECT_NODE_COUNT` · `MEMORY_STATIC`

**Proposed budgets (1080p, mid-range target GPU):**

| Metric | Budget | Rationale |
|---|---|---|
| `RENDER_TOTAL_DRAW_CALLS_IN_FRAME` | **≤ 2,000** | The primary lever; Stage D's `MultiMeshInstance3D` dressing is what keeps this reachable |
| Visible `MultiMeshInstance3D` count | ≤ 40 | 1 draw call each — one per foliage species per terrain chunk |
| `PHYSICS_3D_COLLISION_PAIRS` | ≤ 400 in a 6-enemy fight | Blows up fast if hitbox layers are sloppy |
| `OBJECT_NODE_COUNT` | ≤ 8,000 | Above this, tree traversal costs show up in `TIME_PROCESS` |
| Live spark particles | ≤ 768 (24 pooled emitters × 32) | See below |
| `RENDER_VIDEO_MEM_USED` | ≤ 2.5 GB | Leaves room for 4 GB cards |

**The three categories that actually matter for this specific game:**

**(a) Physics tick cost from Area3D hitbox sweeps.** Step 5 already gets this right by gating `monitoring` to active animation frames — reinforce two rules: **disable the `CollisionShape3D`, not the `Area3D` node** (Godot's documented guidance), and use **strict paired collision layers** so the broadphase does the filtering instead of GDScript type checks. Proposed layer map:

```
1 World · 2 Player · 3 Enemy · 4 PlayerHitbox · 5 EnemyHitbox
6 PlayerHurtbox · 7 EnemyHurtbox · 8 Interactable · 9 Perception · 10 ArenaTrigger
```
with `PlayerHitbox.mask = {EnemyHurtbox}` only, etc. A bitmask rejection in the broadphase is vastly cheaper than an `is Hurtbox` cast in a signal handler (~40% signal-handler cost reduction measured in a 30-character stress test).

Also: **stagger the Step 7 perception sweeps.** §7.1 runs perception every 0.1s = every 6 physics ticks; if 8 enemies all evaluate on the same tick you get a 6× spike every 100 ms that shows up as visible hitching during a fight. Give each enemy a `_perception_offset = randf() * 0.1` at spawn.

**(b) Particle count from the Step 6 juice engine.** Never `instantiate()` a `GPUParticles3D` per hit — in a Flame-stance crowd cleave that's 6 allocations in one frame. Pool **24 spark emitters** (`one_shot = true`, reset `emitting = true` on reuse), 32 particles each. Set `fixed_fps = 30` on non-hero VFX. Set `visibility_aabb` explicitly on every particle system or they'll be incorrectly culled at the edges of the view (a classic silent bug). One `Trail3D` ribbon per active weapon, never per swing.

**(c) Occlusion culling and MultiMesh (verified ✅ — this is a real trap).** `OccluderInstance3D` baking **only considers `MeshInstance3D`**. `MultiMeshInstance3D`, `GPUParticles3D`, `CPUParticles3D`, and **CSG nodes are ignored**. Two consequences for Stage D:
- Greybox `CSGCombiner3D` geometry **must be baked to `MeshInstance3D`** before occluders are baked, or the estate interior and Mount Shindai's terrain will occlude nothing.
- Combine occlusion culling with **mesh LOD** and **visibility ranges (HLOD)** via `GeometryInstance3D.visibility_range_begin/end` — the docs specifically recommend the combination, and it's most effective in the interior-heavy Act V estate. Note also that the **Forward+ renderer already does a depth prepass**, so occlusion culling's marginal gain there is smaller than on Mobile — set expectations accordingly rather than treating it as a silver bullet.

**(d) Jolt caveat ⚠️.** `project.godot` sets `3d/physics_engine="Jolt Physics"`. There have been reported `Area3D` detection behaviour differences under Jolt in some 4.x versions. Since the entire combat system rests on `Area3D` overlap (Step 5) and `Area3D` sound radii (Step 7), **QA should include an explicit `Area3D` overlap regression test** as a standing item, not a one-time check.

**Object pooling targets (per the charter's Step 14 mandate):** enemies (they respawn on shrine rest, so pool per region), spark emitters, footstep `AudioStreamPlayer3D`s, blood/impact decals, notice-toast `Control`s, and interaction prompt billboards.

**Automated perf gate (recommended, fits the existing headless QA pattern from Steps 1–2):** a `res://scenes/bench/perf_bench.tscn` run in `--headless`-adjacent batch mode that spawns 8 enemies + full VFX in the densest region chunk, records `TIME_PROCESS` / `TIME_PHYSICS_PROCESS` / draw calls over 600 frames, and fails if **p95 frame time > 16.6 ms**. This turns "locked 60 FPS" into something the QA agent can actually pass/fail, the same way the `GAMESTATE_OK` sentinel made Step 2 verifiable.

---

## Open Questions for Director

These need a human/Director judgment call, not a research answer:

1. **EXP formula interpretation** — is `100 × L^1.5` the cost of the *next* level or the *cumulative* total? The budgets in §14.1 differ by ~40×. Needs a ruling before any balancing work.
2. **Act-gated level cap** (§14.1) — this is my strongest recommendation in Step 14 and it adds a system the charter doesn't mention. Approve or reject before Step 14 planning.
3. **What does a level actually grant?** The charter locks EXP→Level but never Level→power. ER-style player-allocated stat points (recommended) vs. automatic stat growth is a genuine design fork.
4. **EventBus signal additions** — Steps 10–12 need `player_exp_changed`, `player_level_up`, `item_acquired`, `interaction_available`/`unavailable`, `shrine_rested`, `game_saved`, `target_locked`/`released`. Charter §2.1 is a locked spec; this is an amendment request. Also: `quest_state_updated(quest_id: String, ...)` vs `StringName` everywhere else.
5. **Quest FSM needs a `FAILED` state?** The charter names exactly four (UNSTARTED/ACTIVE/OBJECTIVE_COMPLETE/COMPLETED). Missable NPC threads and the Soren branch want a fifth.
6. **Save serialization vs. the code-execution risk.** Charter §2 locks `ResourceSaver`/`ResourceLoader` on `PlayerData.tres`; that's an ACE vector on user-editable files (verified). Recommend `to_dict()`/`from_dict()` + `FileAccess.store_var(dict, false)` while keeping `PlayerData` as the schema — but it contradicts a charter line.
7. **Death penalty.** ER's rune-drop tension isn't available to us (EXP and Mon are separate resources here). Recommend losing 30% of unbanked EXP, recoverable once at the death spot. Needs a call — it changes the feel of the whole loop.
8. **Root motion ownership** during attacks (§13.1) — overriding Step 4's `V_horizontal` lerp during `CombatSM` attack states is my reading of the locked spec, not something the spec states.
9. **Campaign length target.** Everything in Steps 9, 12, and 14 (shrine count, NPC thread count, EXP budget) scales off it. §9.1's table assumes ~20 hours first playthrough; confirm or move it.
10. **New Game+?** It changes the save schema (a `ng_cycle: int` field, enemy scaling multipliers) and it's much cheaper to add to `PlayerData` now than after saves ship.
11. **Posture regen formula and crit definition** (§14.2 items 2 and 4) — both are new formulas the charter doesn't cover, and both materially change combat feel.

---

## Director Decisions (Resolved 2026-07-29)

All 11 open questions above are ruled on below. These rulings are binding; CLAUDE.md's condensed Steps 9–14 spec reflects them.

1. **EXP formula = per-level cost, not cumulative.** `EXP Required = 100 × (Level)^1.5` reads as "the cost to advance from this level," consistent with the singular "Required" phrasing. Cumulative totals are always derivable by summing (`Σ 100·L^1.5` for L=1..N) — implementers should expose both a `cost_for_level(n)` and a `cumulative_to_level(n)` helper so nothing has to re-derive the sum inline.
2. **Act-gated level cap: approved as specified** in §14.1's table (Prologue 8 / Act I 20 / Act II 35 / Act III 50 / Act IV 68 / Act V 85). It's the cleanest fix that doesn't touch the locked formula and reuses an existing reward beat (stance unlock = cap raise, same moment).
3. **Leveling grants player-allocated stat points (Elden Ring model), not automatic growth.** Four stats — `body` (HP), `breath` (stamina + posture), `blade` (damage), `spirit` (posture damage) — confirmed per §14.2 item 1. One point per level, spent at a shrine (ties into §9.2's shrine verb set) or freely, Director's call at Step 14 implementation time (default: freely, since Elden Ring allows free allocation at any Grace and gate-free allocation is simpler to implement).
4. **EventBus signal additions: approved**, to be added incrementally when the step that needs them is actually implemented (Step 10 adds `item_acquired`/`interaction_available`/`interaction_unavailable`; Step 11 adds `player_exp_changed`/`player_level_up`/`game_saved`; Step 12 adds `shrine_rested`; combat-adjacent `target_locked`/`target_released` added whichever of Steps 4/11 implements lock-on first) — never all at once, per the charter's own "don't scope-creep ahead of the current step" rule. **`quest_state_updated(quest_id: String, state: int)` keeps `String`, unchanged** — it's already shipped in Step 2's `event_bus.gd`; GDScript coerces `StringName`↔`String` at call sites with no functional cost, and there's no shipped code relying on it being `StringName`, so this is not worth a breaking edit to already-QA'd Step 2 code. Future `StringName`-typed params (quest_id elsewhere, item_id, etc.) should simply coerce at the call site.
5. **Quest FSM gets a fifth state: `FAILED = 4`.** Approved — missable NPC threads and the Soren "lower your blade" branch (§12.1) need a way to be legibly, permanently closed rather than stuck in `ACTIVE` or misreported as `COMPLETED`. This is a charter amendment to the enum named in the roadmap; apply it when Step 12 is implemented, not retroactively (nothing depends on the quest enum yet).
6. **Save serialization: approved mitigation (a).** `PlayerData` stays the in-memory `Resource` schema (keeps editor tooling/type-safety), but persistence goes through `to_dict()`/`from_dict()` + `FileAccess.store_var(dict, false)` (`full_objects=false` removes the embedded-script ACE vector entirely) rather than `ResourceSaver.save()`/`ResourceLoader.load()` directly on a user-editable file. This amends CLAUDE.md §2.1's literal "via ResourceSaver/ResourceLoader" line for `PlayerData.tres` specifically — the persistence *pattern* (custom Resource schema) survives, only the serialization *call* changes. Apply at Step 11 implementation.
7. **Death penalty: approved as proposed.** Losing 30% of unbanked EXP (earned since last shrine rest) on death, recoverable exactly once by returning to the death location, preserves Elden-Ring-style stakes without retrofitting rune-style EXP/currency fusion onto the charter's separate EXP/Mon resources. Apply at Step 14 (economy) / Step 9 (death-location marker, reuses the `GraveMarker` node already specified in §9.2's boss-arena respawn convention — same node type, general-purpose not boss-only).
8. **Root motion during attack states: approved as the correct reading.** Step 4's `V_horizontal` lerp formula governs locomotion; `CombatSM` attack states (Step 5/13) override with root-motion-derived velocity instead. This doesn't contradict Step 4's locked spec (which is scoped to movement, not combat animation) — recorded here explicitly so a future implementer doesn't need to re-derive the interpretation.
9. **Campaign length target: confirmed at ~20 hours** for a first playthrough. All region/shrine/NPC-thread/EXP counts in this document (§9.1's region table, §12.1's NPC thread count, §14.1's EXP budget) are calibrated to this and should scale together if it ever changes — treat it as the one dial that drives the others, not an independent one.
10. **New Game+: approved for inclusion.** Add `ng_cycle: int = 0` to `PlayerData` now (Step 11 schema, §11.3) since it is materially cheaper before saves exist in the wild than after. Enemy scaling multipliers and any NG+-exclusive content are deferred design (not scoped by this document) — only the save-schema field is locked now.
11. **Posture regeneration formula and crit definition: both approved as proposed.** `R(hp) = R_base × (0.35 + 0.65 × hp_ratio)` (enemies regenerate posture slower as HP drops — the Sekiro "chip HP enables deathblow" loop) and **crits are not a probability roll** — `is_critical` is true only on deathblow/posture-break executions, never randomized. Both apply at Step 5 (posture) / Step 14 (tuning the regen constant `R_base`).

---

**Sources:** [AnimationNodeBlendTree (Godot 4.7)](https://docs.godotengine.org/en/stable/classes/class_animationnodeblendtree.html) · [Using AnimationTree](https://docs.godotengine.org/en/stable/tutorials/animation/animation_tree.html) · [Performance class](https://docs.godotengine.org/en/stable/classes/class_performance.html) · [Occlusion culling](https://docs.godotengine.org/en/stable/tutorials/3d/occlusion_culling.html) · [AudioStreamInteractive](https://docs.godotengine.org/en/stable/classes/class_audiostreaminteractive.html) · [GDQuest — hitboxes/hurtboxes in Godot 4](https://www.gdquest.com/library/hitbox_hurtbox_godot4/) · [GDQuest — save systems cheatsheet](https://www.gdquest.com/library/cheatsheet_save_systems/) · [godot-proposals #10968 — untrusted resource loading](https://github.com/godotengine/godot-proposals/issues/10968) · [godot-safe-resource-loader](https://github.com/derkork/godot-safe-resource-loader)