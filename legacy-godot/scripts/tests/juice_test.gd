extends Node

# Headless test for Step 6's Juice Engine: autoload/hit_stop.gd,
# scripts/player/camera_trauma.gd, scripts/combat/spark_pool.gd (config-only,
# no dedicated assertion beyond compiling/triggering without error -- Research
# verified `emitting` reads back false one frame after a one-shot burst so
# there's nothing meaningful to assert there), the hit-flash shader wiring on
# combatant.gd, and scripts/combat/weapon_trail.gd.
# Run via: godot --headless --path . res://scripts/tests/juice_test.tscn
# Prints JUICE_TEST_PASS / JUICE_TEST_FAIL: <reason> and quits with a
# matching exit code so QA can grep the log.

const CombatantScene := preload("res://scenes/combat/combatant.tscn")

var _neutral_stance: StanceData


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	GameState.set_state(GameState.State.PLAYING)

	_neutral_stance = StanceData.new()
	_neutral_stance.stance_name = "test_neutral"
	_neutral_stance.base_damage_multiplier = 1.0
	_neutral_stance.posture_damage_multiplier = 1.0
	_neutral_stance.attack_speed_scalar = 1.0
	_neutral_stance.parry_window_duration = 0.1

	await _run_test()


func _run_test() -> void:
	await _test_hit_stop_timing()
	await _test_hit_stop_gates_parry_tick()
	await _test_camera_trauma()
	await _test_hit_flash()
	await _test_weapon_trail()

	print("JUICE_TEST_PASS")
	get_tree().quit(0)


# 1. HitStop.trigger() sets time_scale=0 for the correct tick duration and
# restores it. 0.05s -> roundi(0.05*60) == 3 ticks.
func _test_hit_stop_timing() -> void:
	Engine.time_scale = 1.0
	HitStop.trigger(0.05)

	if not is_equal_approx(Engine.time_scale, 0.0):
		_fail("test 1 (hit-stop timing): expected Engine.time_scale == 0.0 immediately after trigger(), got %f" % Engine.time_scale)
		return
	if not HitStop.is_frozen():
		_fail("test 1 (hit-stop timing): expected HitStop.is_frozen() == true immediately after trigger()")
		return

	# Still within the 3-tick window (2 signals in, regardless of exactly
	# whether `physics_frame` fires before or after that frame's
	# _physics_process calls -- 2 < 3 either way).
	await get_tree().physics_frame
	await get_tree().physics_frame
	if not HitStop.is_frozen():
		_fail("test 1 (hit-stop timing): expected HitStop.is_frozen() == true 2 ticks into a 3-tick window")
		return

	# Comfortably past the 3-tick window: countdown must have reached 0 and
	# time_scale restored, regardless of the exact signal/process ordering.
	for i in 3:
		await get_tree().physics_frame
	if HitStop.is_frozen():
		_fail("test 1 (hit-stop timing): expected HitStop.is_frozen() == false well after the 3-tick window")
		return
	if not is_equal_approx(Engine.time_scale, 1.0):
		_fail("test 1 (hit-stop timing): expected Engine.time_scale restored to 1.0, got %f" % Engine.time_scale)
		return


