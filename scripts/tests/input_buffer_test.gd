extends Node

# Headless sentinel test for InputBuffer (autoload/input_buffer.gd).
# Run via: godot --headless --path . res://scripts/tests/input_buffer_test.tscn
# Prints INPUT_BUFFER_TEST_PASS on success, INPUT_BUFFER_TEST_FAIL: <reason> on
# failure, and quits with a matching exit code so QA can grep the log.


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_run_test()


func _run_test() -> void:
	# InputBuffer.buffer_action() drops input while GameState considers the
	# player locked (default INITIALIZING state); force PLAYING so the test
	# actually exercises ingestion rather than the lock-out path.
	GameState.set_state(GameState.State.PLAYING)
	InputBuffer.clear()

	# 1. Simulate a joypad-button press for "dodge" (JOY_BUTTON_A = 0) and let
	# it flush through the input pipeline into the buffer.
	var press := InputEventJoypadButton.new()
	press.button_index = JOY_BUTTON_A
	press.pressed = true
	Input.parse_input_event(press)

	await get_tree().process_frame
	await get_tree().process_frame

	# 2. Immediately consuming should succeed (within the 0.15s window).
	if not InputBuffer.consume_action(&"dodge"):
		_fail("expected consume_action('dodge') to return true immediately after buffering")
		return

	# 3. Consuming again should fail — the entry was already removed.
	if InputBuffer.consume_action(&"dodge"):
		_fail("expected second consume_action('dodge') to return false (entry already consumed)")
		return

	# 4. Buffer another press, then wait past the 0.15s expiry window and
	# confirm consume_action discards it and returns false.
	var press2 := InputEventJoypadButton.new()
	press2.button_index = JOY_BUTTON_A
	press2.pressed = true
	Input.parse_input_event(press2)

	await get_tree().process_frame
	await get_tree().process_frame

	await get_tree().create_timer(0.2).timeout

	if InputBuffer.consume_action(&"dodge"):
		_fail("expected consume_action('dodge') to return false after the 0.15s expiry window")
		return

	print("INPUT_BUFFER_TEST_PASS")
	get_tree().quit(0)


func _fail(reason: String) -> void:
	print("INPUT_BUFFER_TEST_FAIL: ", reason)
	get_tree().quit(1)
