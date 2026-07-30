class_name Combatant
extends Node3D

# Step 5: Stance Engine, Hitbox Registration & Parry Logic.
# Generic combat actor: owns health/posture/armor stats + a Hurtbox Area3D
# (hurtbox_entered via signal on the Area3D itself is NOT used -- per the
# Director sign-off, resolution happens on the ATTACKER's hitbox entering the
# DEFENDER's hurtbox, i.e. _on_hitbox_area_entered is connected to this
# Combatant's own Weapon/Hitbox Area3D's area_entered signal) + parry/block
# state, per CLAUDE.md 5.2 and the Step 5 task brief's Research/sign-off.
#
# NOTE: no Player/Enemy-specific stat migration yet -- these are real working
# numbers for test purposes, matching Step 4's stamina-stub precedent.
# TODO(Step 11/14): migrate health/posture/armor to PlayerData/enemy stat
# resources.
#
# Collision layer convention (project.godot [layer_names], Director sign-off):
#   layer 6 = player_hurtbox, layer 7 = enemy_hurtbox, layer 8 = hitbox.
# Hurtboxes: monitoring=false, monitorable=true, on their own side's layer.
# Hitboxes: monitorable=false, monitoring toggled by enable/disable_hitbox(),
# collision_mask set to the OPPOSING hurtbox layer only. `owner_actor` (this
# script's `self`, set on the hurtbox via the scene) is a second guard on top
# of layer separation -- belt-and-suspenders against self-hits, per Research's
# verified self-hit bug when hitbox/hurtbox share a layer.

@export var health: float = 100.0
@export var max_health: float = 100.0
@export var posture: float = 100.0
@export var max_posture: float = 100.0
@export var armor: float = 0.0
@export var active_stance: StanceData

# Per-instance layer/mask configuration (Combatant is faction-agnostic; a
# player instance sets hurtbox_layer=player_hurtbox/hitbox_mask=enemy_hurtbox,
# an enemy instance the reverse). Applied to the child Area3Ds in _ready()
# rather than hardcoded on the .tscn, so combatant.tscn stays generic and
# reusable per the sign-off (Step 7's enemy reuses this same scene/script).
@export_flags_3d_physics var hurtbox_layer: int = 0
@export_flags_3d_physics var hitbox_mask: int = 0

var is_parrying: bool = false
var is_blocking: bool = false  # Public state only -- no InputMap "block" action exists yet; see task log's charter-gap ruling.

var _parry_tick_counter: int = 0
var _hit_this_window: Array[Node] = []

# Step 6: hit-flash shader (CLAUDE.md 6.3). Shared constant with player.gd --
# duplicated rather than factored into a shared util, matching this project's
# convention of explicit per-file code over cross-cutting abstractions.
const HIT_FLASH_SHADER: Shader = preload("res://assets/shaders/hit_flash.gdshader")
const HIT_FLASH_DURATION: float = 0.08

var _flash_material: ShaderMaterial
var _spark_pool: SparkPool
var _weapon_trail: WeaponTrail

@onready var hurtbox: Area3D = $Hurtbox
@onready var hitbox: Area3D = $Weapon/Hitbox
@onready var mesh: MeshInstance3D = $MeshPlaceholder


