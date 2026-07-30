class_name SparkPool
extends Node3D

# Step 6: directional spark particles (CLAUDE.md 6.3), minimal pool.
#
# Pool size judgment call: CLAUDE.md 14's full budget is 24 pooled
# GPUParticles3D emitters across the whole scene -- per the sign-off this
# step ships a smaller working pool (POOL_SIZE) reused round-robin per actor,
# with the full 24-emitter global tuning pass explicitly deferred to Step 14.
#
# Contact-point sourcing judgment call: Step 5's Area3D-based area_entered
# resolution (combatant.gd's _on_hitbox_area_entered) has no equivalent to
# KinematicCollision3D's contact point/normal -- Area3D overlap detection
# doesn't expose one. Per the sign-off's documented stand-in, callers pass
# the defender's position/up-vector instead of a true contact normal (see
# combatant.gd/player.gd's trigger() call sites).
#
# No texture: QuadMesh draw pass with a flat unshaded material, verified by
# Research to render fine with zero art assets.

const POOL_SIZE: int = 6
const SPAWN_HEIGHT: float = 1.0
const PARTICLE_LIFETIME: float = 0.35
const PARTICLE_AMOUNT: int = 12
const SPARK_COLOR: Color = Color(1.0, 0.85, 0.3)

var _pool: Array[GPUParticles3D] = []
var _next_index: int = 0


func _ready() -> void:
	for i in POOL_SIZE:
		var p := GPUParticles3D.new()
		p.name = "Spark%d" % i
		p.emitting = false
		p.one_shot = true
		p.amount = PARTICLE_AMOUNT
		p.lifetime = PARTICLE_LIFETIME
		p.explosiveness = 1.0
		p.process_material = _make_process_material()
		p.draw_pass_1 = _make_draw_mesh()
		add_child(p)
		_pool.append(p)


func _make_process_material() -> ParticleProcessMaterial:
	var mat := ParticleProcessMaterial.new()
	mat.direction = Vector3(0, 1, 0)
	mat.spread = 45.0
	mat.initial_velocity_min = 2.0
	mat.initial_velocity_max = 4.5
	mat.gravity = Vector3(0, -9.8, 0)
	mat.scale_min = 0.5
	mat.scale_max = 1.0
	mat.color = SPARK_COLOR
	return mat


func _make_draw_mesh() -> QuadMesh:
	var quad := QuadMesh.new()
	quad.size = Vector2(0.08, 0.08)

	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = SPARK_COLOR
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	quad.material = mat

	return quad


# Research's verified gotcha: `emitting` reads back false one frame after a
# one-shot burst, so tests/callers must assert on trigger/config having run,
# not on the `emitting` flag persisting.
func trigger(at_position: Vector3, normal: Vector3 = Vector3.UP) -> void:
	if _pool.is_empty():
		return

	var p: GPUParticles3D = _pool[_next_index]
	_next_index = (_next_index + 1) % _pool.size()

	p.global_position = at_position
	if normal.length() > 0.0001:
		var up_hint: Vector3 = Vector3.FORWARD if absf(normal.dot(Vector3.UP)) > 0.99 else Vector3.UP
		p.look_at(at_position + normal, up_hint)

	p.restart()
	p.emitting = true
