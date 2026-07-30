extends Node

# Headless kinematics/dodge test for scripts/player/player.gd.
# Run via: godot --headless --path . res://scripts/tests/kinematics_test.tscn
# Drives physics manually via repeated `await get_tree().physics_frame` — never
# Engine.time_scale, which Research verified corrupts the delta/lerp curve
# under test. Asserts by physics-tick count (60 ticks/sec headless, constant
# delta), per Research's verified tick-boundary mapping. Prints
# KINEMATICS_TEST_PASS / KINEMATICS_TEST_FAIL: <reason> and quits with a
# matching exit code so QA can grep the log.

const PlayerScene := preload("res://scenes/player/player.tscn")

var _player_root
var _player


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	GameState.set_state(GameState.State.PLAYING)
	_build_floor()
	_spawn_player()
	await _run_test()


func _build_floor() -> void:
	var body := StaticBody3D.new()
	body.name = "Floor"

	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = Vector3(20, 1, 20)
	shape.shape = box
	body.add_child(shape)

	add_child(body)
	body.position = Vector3(0, -0.5, 0)


func _spawn_player() -> void:
	_player_root = PlayerScene.instantiate()
	add_child(_player_root)
	_player_root.position = Vector3(0, 3.0, 0)
	_player = _player_root.get_node("Body")


func _run_test() -> void:
	# 1. Gravity/landing: dropped from y=3.0, the player should settle onto
	# the floor (is_on_floor() true) within a generous tick budget.
	var landed := false
	for i in 90:
		await get_tree().physics_frame
		if _player.is_on_floor():
			landed = true
			break

	if not landed:
		_fail("player did not land on floor within 90 physics ticks")
		return

	# 2. Camera-relative flatten formula: pitch CamPivot down -35deg and
	# confirm forward input produces a flattened D_input (y ~= 0, full
	# horizontal magnitude) rather than the tilted/shortened vector the
	# literal (unflattened) charter formula would produce.
	var cam_pivot = _player_root.get_node("CamPivot")
	cam_pivot.rotation_degrees = Vector3(-35, 0, 0)
	await get_tree().physics_frame

	var d_input: Vector3 = _player.compute_camera_relative_input(Vector2(0, -1))
	if not is_equal_approx(d_input.y, 0.0):
		_fail("D_input.y expected ~0 with a pitched camera, got %f" % d_input.y)
		return
	if absf(d_input.length() - 1.0) > 0.01:
		_fail("D_input expected full horizontal magnitude ~1.0, got %f" % d_input.length())
		return

	# 3. Horizontal lerp curve: hold forward input, assert ~94% of target
	# speed is reached by tick 10 (Research's verified alpha_accel=15 curve).
	_player.velocity = Vector3.ZERO
	Input.action_press(&"move_forward", 1.0)

	var target_speed: float = _player.S_SPEED
	var reached_ratio := 0.0
	for i in 10:
		await get_tree().physics_frame
		var speed: float = Vector3(_player.velocity.x, 0.0, _player.velocity.z).length()
		reached_ratio = speed / target_speed

	Input.action_release(&"move_forward")

	if reached_ratio < 0.9:
		_fail("expected ~94%% of target speed by tick 10, got %.1f%%" % (reached_ratio * 100.0))
		return

	# 4. Dodge: trigger directly via start_dodge() (cleaner than racing the
	# input buffer per the sign-off), then spot-check invulnerability inside
	# vs. outside the tick 9-21 i-frame window and the stamina cost.
	_player.velocity = Vector3.ZERO
	var stamina_before: float = _player.stamina
	_player.start_dodge(Vector3(0, 0, -1))

	if not is_equal_approx(_player.stamina, stamina_before - 20.0):
		_fail("expected stamina to drop by 20.0 on dodge start, got %f -> %f" % [stamina_before, _player.stamina])
		return

	var tick := 0
	var checked_inside := false
	var checked_outside := false
	while tick < 30:
		await get_tree().physics_frame
		tick += 1

		if tick == 5:
			if _player.is_player_invulnerable():
				_fail("expected is_invulnerable == false at tick 5 (before the i-frame window)")
				return
			checked_outside = true

		if tick == 15:
			if not _player.is_player_invulnerable():
				_fail("expected is_invulnerable == true at tick 15 (inside the i-frame window)")
				return
			checked_inside = true

	if _player.is_player_invulnerable():
		_fail("expected is_invulnerable == false once the dodge has ended")
		return

	if not checked_inside or not checked_outside:
		_fail("dodge i-frame spot-checks did not run as expected")
		return

	# 5. Stamina regen resumes after the 1.2s (72-tick) regen-pause window.
	var stamina_after_dodge: float = _player.stamina
	for i in 80:
		await get_tree().physics_frame

	if _player.stamina <= stamina_after_dodge:
		_fail("expected stamina to have begun regenerating after the 1.2s regen pause")
		return

	print("KINEMATICS_TEST_PASS")
	get_tree().quit(0)


func _fail(reason: String) -> void:
	print("KINEMATICS_TEST_FAIL: ", reason)
	get_tree().quit(1)