func _ready() -> void:
	# Hurtbox needs a way back to the Combatant that owns it; the hurtbox is a
	# direct child, so get_parent() would work, but an explicit owner_actor
	# reference (set here rather than relied upon structurally) keeps the
	# lookup correct even if a future scene nests the hurtbox differently.
	if hurtbox:
		hurtbox.set_meta(&"owner_actor", self)
		if hurtbox_layer != 0:
			hurtbox.collision_layer = hurtbox_layer
	if hitbox:
		if hitbox_mask != 0:
			hitbox.collision_mask = hitbox_mask
		hitbox.area_entered.connect(_on_hitbox_area_entered)

	# Step 6: hit-flash material -- explicitly set to 0.0 once here (Research's
	# null-until-set gotcha: get_shader_parameter() returns null, not the
	# shader's declared default, until a value has been set at least once).
	_flash_material = ShaderMaterial.new()
	_flash_material.shader = HIT_FLASH_SHADER
	_flash_material.set_shader_parameter(&"flash_intensity", 0.0)
	if mesh:
		mesh.material_override = _flash_material

	_spark_pool = SparkPool.new()
	add_child(_spark_pool)

	# Step 6: weapon trail records the hitbox's own world position each
	# active tick (see enable_hitbox()/disable_hitbox() below), per the
	# sign-off's "record the hitbox's world position" ruling.
	_weapon_trail = WeaponTrail.new()
	_weapon_trail.track_node = hitbox
	add_child(_weapon_trail)

	# Step 6: reactor to Step 5's existing combat signals, per the task
	# brief -- this never duplicates _resolve_hit/_resolve_block/_resolve_parry's
	# resolution logic, only reacts to what those already emit.
	EventBus.entity_damaged.connect(_on_entity_damaged)
	EventBus.parry_executed.connect(_on_parry_executed)


func _physics_process(_delta: float) -> void:
	# Step 6 critical fix: HitStop.is_frozen() must be the very first line --
	# Research proved _physics_process keeps firing every tick (delta==0) even
	# at Engine.time_scale=0.0, so without this guard _parry_tick_counter would
	# keep silently draining during a hit-stop freeze. See autoload/hit_stop.gd.
	if HitStop.is_frozen():
		return

	if is_parrying:
		_parry_tick_counter -= 1
		if _parry_tick_counter <= 0:
			is_parrying = false
		return

	if GameState.is_player_input_locked():
		return

	if active_stance and InputBuffer.consume_action(&"parry"):
		is_parrying = true
		_parry_tick_counter = roundi(active_stance.parry_window_duration * 60.0)


func set_blocking(value: bool) -> void:
	is_blocking = value


func enable_hitbox() -> void:
	# Called via an AnimationPlayer method-call track, never a driving script
	# timer, per the sign-off. Minimum active window: >=3 physics ticks
	# (0.05s) -- Research verified a sub-tick window can register zero hits
	# and area_entered fires one physics tick after monitoring=true. This is
	# an authoring constraint on the calling animation, not runtime-enforced.
	_hit_this_window.clear()
	if hitbox:
		hitbox.monitoring = true
	# Step 6: weapon trail piggybacks on this same authored window rather than
	# introducing a second timing mechanism.
	if _weapon_trail:
		_weapon_trail.start_trail()


func disable_hitbox() -> void:
	if hitbox:
		hitbox.monitoring = false
	if _weapon_trail:
		_weapon_trail.stop_trail()


func _on_hitbox_area_entered(area: Area3D) -> void:
	var defender: Combatant = area.get_meta(&"owner_actor", null) as Combatant
	if defender == null:
		return
	if defender == self:
		return  # Belt-and-suspenders self-hit guard; layer separation already prevents this structurally.
	if defender in _hit_this_window:
		return
	_hit_this_window.append(defender)

	defender._resolve_incoming_hit(self)


func _resolve_incoming_hit(attacker: Combatant) -> void:
	var base_damage: float = 20.0  # No weapon system exists yet (Step 10/13); a placeholder pre-combined damage number, per the sign-off.
	var posture_damage: float = 20.0

	# Priority order per CLAUDE.md 5.2 / the sign-off: parry check first, then
	# block check (only if not parried), then default hit.
	if is_parrying:
		_resolve_parry(attacker)
		return

	if is_blocking:
		_resolve_block(attacker, base_damage, posture_damage)
		return

	_resolve_hit(attacker, base_damage, posture_damage)


func _resolve_parry(attacker: Combatant) -> void:
	var attacker_posture_loss: float = attacker.max_posture * 0.4
	attacker.posture -= attacker_posture_loss
	if attacker.posture <= 0.0:
		EventBus.posture_broken.emit(attacker)
	EventBus.parry_executed.emit(attacker, self)


