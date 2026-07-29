extends Node

# Player & Vital Signals
signal player_health_changed(current: float, max_health: float)
signal player_stamina_changed(current: float, max_stamina: float)
signal player_posture_changed(current: float, max_posture: float)
signal stance_swapped(new_stance_resource: StanceData)
signal player_died()

# Combat & Damage Signals
signal entity_damaged(target_node: Node3D, amount: float, is_critical: bool)
signal posture_broken(target_node: Node3D)
signal parry_executed(attacker: Node3D, defender: Node3D)
signal enemy_killed(enemy_node: Node3D, exp_reward: int)

# World & UI Signals
signal quest_state_updated(quest_id: String, state: int)
signal interaction_triggered(interactable_node: Node3D)
signal show_notice(text: String, duration: float)


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
