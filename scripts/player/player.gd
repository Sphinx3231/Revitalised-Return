extends CharacterBody3D

# Step 4: 3D Kinematics, Movement Physics & Dodge Roll.
# Camera-relative movement + lerped accel/friction + gravity + mesh lean +
# dodge roll with i-frames, per CLAUDE.md 4.1-4.3.
#
# NOTE (Director sign-off, docs/Tasks/2026-07-30-step-4-kinematics-dodge.md):
# the camera-relative formula is implemented FLATTENED, not per CLAUDE.md
# 4.1's literal `(Transform_cam.basis * V_input).normalized()` text — Research
# verified the literal formula injects a vertical component under any camera
# pitch, silently varying walk speed with camera angle. See
# compute_camera_relative_input() below.

const S_SPEED: float = 6.0  # m/s walk speed; charter names no number, judgment call.
const ALPHA_ACCEL: float = 15.0
const ALPHA_FRICT: float = 20.0
const GRAVITY: float = 24.5

const LEAN_SCALE: float = 0.1
const LEAN_MAX_RAD: float = 0.0872664626  # deg_to_rad(5.0), precomputed for a const expression.

const DODGE_SPEED_START_MULT: float = 1.8
const DODGE_SPEED_END_MULT: float = 1.0
const DODGE_TOTAL_TICKS: int = 30       # 0.5s @ 60 physics ticks/sec.
const DODGE_IFRAME_START_TICK: int = 9  # 0.15s
const DODGE_IFRAME_END_TICK: int = 21   # 0.35s
const DODGE_STAMINA_COST: float = 20.0
const REGEN_PAUSE_DURATION: float = 1.2

const REGEN_RATE: float = 10.0  # stamina/sec passive regen; charter names no rate, judgment call.

# TODO(Step 5/14): move to PlayerData/stat system.
@export var stamina_max: float = 100.0
# TODO(Step 5/14): move to PlayerData/stat system.
var stamina: float = 100.0
# TODO(Step 5/14): move to PlayerData/stat system.
var regen_pause_timer: float = 0.0

var is_invulnerable: bool = false
var is_dodging: bool = false

var _dodge_tick: int = 0
var _dodge_direction: Vector3 = Vector3.ZERO

# Facing fallback for a dodge triggered with no held input; forward is -Z.
var _facing_dir: Vector3 = Vector3(0.0, 0.0, -1.0)
var _prev_yaw_angle: float = 0.0

@onready var mesh: MeshInstance3D = $MeshPlaceholder
@onready var _cam_pivot: Node3D = get_parent().get_node_or_null(^"CamPivot")


func _physics_process(delta: float) -> void:
	var input_locked: bool = GameState.is_player_input_locked()

	var raw_input: Vector2 = Vector2.ZERO
	if not input_locked:
		raw_input = Input.get_vector(&"move_left", &"move_right", &"move_forward", &"move_back")

	var d_input: Vector3 = compute_camera_relative_input(raw_input)

	if d_input.length() > 0.0001:
		_facing_dir = d_input

	if not is_on_floor():
		velocity.y -= GRAVITY * delta

	if not input_locked and not is_dodging and InputBuffer.consume_action(&"dodge") and stamina >= DODGE_STAMINA_COST:
		start_dodge(d_input)

	if is_dodging:
		_process_dodge()
	else:
		var v_target: Vector3 = d_input * S_SPEED
		var alpha: float = ALPHA_ACCEL if d_input.length() > 0.0001 else ALPHA_FRICT
		var horizontal: Vector3 = Vector3(velocity.x, 0.0, velocity.z)
		horizontal = horizontal.lerp(v_target, clampf(alpha * delta, 0.0, 1.0))
		velocity.x = horizontal.x
		velocity.z = horizontal.z

	_process_stamina_regen(delta)
	_update_mesh_lean(delta)
	_follow_camera_pivot()

	move_and_slide()


