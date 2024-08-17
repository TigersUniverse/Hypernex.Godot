@tool
class_name VideoPlayer
extends Node

@export var textureRect: TextureRect
@export var audioPlayer3d: AudioStreamPlayer3D
@export var loop: bool = false

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "VideoPlayer")

func _exit_tree() -> void:
	remove_meta(&"typename")
