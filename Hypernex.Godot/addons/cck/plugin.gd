@tool
extends EditorPlugin

func _enter_tree():
	add_tool_menu_item("Export World", export_world)

func _exit_tree():
	remove_tool_menu_item("Export World")

func export_deps(writer : ZIPPacker, path : String) -> void:
	for dep in ResourceLoader.get_dependencies(path):
		var dep_path := dep.get_slice("::", 2)
		var dep_res := ResourceLoader.load(dep_path)
		if dep_res.is_class("Script"):
			continue
		if dep_res.is_class("Texture") or dep_res.is_class("AudioStreamMP3"):
			# write raw file
			var dep_file := FileAccess.get_file_as_bytes(dep_path)
			writer.start_file(dep_path.replacen("res://", ""))
			writer.write_file(dep_file)
			writer.close_file()
		else:
			# write text-version of file
			ResourceSaver.save(dep_res, "user://temp/temp.tres")
			var dep_file := FileAccess.open("user://temp/temp.tres", FileAccess.READ)
			writer.start_file(dep_path.replacen("res://", "").replacen("." + dep_path.get_extension(), ".tres"))
			writer.write_file(dep_file.get_as_text().to_utf8_buffer())
			writer.close_file()
			dep_file.close()
		export_deps(writer, dep_path)

func export_world():
	var root := EditorInterface.get_edited_scene_root()
	DirAccess.make_dir_absolute("res://temp/")
	DirAccess.make_dir_absolute("user://temp/")
	# make all-local scene file
	# var temp_root := root.duplicate(Node.DUPLICATE_SIGNALS | Node.DUPLICATE_GROUPS | Node.DUPLICATE_USE_INSTANTIATION)
	# ResourceSaver.save(ResourceLoader.load(root.scene_file_path), "res://temp/temp.tscn", ResourceSaver.FLAG_BUNDLE_RESOURCES)

	# write zip file
	var writer := ZIPPacker.new()
	assert(writer.open("res://temp/bin.zip") == OK)
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