# 2. THE CRITICAL ASSERTION: a Combatant's _parry_tick_counter must NOT
# advance while HitStop.is_frozen() -- this directly proves the Step 4/5
# retrofit (the `if HitStop.is_frozen(): return` guard added as the first
# line of combatant.gd's _physics_process) actually works, per Research's
# confirmed real bug (manual tick counters silently advance during a
# time_scale=0.0 freeze because _physics_process keeps firing every tick).
func _test_hit_stop_gates_parry_tick() -> void:
	Engine.time_scale = 1.0

	var c = CombatantScene.instantiate()
	c.active_stance = _neutral_stance
	add_child(c)

	c.is_parrying = true
	c._parry_tick_counter = 10

	HitStop.trigger(0.05)  # 3-tick freeze.

	# Spot-check DURING the freeze (2 of the 3 ticks -- staying clear of the
	# exact unfreeze-boundary tick, which may or may not have already resumed
	# depending on autoload-vs-scene-node processing order within that tick).
	await get_tree().physics_frame
	await get_tree().physics_frame

	if not HitStop.is_frozen():
		_fail("test 2 (critical: tick-counter freeze gating): expected still frozen 2 ticks into a 3-tick hit-stop")
		return
	if c._parry_tick_counter != 10:
		_fail("test 2 (critical: tick-counter freeze gating): expected _parry_tick_counter to NOT advance during the freeze, still 10, got %d" % c._parry_tick_counter)
		return

	# Let the freeze fully lift and give the counter room to resume ticking.
	for i in 6:
		await get_tree().physics_frame

	if HitStop.is_frozen():
		_fail("test 2 (critical: tick-counter freeze gating): expected HitStop.is_frozen() == false well after the 3-tick window")
		return
	if c._parry_tick_counter >= 10:
		_fail("test 2 (critical: tick-counter freeze gating): expected _parry_tick_counter to have resumed decrementing after the freeze lifted, still %d" % c._parry_tick_counter)
		return

	c.queue_free()


# 3. Camera trauma decays per CLAUDE.md 6.2's formula, and the pitch/yaw/roll
# shake axes are not strongly correlated at the tuned noise settings (a loose
# spot-check across several ticks, per the sign-off -- not a full statistical
# test).
func _test_camera_trauma() -> void:
	var cam := CameraTrauma.new()
	cam.rotation_degrees = Vector3(-35.0, 0.0, 0.0)  # Authored pitch, captured once in _ready().
	add_child(cam)

	cam.add_trauma(1.0)
	if not is_equal_approx(cam.trauma, 1.0):
		_fail("test 3 (camera trauma): expected trauma == 1.0 after add_trauma(1.0), got %f" % cam.trauma)
		return

	var pitches: Array[float] = []
	var yaws: Array[float] = []
	var rolls: Array[float] = []

	var ticks: int = 20
	for i in ticks:
		await get_tree().physics_frame
		pitches.append(cam.rotation_degrees.x - cam._authored_pitch.x)
		yaws.append(cam.rotation_degrees.y - cam._authored_pitch.y)
		rolls.append(cam.rotation_degrees.z - cam._authored_pitch.z)

	# Decay check: after `ticks` physics frames at 60Hz, expected trauma ~=
	# clamp(1.0 - 1.5 * ticks/60.0, 0.0, 1.0), per CLAUDE.md 6.2.
	var expected_trauma: float = clampf(1.0 - CameraTrauma.DECAY_RATE * (float(ticks) / 60.0), 0.0, 1.0)
	if absf(cam.trauma - expected_trauma) > 0.05:
		_fail("test 3 (camera trauma): expected trauma ~= %f after %d ticks, got %f" % [expected_trauma, ticks, cam.trauma])
		return

	# Axis decorrelation spot-check: distinct FastNoiseLite seeds at
	# frequency~=2.0 should keep pitch/yaw/roll from moving in lockstep.
	# Loose Pearson correlation check (not a full statistical test).
	var corr_pitch_yaw: float = _pearson(pitches, yaws)
	var corr_pitch_roll: float = _pearson(pitches, rolls)
	if absf(corr_pitch_yaw) > 0.9:
		_fail("test 3 (camera trauma): pitch/yaw shake axes look strongly correlated (r=%f), expected decorrelated noise seeds" % corr_pitch_yaw)
		return
	if absf(corr_pitch_roll) > 0.9:
		_fail("test 3 (camera trauma): pitch/roll shake axes look strongly correlated (r=%f), expected decorrelated noise seeds" % corr_pitch_roll)
		return

	cam.queue_free()