func _resolve_block(attacker: Combatant, base_damage: float, posture_damage: float) -> void:
	# Armor mitigation is the DEFENDER's (self) armor; stance multipliers are
	# the ATTACKER's active stance -- the attacker's stance governs the damage
	# they deal, the defender's armor governs how much lands.
	var mitigation: float = armor / (armor + 100.0)
	var stance_dmg_mult: float = attacker.active_stance.base_damage_multiplier if attacker.active_stance else 1.0
	var stance_posture_mult: float = attacker.active_stance.posture_damage_multiplier if attacker.active_stance else 1.0

	var health_delta: float = base_damage * stance_dmg_mult * (1.0 - mitigation) * 0.2  # 80% block mitigation.
	health -= health_delta

	# Block applies FULL posture damage, bypassing armor mitigation -- per
	# CLAUDE.md 5.2's literal wording ("apply full posture damage to target's
	# posture bar"), the sign-off's judgment call.
	posture -= posture_damage * stance_posture_mult

	EventBus.entity_damaged.emit(self, health_delta, false)
	if posture <= 0.0:
		EventBus.posture_broken.emit(self)


func _resolve_hit(attacker: Combatant, base_damage: float, posture_damage: float) -> void:
	# Armor mitigation is the DEFENDER's (self) armor; stance multipliers are
	# the ATTACKER's active stance -- same reasoning as _resolve_block above.
	var mitigation: float = armor / (armor + 100.0)
	var stance_dmg_mult: float = attacker.active_stance.base_damage_multiplier if attacker.active_stance else 1.0
	var stance_posture_mult: float = attacker.active_stance.posture_damage_multiplier if attacker.active_stance else 1.0

	var health_delta: float = base_damage * stance_dmg_mult * (1.0 - mitigation)
	health -= health_delta

	posture -= posture_damage * stance_posture_mult * (1.0 - mitigation)

	EventBus.entity_damaged.emit(self, health_delta, false)
	if posture <= 0.0:
		EventBus.posture_broken.emit(self)


func _on_entity_damaged(target_node: Node3D, _amount: float, _is_critical: bool) -> void:
	if target_node != self:
		return

	_play_hit_flash()

	if _spark_pool:
		# Contact-point stand-in (sign-off's documented judgment call): Area3D
		# overlap resolution has no contact point/normal equivalent to
		# KinematicCollision3D, so sparks spawn at the DEFENDER's own position
		# (offset to roughly chest height) oriented along its up vector,
		# rather than a true hit-surface contact normal.
		_spark_pool.trigger(global_position + Vector3.UP * SparkPool.SPAWN_HEIGHT)


func _on_parry_executed(attacker: Node3D, defender: Node3D) -> void:
	if attacker != self and defender != self:
		return
	# CLAUDE.md 6.1: hit-stop on "successful heavy hit or parry" -- parry is
	# the unambiguous case (no heavy/light attack distinction exists yet in
	# the current placeholder damage system, see _resolve_incoming_hit's
	# hardcoded base_damage; that split is Step 10/13 territory). 0.05s is
	# within the charter's 0.03s-0.06s hit-stop window.
	HitStop.trigger(0.05)


func _play_hit_flash() -> void:
	# Step 6 ruling: VFX play THROUGH a hit-stop freeze -- set_ignore_time_scale()
	# is load-bearing here; without it a Tween simply doesn't advance while
	# Engine.time_scale==0.0, so the flash would silently never decay during
	# the exact window it's meant to sell the hit.
	if _flash_material == null:
		return

	_flash_material.set_shader_parameter(&"flash_intensity", 1.0)
	var tween: Tween = create_tween()
	tween.set_ignore_time_scale(true)
	tween.tween_property(_flash_material, ^"shader_parameter/flash_intensity", 0.0, HIT_FLASH_DURATION)
