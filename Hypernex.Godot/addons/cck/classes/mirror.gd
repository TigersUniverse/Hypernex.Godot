@tool
class_name Mirror
extends Node3D

@export var size: Vector2 = Vector2.ONE

var quad: MeshInstance3D

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "Mirror")
	if what == NOTIFICATION_EDITOR_POST_SAVE:
		quad.mesh.size = size

func _enter_tree() -> void:
	quad = MeshInstance3D.new()
	quad.mesh = QuadMesh.new()
	quad.mesh.size = size
	add_child(quad, false, INTERNAL_MODE_FRONT)

func _exit_tree() -> void:
	remove_meta(&"typename")
	remove_child(quad)
	quad.free()
