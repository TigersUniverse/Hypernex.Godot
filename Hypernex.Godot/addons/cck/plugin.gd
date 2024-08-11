@tool
extends EditorPlugin

func _enter_tree():
	add_tool_menu_item("Export World", export_world)

func _exit_tree():
	remove_tool_menu_item("Export World")

func export_asset(res : Resource, path : String) -> void:
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

func export_deps(writer : ZIPPacker, path : String) -> void:
	for dep in ResourceLoader.get_dependencies(path):
		var dep_path := dep.get_slice("::", 2)
		var dep_res := ResourceLoader.load(dep_path)
		if not dep_res:
			continue
		if dep_res.is_class("Script"):
			continue
		var temp_path := "user://temp/temp.tres"
		if dep_res.is_class("Texture"):
			# write raw file
			var dep_file := FileAccess.get_file_as_bytes(dep_path)
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(dep_file)
			writer.close_file()
		elif dep_res.is_class("AudioStream") or true:
			# write binary file
			export_asset(dep_res, temp_path)
			var dep_file := FileAccess.get_file_as_bytes(temp_path)
			writer.start_file(dep_path.replacen("res://", "").replacen("." + dep_path.get_extension(), ".asset"))
			writer.write_file(dep_file)
			writer.close_file()
		else:
			# write text-version of file
			ResourceSaver.save(dep_res, temp_path)
			var dep_file := FileAccess.get_file_as_bytes(temp_path)
			writer.start_file(dep_path.replacen("res://", "").replacen("." + dep_path.get_extension(), ".tres"))
			writer.write_file(dep_file)
			writer.close_file()
		export_deps(writer, dep_path)

func export_world():
	var root := EditorInterface.get_edited_scene_root()
	DirAccess.make_dir_absolute("res://temp/")
	DirAccess.make_dir_absolute("user://temp/")

	# write zip file
	var writer := ZIPPacker.new()
	assert(writer.open("user://my_world.hnw") == OK)
	var scn_path := root.scene_file_path.replacen("res://", "")
	writer.start_file("world.txt")
	writer.write_file(scn_path.to_utf8_buffer())
	writer.close_file()

	var scn_file := FileAccess.open(root.scene_file_path, FileAccess.READ)
	writer.start_file(scn_path)
	writer.write_file(scn_file.get_as_text().to_utf8_buffer())
	writer.close_file()
	scn_file.close()

	export_deps(writer, root.scene_file_path)
	writer.close()
