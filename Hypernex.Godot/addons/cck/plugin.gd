@tool
class_name CCKPlugin
extends EditorPlugin

var cck_dock: Node = null

func _enter_tree():
	add_tool_menu_item("Export World", export_world_quick)
	cck_dock = load("res://addons/cck/cck_dock.tscn").instantiate()
	add_control_to_dock(EditorPlugin.DOCK_SLOT_LEFT_UR, cck_dock)

func _exit_tree():
	remove_tool_menu_item("Export World")
	remove_control_from_docks(cck_dock)

static func export_asset(res: Resource, path: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_pascal_string(res.get_class())
	for prop in res.get_property_list():
		var prop_name : String = prop["name"]
		var prop_val : Variant = res.get(prop_name)
		file.store_pascal_string(prop_name)
		if prop_val is Resource:
			file.store_8(1)
			if prop_val.resource_path.is_empty():
				printerr("Not able to export ", prop_val)
			file.store_pascal_string(prop_val.resource_path)
		else:
			file.store_8(0)
			file.store_var(prop_val)
	file.close()

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
		elif dep_res.is_class("Texture"):
			# write raw file
			var dep_file := FileAccess.get_file_as_bytes(dep_path)
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(dep_file)
			writer.close_file()
		elif dep_res.is_class("AudioStream") or true:
			# write binary file
			export_asset(dep_res, temp_path)
			var dep_file := FileAccess.get_file_as_bytes(temp_path)
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(dep_file)
			writer.close_file()
		else:
			# write text-version of file
			ResourceSaver.save(dep_res, temp_path)
			var dep_file := FileAccess.get_file_as_bytes(temp_path)
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(dep_file)
			writer.close_file()
		export_deps(writer, dep_path)

class TscnState extends RefCounted:
	var path := ""
	var nodes := []
	var sub_tres := []
	var ext_tres := []
	
static func export_prop(val: Variant, scn_state: TscnState) -> Variant:
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
	print(path)
	var dep_path := path
	var scn := ResourceLoader.load(path) as PackedScene
	var state := scn.get_state()
	var scn_state := TscnState.new()
	scn_state.path = path
	for i in range(state.get_node_count()):
		var props := {}
		for j in range(state.get_node_property_count(i)):
			var val := state.get_node_property_value(i, j)
			props[state.get_node_property_name(i, j)] = export_prop(val, scn_state)
		var inst := ""
		if state.get_node_instance(i):
			inst = export_prop(state.get_node_instance(i), scn_state)
		scn_state.nodes.append({
			"name": str(state.get_node_name(i)),
			"type": str(state.get_node_type(i)),
			"parent": str(state.get_node_path(i, true)),
			"instance": str(inst),
			"props": props,
		})
	var dict := {"nodes": scn_state.nodes, "sub_resources": scn_state.sub_tres, "ext_resources": scn_state.ext_tres, "connections": []}

	writer.start_file(dep_path.replacen("res://", "").replacen("." + dep_path.get_extension(), ".scn"))
	writer.write_file(var_to_bytes(dict))
	writer.close_file()


static func export_world_quick() -> void:
	OS.alert(export_world("my_world"))

static func export_world(name: String) -> String:
	var root := EditorInterface.get_edited_scene_root()
	if not root:
		return "No Scene Open"
	DirAccess.make_dir_absolute("res://temp/")
	DirAccess.make_dir_absolute("user://temp/")

	# write zip file
	var writer := ZIPPacker.new()
	var path := "user://" + name.replace(".", "") + ".hnw"
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
	return "Exported World"
