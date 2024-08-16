@tool
class_name ReverbZone
extends CSGBox3D

@export var effect: AudioEffect

var was_visible := true

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		was_visible = visible
		visible = false
		transparency = 0
		gi_mode = GeometryInstance3D.GI_MODE_DISABLED
		set_meta(&"typename", "ReverbZone")
	elif what == NOTIFICATION_EDITOR_POST_SAVE:
		visible = was_visible
		transparency = 0.25

func _enter_tree() -> void:
	transparency = 0.25
	gi_mode = GeometryInstance3D.GI_MODE_DISABLED

func _exit_tree() -> void:
	remove_meta(&"typename")
