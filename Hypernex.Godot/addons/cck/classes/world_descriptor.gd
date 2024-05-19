@tool
class_name WorldDescriptor
extends Node3D

@export var StartPosition : Vector3

func _notification(what):
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "WorldDescriptor")

func _exit_tree():
	remove_meta(&"typename")
