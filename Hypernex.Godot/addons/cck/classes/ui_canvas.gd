@tool
class_name UICanvas
extends Node3D

@export var size: Vector2 = Vector2.ONE
@export var subViewport: SubViewport
@export var material: Material

var quad: MeshInstance3D

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "UICanvas")
	if what == NOTIFICATION_EDITOR_POST_SAVE:
		quad.mesh.size = size
		if material:
			quad.material_override = material
		else:
			quad.material_override = StandardMaterial3D.new()
			quad.material_override.albedo_texture = subViewport.get_texture()
			quad.material_override.transparency = StandardMaterial3D.TRANSPARENCY_ALPHA

func _enter_tree() -> void:
	quad = MeshInstance3D.new()
	quad.mesh = QuadMesh.new()
	quad.mesh.size = size
	add_child(quad, false, INTERNAL_MODE_FRONT)
	if material:
		quad.material_override = material
	else:
		quad.material_override = StandardMaterial3D.new()
		quad.material_override.albedo_texture = subViewport.get_texture()
		quad.material_override.transparency = StandardMaterial3D.TRANSPARENCY_ALPHA

func _exit_tree() -> void:
	remove_meta(&"typename")
	remove_child(quad)
	quad.free()