func _pearson(a: Array[float], b: Array[float]) -> float:
	var n: int = a.size()
	if n == 0:
		return 0.0

	var mean_a: float = 0.0
	var mean_b: float = 0.0
	for i in n:
		mean_a += a[i]
		mean_b += b[i]
	mean_a /= n
	mean_b /= n

	var cov: float = 0.0
	var var_a: float = 0.0
	var var_b: float = 0.0
	for i in n:
		var da: float = a[i] - mean_a
		var db: float = b[i] - mean_b
		cov += da * db
		var_a += da * da
		var_b += db * db

	if var_a <= 0.0000001 or var_b <= 0.0000001:
		return 0.0

	return cov / sqrt(var_a * var_b)


# 4. Hit-flash shader parameter round-trips: set it, read it back, confirm
# non-null, confirm it changes when damage is applied via the real
# EventBus.entity_damaged reactor wiring on combatant.gd.
func _test_hit_flash() -> void:
	var c = CombatantScene.instantiate()
	c.active_stance = _neutral_stance
	add_child(c)
	await get_tree().physics_frame  # Let _ready() run and set the material.

	var initial: Variant = c._flash_material.get_shader_parameter(&"flash_intensity")
	if initial == null:
		_fail("test 4 (hit-flash): expected shader_parameter/flash_intensity to be non-null after _ready(), got null")
		return
	if not is_equal_approx(float(initial), 0.0):
		_fail("test 4 (hit-flash): expected shader_parameter/flash_intensity == 0.0 initially, got %f" % float(initial))
		return

	EventBus.entity_damaged.emit(c, 10.0, false)

	var after_hit: Variant = c._flash_material.get_shader_parameter(&"flash_intensity")
	if after_hit == null or not is_equal_approx(float(after_hit), 1.0):
		_fail("test 4 (hit-flash): expected shader_parameter/flash_intensity == 1.0 immediately after entity_damaged, got %s" % str(after_hit))
		return

	# Tween runs on the idle process callback with set_ignore_time_scale();
	# wait real time (not physics ticks) for it to decay back down.
	await get_tree().create_timer(0.2).timeout

	var decayed: Variant = c._flash_material.get_shader_parameter(&"flash_intensity")
	if decayed == null or float(decayed) > 0.01:
		_fail("test 4 (hit-flash): expected shader_parameter/flash_intensity to have decayed back near 0.0, got %s" % str(decayed))
		return

	c.queue_free()


# 5. WeaponTrail's history grows while start_trail() is active and clears on
# stop_trail().
func _test_weapon_trail() -> void:
	var track := Node3D.new()
	add_child(track)
	track.global_position = Vector3.ZERO

	var trail := WeaponTrail.new()
	trail.track_node = track
	add_child(trail)
	await get_tree().physics_frame  # Let _ready() build the ImmediateMesh.

	if trail.is_active:
		_fail("test 5 (weapon trail): expected is_active == false before start_trail()")
		return

	trail.start_trail()
	if not trail.is_active:
		_fail("test 5 (weapon trail): expected is_active == true after start_trail()")
		return

	for i in 5:
		track.global_position += Vector3(0.1, 0.0, 0.0)
		await get_tree().physics_frame

	if trail._history.size() <= 0:
		_fail("test 5 (weapon trail): expected _history to have grown while start_trail() was active, size == %d" % trail._history.size())
		return

	trail.stop_trail()
	if trail.is_active:
		_fail("test 5 (weapon trail): expected is_active == false after stop_trail()")
		return
	if trail._history.size() != 0:
		_fail("test 5 (weapon trail): expected _history to clear on stop_trail(), size == %d" % trail._history.size())
		return

	# Confirm it stays cleared -- no longer active, no further growth.
	await get_tree().physics_frame
	if trail._history.size() != 0:
		_fail("test 5 (weapon trail): expected _history to remain empty after stop_trail() with no further growth, size == %d" % trail._history.size())
		return

	trail.queue_free()
	track.queue_free()


func _fail(reason: String) -> void:
	print("JUICE_TEST_FAIL: ", reason)
	get_tree().quit(1)