func compute_camera_relative_input(input_dir: Vector2) -> Vector3:
	# Camera-relative direction, FLATTENED per the Step 4 sign-off: the literal
	# charter formula `(cam.basis * V_input).normalized()` keeps whatever
	# vertical component the camera's pitch injects, so a downward-looking
	# camera both tilts and shortens the horizontal result. Flattening x/z
	# before normalizing fixes both. Exposed as its own method (rather than
	# inlined in _physics_process) so kinematics_test.gd can exercise the
	# formula directly and deterministically, without faking hardware input.
	if input_dir == Vector2.ZERO:
		return Vector3.ZERO

	var cam: Camera3D = get_viewport().get_camera_3d()
	if cam == null:
		return Vector3.ZERO

	var v_input: Vector3 = Vector3(input_dir.x, 0.0, input_dir.y)
	var raw: Vector3 = cam.global_transform.basis * v_input
	var flat: Vector3 = Vector3(raw.x, 0.0, raw.z)

	if flat.length() < 0.0001:
		return Vector3.ZERO

	return flat.normalized()


func start_dodge(d_input: Vector3) -> void:
	# Guards internally (not just at the _physics_process call site) so any
	# future caller can't drive stamina negative -- QA flagged this as a latent
	# API-safety gap during Step 4's review.
	if is_dodging or stamina < DODGE_STAMINA_COST:
		return

	var direction: Vector3 = d_input if d_input.length() > 0.0001 else _facing_dir
	_dodge_direction = direction.normalized()
	_dodge_tick = 0
	is_dodging = true
	is_invulnerable = false
	stamina -= DODGE_STAMINA_COST
	regen_pause_timer = REGEN_PAUSE_DURATION


func _process_dodge() -> void:
	_dodge_tick += 1

	is_invulnerable = _dodge_tick >= DODGE_IFRAME_START_TICK and _dodge_tick <= DODGE_IFRAME_END_TICK

	var progress: float = clampf(float(_dodge_tick) / float(DODGE_TOTAL_TICKS), 0.0, 1.0)
	var speed_mult: float = lerpf(DODGE_SPEED_START_MULT, DODGE_SPEED_END_MULT, progress)

	velocity.x = _dodge_direction.x * S_SPEED * speed_mult
	velocity.z = _dodge_direction.z * S_SPEED * speed_mult

	if _dodge_tick >= DODGE_TOTAL_TICKS:
		is_dodging = false
		is_invulnerable = false


func _process_stamina_regen(delta: float) -> void:
	if regen_pause_timer > 0.0:
		regen_pause_timer -= delta
	else:
		stamina = minf(stamina_max, stamina + REGEN_RATE * delta)


func _update_mesh_lean(delta: float) -> void:
	# omega_yaw derived from the frame-to-frame change in the horizontal
	# velocity vector's facing angle over delta (no rigged/animated character
	# exists yet per the sign-off, so there's no independent facing-rotation
	# signal to read — velocity's direction is the best available proxy).
	if mesh == null or delta <= 0.0:
		return

	var horizontal: Vector3 = Vector3(velocity.x, 0.0, velocity.z)
	var yaw_angle: float = _prev_yaw_angle
	if horizontal.length() > 0.001:
		yaw_angle = atan2(horizontal.x, horizontal.z)

	var omega_yaw: float = wrapf(yaw_angle - _prev_yaw_angle, -PI, PI) / delta
	_prev_yaw_angle = yaw_angle

	var lean: float = clampf(omega_yaw * LEAN_SCALE, -LEAN_MAX_RAD, LEAN_MAX_RAD)
	mesh.rotation.z = -lean


func _follow_camera_pivot() -> void:
	# CamPivot is a sibling of this body (not a child), per the sign-off's
	# explicit reasoning: mesh lean rotates only the placeholder MeshInstance3D
	# above, never this CharacterBody3D or anything parented under it, so the
	# camera can never inherit that rotation regardless. Following position is
	# scripted here rather than via parenting.
	if _cam_pivot:
		_cam_pivot.global_position = global_position


func is_player_invulnerable() -> bool:
	return is_invulnerable
