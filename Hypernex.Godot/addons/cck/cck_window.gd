@tool
extends Node

@export var export_avatar_ui: Control
@export var avatar_name_edit: LineEdit

@export var export_world_ui: Control
@export var world_name_edit: LineEdit

func _process(delta: float) -> void:
	var root := EditorInterface.get_edited_scene_root()
	var found_world := false
	if root:
		var world_desc := root.find_children("*", "WorldDescriptor", true, false)
		if not world_desc.is_empty():
			found_world = true
	export_world_ui.visible = found_world
	var found_avatar := false
	if root:
		var avatar_desc := root.find_children("*", "AvatarDescriptor", true, false)
		if not avatar_desc.is_empty():
			found_avatar = true
	export_avatar_ui.visible = found_avatar

func export_world() -> void:
	OS.alert(CCKPlugin.export_world(world_name_edit.text))

func export_avatar() -> void:
	OS.alert(CCKPlugin.export_world(avatar_name_edit.text))
