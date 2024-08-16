extends EditorNode3DGizmoPlugin

func _get_gizmo_name() -> String:
	return "ZoneGizmo"

func _has_gizmo(for_node_3d: Node3D) -> bool:
	return for_node_3d is ReverbZone

func _init() -> void:
	create_material("main", Color.AQUA)

func _redraw(gizmo: EditorNode3DGizmo) -> void:
	"""
	gizmo.clear()
	var node := gizmo.get_node_3d() as ReverbZone
	var lines := PackedVector3Array()

	var aabb := node.aabb

	var lookup := [
		1, 0, # -y
		3, 1, # +z
		2, 3, # +y
		0, 2, # -z

		4, 5, # -y
		5, 7, # +z
		7, 6, # +y
		6, 4, # -z

		1, 5,
		3, 7,
		2, 6,
		0, 4,
	]

	for i in range(0, lookup.size()-1, 2):
		lines.append(aabb.get_endpoint(lookup[i]))
		lines.append(aabb.get_endpoint(lookup[i+1]))

	gizmo.add_lines(lines, get_material("main", gizmo))
	"""
	pass