class_name CameraTrauma
extends Node3D

# Step 6: Camera3D Trauma Shake System (CLAUDE.md 6.2). Attached directly to
# player.tscn's CamPivot node (previously a plain, unscripted Node3D).
#
# NOTE: decay + shake math runs in _physics_process, not _process --
# CLAUDE.md 6.2's decay formula is delta-based (Decay * delta_t), same
# convention as every other physics-tick system in this project (Step 4's
# kinematics, Step 5's parry ticks). Running it on the physics tick also
# means a HitStop freeze (Engine.time_scale=0.0 -> delta==0.0) automatically
# freezes trauma decay and the shake offset along with everything else,
# with no separate HitStop.is_frozen() guard needed -- the delta-based terms
# already go inert on their own during a freeze.
#
# Amplitude tuning judgment call: Research measured FastNoiseLite's actual
# output envelope at frequency=2.0 as +-0.70, not the nominal +-1.0.
# MaxPitch/MaxYaw/MaxRoll below are picked so trauma=1.0 (worst case) still
# reads as a small, subtle few-degree shake once multiplied through that
# ~0.70 envelope, not the full nominal range.
const DECAY_RATE: float = 1.5
const MAX_PITCH_DEG: float = 4.0
const MAX_YAW_DEG: float = 4.0
const MAX_ROLL_DEG: float = 2.5
const NOISE_FREQUENCY: float = 2.0

var trauma: float = 0.0

var _authored_pitch: Vector3
var _t: float = 0.0

# Three separate FastNoiseLite instances with distinct seeds (not small
# offset variations of one shared instance) per the sign-off's ruling on
# Research's decorrelation gotcha -- small seed *offsets* of a single
# instance measured Pearson r=-0.76 between axes (shake collapsing onto one
# axis); distinct seeds + frequency~=2.0 measured ~0.02 correlation.
var _noise_pitch: FastNoiseLite
var _noise_yaw: FastNoiseLite
var _noise_roll: FastNoiseLite


func _ready() -> void:
	# Captured once, per the sign-off -- rotation_degrees must never be read
	# back each frame as the shake's base, or the offset would compound.
	_authored_pitch = rotation_degrees

	_noise_pitch = FastNoiseLite.new()
	_noise_pitch.seed = 1
	_noise_pitch.frequency = NOISE_FREQUENCY

	_noise_yaw = FastNoiseLite.new()
	_noise_yaw.seed = 2
	_noise_yaw.frequency = NOISE_FREQUENCY

	_noise_roll = FastNoiseLite.new()
	_noise_roll.seed = 3
	_noise_roll.frequency = NOISE_FREQUENCY


func add_trauma(amount: float) -> void:
	trauma = clampf(trauma + amount, 0.0, 1.0)


func _physics_process(delta: float) -> void:
	_t += delta
	trauma = clampf(trauma - DECAY_RATE * delta, 0.0, 1.0)

	var t2: float = trauma * trauma
	var pitch: float = MAX_PITCH_DEG * t2 * _noise_pitch.get_noise_2d(_t, 0.0)
	var yaw: float = MAX_YAW_DEG * t2 * _noise_yaw.get_noise_2d(_t, 0.0)
	var roll: float = MAX_ROLL_DEG * t2 * _noise_roll.get_noise_2d(_t, 0.0)

	rotation_degrees = _authored_pitch + Vector3(pitch, yaw, roll)
