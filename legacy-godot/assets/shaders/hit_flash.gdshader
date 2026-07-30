shader_type spatial;

// Step 6: Hit-flash material (CLAUDE.md 6.3). Applied as a ShaderMaterial
// material_override on the placeholder MeshInstance3D of both player.gd and
// combatant.gd. Explicitly set to 0.0 in each owner's _ready() -- Research
// verified get_shader_parameter() returns null (not this declared default)
// until a value is set at least once, so callers must never rely on the
// hint_range default alone.

uniform float flash_intensity : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	ALBEDO = mix(ALBEDO, vec3(1.0, 1.0, 1.0), flash_intensity);
}
