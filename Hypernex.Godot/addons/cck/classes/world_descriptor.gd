@tool
class_name WorldDescriptor
extends Node3D

@export var StartPosition: Vector3 = Vector3.ZERO

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "WorldDescriptor")

func _exit_tree() -> void:
	remove_meta(&"typename")
