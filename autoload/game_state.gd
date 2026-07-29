extends Node

enum State { INITIALIZING, MAIN_MENU, PLAYING, PAUSED, DIALOGUE, CUTSCENE, GAME_OVER }

var current_state: State = State.INITIALIZING


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	print("GAMESTATE_OK ", State.keys()[current_state])


func set_state(new_state: State) -> void:
	current_state = new_state

	match new_state:
		State.PLAYING:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
			get_tree().paused = false
		State.PAUSED, State.MAIN_MENU:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
			get_tree().paused = true
		State.DIALOGUE, State.CUTSCENE:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
			get_tree().paused = false


func is_player_input_locked() -> bool:
	return current_state != State.PLAYING
