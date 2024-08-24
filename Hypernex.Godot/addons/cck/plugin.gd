@tool
class_name CCKPlugin
extends EditorPlugin

const ZoneGizmo := preload("res://addons/cck/classes/zone_gizmo.gd")

var cck_dock: Node = null
var zone_gizmo_plugin := ZoneGizmo.new()

func _enter_tree():
	add_tool_menu_item("Export World", export_world_quick)
	cck_dock = load("res://addons/cck/cck_dock.tscn").instantiate()
	add_control_to_dock(EditorPlugin.DOCK_SLOT_LEFT_UR, cck_dock)
	# add_node_3d_gizmo_plugin(zone_gizmo_plugin)

func _exit_tree():
	remove_tool_menu_item("Export World")
	remove_control_from_docks(cck_dock)
	# remove_node_3d_gizmo_plugin(zone_gizmo_plugin)

static func export_deps(writer: ZIPPacker, path: String) -> void:
	for dep in ResourceLoader.get_dependencies(path):
		var dep_path := dep.get_slice("::", 2)
		var dep_res := ResourceLoader.load(dep_path)
		if not dep_res:
			continue
		if dep_res.is_class("Script"):
			continue
		var temp_path := "user://temp/temp.tres"
		if dep_res.is_class("PackedScene"):
			# write tscn
			export_scn(writer, dep_path)
		elif dep_res.is_class("CompressedTexture2D"):
			# write raw file
			var dep_file := FileAccess.get_file_as_bytes(dep_path)
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(dep_file)
			writer.close_file()
		else:
			# write tres
			var state := TscnState.new()
			state.path = dep_path
			var res_dict := export_res(dep_res.resource_path, dep_res, state)
			var dict := {"resource": res_dict, "sub_resources": state.sub_tres, "ext_resources": state.ext_tres}
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(var_to_bytes(dict))
			writer.close_file()
			export_deps(writer, dep_res.resource_path)
		export_deps(writer, dep_path)

class TscnState extends RefCounted:
	var path := ""
	# var nodes := []
	var sub_tres := []
	var ext_tres := []
	
static func export_prop(val: Variant, scn_state: TscnState) -> Variant:
	if val is Array:
		var arr = []
		for i in val:
			arr.push_back(export_prop(i, scn_state))
		return arr
	if val is Dictionary:
		var dict = {}
		for i in val:
			dict[i] = export_prop(val[i], scn_state)
		return dict
	if val is EncodedObjectAsID:
		return export_prop(instance_from_id(val.object_id), scn_state)
	var s := Marshalls.variant_to_base64(val)
	if val is Resource:
		if val.resource_path.begins_with(scn_state.path):
			var id := val.resource_path.substr(val.resource_path.find("::") + 2) as String
			s = str("SubResource(\"", id, "\")")
			if not scn_state.sub_tres.any(func(v): return v["id"] == id):
				scn_state.sub_tres.append(export_res(id, val, scn_state))
		else:
			s = str("ExtResource(\"", val.get_instance_id(), "\")")
			scn_state.ext_tres.append({
				"id": val.get_instance_id(),
				"path": val.resource_path,
			})
	return s

static func export_res(id: String, res: Resource, scn_state: TscnState) -> Dictionary:
	var props := {}
	for p in res.get_property_list():
		props[p["name"]] = export_prop(res.get(p["name"]), scn_state)
	return {
		"id": id,
		"type": res.get_class(),
		"props": props,
	}

static func export_scn(writer: ZIPPacker, path: String) -> void:
	var dep_path := path
	var scn := ResourceLoader.load(path) as PackedScene
	var state := scn.get_state()
	var scn_state := TscnState.new()
	scn_state.path = path
	var nodes := []
	for i in range(state.get_node_count()):
		var props := {}
		for j in range(state.get_node_property_count(i)):
			var val := state.get_node_property_value(i, j)
			props[state.get_node_property_name(i, j)] = export_prop(val, scn_state)
		var inst := ""
		if state.get_node_instance(i):
			inst = export_prop(state.get_node_instance(i), scn_state)
		nodes.append({
			"name": str(state.get_node_name(i)),
			"type": str(state.get_node_type(i)),
			"parent": str(state.get_node_path(i, true)),
			"instance": str(inst),
			"props": props,
		})
	var dict := {"nodes": nodes, "sub_resources": scn_state.sub_tres, "ext_resources": scn_state.ext_tres, "connections": []}

	"""
	DirAccess.make_dir_recursive_absolute(dep_path.replacen("res://", "user://").replacen("." + dep_path.get_extension(), ".scn").get_base_dir())
	var file := FileAccess.open(dep_path.replacen("res://", "user://").replacen("." + dep_path.get_extension(), ".scn"), FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(dict, "\t"))
		file.close()
	"""

	writer.start_file(dep_path.replacen("res://", "").replacen("." + dep_path.get_extension(), ".scn"))
	writer.write_file(var_to_bytes(dict))
	writer.close_file()


static func export_world_quick() -> void:
	OS.alert(export_world("my_world", "hnw"))

static func export_world(name: String, ext: String) -> String:
	var root := EditorInterface.get_edited_scene_root()
	if not root:
		return "No Scene Open"
	DirAccess.make_dir_absolute("res://temp/")
	DirAccess.make_dir_absolute("user://temp/")

	# write zip file
	var writer := ZIPPacker.new()
	var path := "user://" + name.replace(".", "") + "." + ext
	var err := writer.open(path)
	if err != OK:
		return error_string(err)
	var scn_path := root.scene_file_path.replacen("res://", "").replacen("." + root.scene_file_path.get_extension(), ".scn")
	writer.start_file("world.txt")
	writer.write_file(scn_path.to_utf8_buffer())
	writer.close_file()

	export_scn(writer, root.scene_file_path)

	export_deps(writer, root.scene_file_path)
	writer.close()
	return "Exported Scene"
