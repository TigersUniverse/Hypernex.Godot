@tool
extends EditorPlugin

const Copy = preload("res://addons/build_copy/copy.gd")

var plugin

func _enter_tree():
	plugin = Copy.new()
	add_export_plugin(plugin)

func _exit_tree():
	remove_export_plugin(plugin)
