@tool
class_name VideoPlayer
extends Node

@export var VideoPlayback: NodePath
@export var AudioPlayback: AudioStreamPlayer3D

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "VideoPlayer")

func _exit_tree() -> void:
	remove_meta(&"typename")
