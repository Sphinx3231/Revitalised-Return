extends Node

# Headless combat resolution test for scripts/combat/combatant.gd.
# Run via: godot --headless --path . res://scripts/tests/combat_test.tscn
# Instances two bare combatant.tscn actors (attacker/defender) and drives
# physics manually via repeated `await get_tree().physics_frame`, matching
# kinematics_test.gd's established pattern. Prints COMBAT_TEST_PASS /
# COMBAT_TEST_FAIL: <reason> and quits with a matching exit code.

const CombatantScene := preload("res://scenes/combat/combatant.tscn")

const PLAYER_HURTBOX_LAYER: int = 1 << 5  # layer 6, "player_hurtbox" (bit index = layer-1).
const ENEMY_HURTBOX_LAYER: int = 1 << 6   # layer 7, "enemy_hurtbox".

var _attacker
var _defender

var _last_entity_damaged: Array = []
var _last_parry_executed: Array = []
var _neutral_stance: StanceData


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	GameState.set_state(GameState.State.PLAYING)
	EventBus.entity_damaged.connect(_on_entity_damaged)
	EventBus.parry_executed.connect(_on_parry_executed)
	_spawn_actors()
	await _run_test()


func _spawn_actors() -> void:
	_attacker = CombatantScene.instantiate()
	_defender = CombatantScene.instantiate()

	# Deterministic, stance-tuning-independent multipliers so the test's
	# expected numbers don't drift if Stone/Water/Flame/Wind get rebalanced.
	_neutral_stance = StanceData.new()
	_neutral_stance.stance_name = "test_neutral"
	_neutral_stance.base_damage_multiplier = 1.0
	_neutral_stance.posture_damage_multiplier = 1.0
	_neutral_stance.attack_speed_scalar = 1.0
	_neutral_stance.parry_window_duration = 0.1

	_attacker.active_stance = _neutral_stance
	_defender.active_stance = _neutral_stance
	_attacker.armor = 0.0
	_defender.armor = 0.0

	_attacker.hurtbox_layer = PLAYER_HURTBOX_LAYER
	_attacker.hitbox_mask = ENEMY_HURTBOX_LAYER
	_defender.hurtbox_layer = ENEMY_HURTBOX_LAYER
	_defender.hitbox_mask = PLAYER_HURTBOX_LAYER

	add_child(_attacker)
	add_child(_defender)

	# Defender sits directly in front of the attacker's weapon hitbox offset
	# (Weapon/Hitbox is authored at local (0,0,-1) in combatant.tscn) so the
	# two actors' hurtbox/hitbox shapes overlap.
	_attacker.global_position = Vector3.ZERO
	_defender.global_position = Vector3(0, 0, -1)


func _on_entity_damaged(target_node: Node, amount: float, is_critical: bool) -> void:
	_last_entity_damaged = [target_node, amount, is_critical]


func _on_parry_executed(attacker: Node, defender: Node) -> void:
	_last_parry_executed = [attacker, defender]


