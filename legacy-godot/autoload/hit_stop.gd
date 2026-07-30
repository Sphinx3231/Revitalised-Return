extends Node

# Step 6: Freeze-Frame Hit-Stop Engine (CLAUDE.md 6.1).
#
# NOTE (Director sign-off, docs/Tasks/2026-07-30-step-6-juice-engine.md):
# implemented as a physics-tick countdown, NOT CLAUDE.md 6.1's literal
# `get_tree().create_timer(duration, true, false, true)` -- Research verified
# the SceneTreeTimer is measurably inaccurate at the charter's own 0.03s-0.06s
# range (0/7/18/21/23ms measured for a nominal 30ms timer), while physics
# ticks provably keep firing at exactly 60Hz even at Engine.time_scale=0.0
# (Godot derives tick COUNT from unscaled wall clock, only scales delta) --
# tick-counting is exact by construction and IS what 6.1's own parenthetical
# ("2-4 frames at 60 FPS") already describes. Logged as a deliberate
# deviation per this project's standing convention for charter deviations
# (Steps 3/4/5's precedent), not silently changed.
#
# is_frozen() is the load-bearing half of this step's critical fix: Research
# proved manual tick counters (player.gd's _dodge_tick, combatant.gd's
# _parry_tick_counter) silently keep advancing during a time_scale=0.0
# freeze, because _physics_process is still called every tick with
# delta==0.0 -- Godot only scales delta, not tick count/rate. Both scripts'
# _physics_process now early-returns via HitStop.is_frozen() as their very
# first line, so a freeze genuinely halts everything driven by those
# functions, not just delta-based motion math.

var _ticks_remaining: int = 0


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	print("HITSTOP_OK")


func trigger(duration_sec: float) -> void:
	# max(), not add: an overlapping trigger() call (e.g. two Combatants both
	# reacting to the same parry_executed signal) extends to the longer
	# window rather than stacking freeze duration.
	_ticks_remaining = maxi(_ticks_remaining, roundi(duration_sec * 60.0))
	Engine.time_scale = 0.0


func is_frozen() -> bool:
	return _ticks_remaining > 0


func _physics_process(_delta: float) -> void:
	if _ticks_remaining <= 0:
		return

	_ticks_remaining -= 1
	if _ticks_remaining <= 0:
		Engine.time_scale = 1.0
