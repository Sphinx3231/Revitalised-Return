extends Node

const BUFFERED_ACTIONS: Array[StringName] = [&"light_attack", &"heavy_attack", &"parry", &"dodge"]
const EXPIRE_SECONDS: float = 0.15

var _buffer: Array[Dictionary] = []


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	print("INPUTBUFFER_OK")


func _unhandled_input(event: InputEvent) -> void:
	for action_name in BUFFERED_ACTIONS:
		if event.is_action_pressed(action_name):
			buffer_action(action_name)


func buffer_action(action: StringName) -> void:
	if GameState.is_player_input_locked():
		return
	_buffer.append({"action": action, "timestamp": Time.get_ticks_msec() / 1000.0})


func consume_action(action: StringName) -> bool:
	var now: float = Time.get_ticks_msec() / 1000.0
	var i: int = _buffer.size() - 1

	while i >= 0:
		var entry: Dictionary = _buffer[i]
		var expired: bool = now - entry.timestamp > EXPIRE_SECONDS

		if entry.action == action:
			_buffer.remove_at(i)
			return not expired
		elif expired:
			_buffer.remove_at(i)

		i -= 1

	return false


func clear() -> void:
	_buffer.clear()