func _run_test() -> void:
	# 1. Normal hit: base_damage=20, posture_damage=20 (hardcoded placeholders
	# in Combatant._resolve_incoming_hit), neutral 1.0x stance, 0 armor ->
	# mitigation=0 -> full 20 damage to health and posture.
	_last_entity_damaged = []
	await _trigger_hit_direct()

	if not is_equal_approx(_defender.health, 80.0):
		_fail("test 1 (normal hit): expected defender.health == 80.0, got %f" % _defender.health)
		return
	if not is_equal_approx(_defender.posture, 80.0):
		_fail("test 1 (normal hit): expected defender.posture == 80.0, got %f" % _defender.posture)
		return
	if _last_entity_damaged.is_empty() or _last_entity_damaged[0] != _defender:
		_fail("test 1 (normal hit): expected entity_damaged emitted for defender")
		return
	if not is_equal_approx(_last_entity_damaged[1], 20.0):
		_fail("test 1 (normal hit): expected entity_damaged amount == 20.0, got %f" % _last_entity_damaged[1])
		return

	# 2. Parry: defender.is_parrying=true at hit time -> attacker loses 40% of
	# its OWN max_posture, parry_executed(attacker, defender) emitted, and the
	# defender's own health/posture are untouched by this exchange.
	var defender_health_before: float = _defender.health
	var defender_posture_before: float = _defender.posture
	_last_parry_executed = []
	_defender.is_parrying = true
	_defender._parry_tick_counter = 30  # Held well past this test's short wait window.

	await _trigger_hit_direct()

	if not is_equal_approx(_attacker.posture, 60.0):
		_fail("test 2 (parry): expected attacker.posture == 60.0 (100 - 40%%), got %f" % _attacker.posture)
		return
	if _last_parry_executed.is_empty() or _last_parry_executed[0] != _attacker or _last_parry_executed[1] != _defender:
		_fail("test 2 (parry): expected parry_executed(attacker, defender) to be emitted")
		return
	if not is_equal_approx(_defender.health, defender_health_before):
		_fail("test 2 (parry): expected defender.health unchanged, got %f -> %f" % [defender_health_before, _defender.health])
		return
	if not is_equal_approx(_defender.posture, defender_posture_before):
		_fail("test 2 (parry): expected defender.posture unchanged, got %f -> %f" % [defender_posture_before, _defender.posture])
		return

	_defender.is_parrying = false
	_defender._parry_tick_counter = 0

	# 3. Block: defender.is_blocking=true -> 80%% health mitigation (4 damage
	# instead of 20) but FULL (unmitigated) posture damage (20), per CLAUDE.md
	# 5.2's literal wording.
	var health_before_block: float = _defender.health
	var posture_before_block: float = _defender.posture
	_defender.is_blocking = true

	await _trigger_hit_direct()

	if not is_equal_approx(_defender.health, health_before_block - 4.0):
		_fail("test 3 (block): expected health to drop by 4.0, got %f -> %f" % [health_before_block, _defender.health])
		return
	if not is_equal_approx(_defender.posture, posture_before_block - 20.0):
		_fail("test 3 (block): expected posture to drop by 20.0 (full, unmitigated by block), got %f -> %f" % [posture_before_block, _defender.posture])
		return

	_defender.is_blocking = false

	# 4. Animation-driven hit: exercise the real AnimationPlayer method-call
	# track (enable_hitbox/disable_hitbox), not the direct-call helper used
	# above, confirming it actually registers a hit within the >=3-physics-
	# tick (0.05s) minimum active window the sign-off requires of any
	# authored animation. combatant.tscn's "attack" animation enables at t=0
	# and disables at t=0.1 (6 ticks @ 60Hz) -- comfortably over the floor.
	var health_before_anim: float = _defender.health
	var posture_before_anim: float = _defender.posture
	var attacker_anim: AnimationPlayer = _attacker.get_node("AnimationPlayer")
	attacker_anim.play(&"attack")

	var anim_hit_registered := false
	for i in 12:
		await get_tree().physics_frame
		if not is_equal_approx(_defender.health, health_before_anim):
			anim_hit_registered = true

	if not anim_hit_registered:
		_fail("test 4 (animation-driven hit): expected the authored 'attack' animation to register a hit within 12 ticks")
		return
	if not is_equal_approx(_defender.health, health_before_anim - 20.0):
		_fail("test 4 (animation-driven hit): expected health to drop by 20.0, got %f -> %f" % [health_before_anim, _defender.health])
		return
	if not is_equal_approx(_defender.posture, posture_before_anim - 20.0):
		_fail("test 4 (animation-driven hit): expected posture to drop by 20.0, got %f -> %f" % [posture_before_anim, _defender.posture])
		return

	# 5. Self-hit prevention: a dedicated third actor whose hitbox is
	# deliberately overlapped with its OWN hurtbox, with its hitbox mask
	# widened to also include its own hurtbox layer (defeating the layer-
	# separation guard on purpose) -- the owner_actor identity check in
	# _on_hitbox_area_entered must still prevent it from damaging itself.
	var self_actor = CombatantScene.instantiate()
	self_actor.active_stance = _neutral_stance
	self_actor.hurtbox_layer = PLAYER_HURTBOX_LAYER
	self_actor.hitbox_mask = PLAYER_HURTBOX_LAYER | ENEMY_HURTBOX_LAYER
	add_child(self_actor)
	self_actor.global_position = Vector3(10, 0, 0)

	var weapon: Node3D = self_actor.get_node("Weapon")
	weapon.position = Vector3(0, 0, 1)  # Hitbox's own -1 local offset lands back on the actor's origin.

	var self_health_before: float = self_actor.health
	var self_posture_before: float = self_actor.posture
	self_actor.enable_hitbox()

	for i in 5:
		await get_tree().physics_frame

	self_actor.disable_hitbox()

	if not is_equal_approx(self_actor.health, self_health_before) or not is_equal_approx(self_actor.posture, self_posture_before):
		_fail("test 5 (self-hit prevention): expected no self-damage, health %f -> %f, posture %f -> %f" % [self_health_before, self_actor.health, self_posture_before, self_actor.posture])
		return

	self_actor.queue_free()

	# 6. Double-enable-window dedup: within a SINGLE active window (one
	# enable_hitbox() call, not yet re-cleared by a second), simulate the
	# area_entered signal firing twice for the same target -- the
	# _hit_this_window append-before-resolve guard must block the second.
	var health_before_dedup: float = _defender.health
	_attacker.enable_hitbox()
	_attacker._on_hitbox_area_entered(_defender.hurtbox)
	_attacker._on_hitbox_area_entered(_defender.hurtbox)
	_attacker.disable_hitbox()

	if not is_equal_approx(_defender.health, health_before_dedup - 20.0):
		_fail("test 6 (double-window dedup): expected exactly one hit's worth of damage (20.0), got %f -> %f" % [health_before_dedup, _defender.health])
		return

	print("COMBAT_TEST_PASS")
	get_tree().quit(0)


func _trigger_hit_direct() -> void:
	# Direct enable/disable_hitbox() calls (not the AnimationPlayer track --
	# that's exercised separately in test 4), holding the window open >=3
	# physics ticks per the sign-off's authoring floor. A trailing physics
	# tick is awaited after disable_hitbox() too: back-to-back
	# disable->enable calls with no physics tick processed in between never
	# let the engine observe the monitoring=false state, so a following
	# enable_hitbox() doesn't produce a fresh false->true edge and
	# area_entered never refires for an already-overlapping pair (found
	# empirically while building this test).
	_attacker.enable_hitbox()
	for i in 5:
		await get_tree().physics_frame
	_attacker.disable_hitbox()
	await get_tree().physics_frame


func _fail(reason: String) -> void:
	print("COMBAT_TEST_FAIL: ", reason)
	get_tree().quit(1)
