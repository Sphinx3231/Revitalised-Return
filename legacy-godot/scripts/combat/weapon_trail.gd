class_name WeaponTrail
extends MeshInstance3D

# Step 6: weapon arc trail recording stub (CLAUDE.md 6.3). Real, working
# recording mechanism -- the actual tapered/textured ribbon look is
# explicitly deferred to Step 13's production art pass per the sign-off, so
# the triangle-strip built below is placeholder-quality by design, not a
# finished visual.
#
# Wired from combatant.gd's EXISTING enable_hitbox()/disable_hitbox() (Step
# 5's animation-driven hitbox window) -- start_trail()/stop_trail() piggyback
# on that same authored window rather than introducing a second timing
# mechanism.

const TRAIL_WIDTH: float = 0.08
const MAX_POINTS: int = 64

var is_active: bool = false
# Set by the owner before use (e.g. combatant.gd points this at its hitbox
# Area3D) so the trail records that node's world position each active tick.
var track_node: Node3D

var _history: PackedVector3Array = PackedVector3Array()
var _immediate_mesh: ImmediateMesh


func _ready() -> void:
	_immediate_mesh = ImmediateMesh.new()
	mesh = _immediate_mesh

	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(1.0, 1.0, 1.0, 0.6)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.cull_mode = BaseMaterial3D.CULL_DISABLED
	material_override = mat


func start_trail() -> void:
	is_active = true
	_history.clear()


func stop_trail() -> void:
	is_active = false
	_history.clear()
	_rebuild_mesh()


func _physics_process(_delta: float) -> void:
	if not is_active:
		return

	var point: Vector3 = track_node.global_position if track_node else global_position
	_history.append(point)
	if _history.size() > MAX_POINTS:
		_history.remove_at(0)

	_rebuild_mesh()


func _rebuild_mesh() -> void:
	_immediate_mesh.clear_surfaces()
	if _history.size() < 2:
		return

	_immediate_mesh.surface_begin(Mesh.PRIMITIVE_TRIANGLE_STRIP)
	for i in _history.size():
		var p: Vector3 = to_local(_history[i])
		_immediate_mesh.surface_add_vertex(p + Vector3(TRAIL_WIDTH, 0.0, 0.0))
		_immediate_mesh.surface_add_vertex(p - Vector3(TRAIL_WIDTH, 0.0, 0.0))
	_immediate_mesh.surface_end()
