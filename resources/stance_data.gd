class_name StanceData
extends Resource

# Step 5: Stance Engine data resource. Defaults are deliberately invalid/
# neutral (per Research's ResourceSaver-omission gotcha, docs/Tasks/
# 2026-07-30-step-5-stances-hitboxes.md) since the 4 real stances are hand-
# authored .tres files under resources/stances/, not produced via
# ResourceSaver -- every value is explicit in those files regardless.

@export var stance_name: String = ""
@export var base_damage_multiplier: float = 0.0
@export var posture_damage_multiplier: float = 0.0
@export var attack_speed_scalar: float = 0.0
@export var parry_window_duration: float = 0.0
@export var icon: Texture2D
