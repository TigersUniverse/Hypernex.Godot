@tool
class_name AvatarDescriptor
extends Node

@export var Skeleton: Skeleton3D
@export var Eyes: Marker3D

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "AvatarDescriptor")

func _exit_tree() -> void:
	remove_meta(&"typename")
