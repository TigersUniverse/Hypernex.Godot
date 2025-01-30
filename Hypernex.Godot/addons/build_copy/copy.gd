@tool
extends EditorExportPlugin

func _export_begin(features, is_debug, path, flags):
	var dirPath := ""
	if features.has("windows"):
		dirPath = ProjectSettings.globalize_path("res://").path_join("export_data/windows/")
	elif features.has("linux"):
		dirPath = ProjectSettings.globalize_path("res://").path_join("export_data/linux/")
	else:
		return
	var dir := DirAccess.open(dirPath)
	for file in dir.get_files():
		prints("Copying file", file, "to", path.get_base_dir().path_join(file))
		DirAccess.copy_absolute(dirPath.path_join(file), path.get_base_dir().path_join(file))
