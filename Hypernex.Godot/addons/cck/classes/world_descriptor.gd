@tool
class_name WorldDescriptor
extends Node3D

@export var StartPositions: Array[Node3D] = []
@export var Assets: Array[WorldAsset] = []

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		for i in range(Assets.size()):
			Assets[i].set_meta(&"typename", "WorldAsset")
		set_meta(&"typename", "WorldDescriptor")

func _exit_tree() -> void:
	remove_meta(&"typename")
