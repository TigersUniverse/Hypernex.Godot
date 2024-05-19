@tool
class_name WorldScript
extends Node

enum NexboxLanguage { Unknown = -1, JavaScript, Lua }

@export var Language : NexboxLanguage
@export_multiline var Contents : String

func _notification(what):
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "WorldScript")

func _exit_tree():
	remove_meta(&"typename")
