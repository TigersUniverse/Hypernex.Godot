@tool
class_name GrabbableDescriptor
extends Node

@export var ApplyVelocity: bool = true
@export var VelocityAmount: float = 10.0
@export var VelocityThreshold: float = 0.05
@export var GrabByLaser: bool = true
@export var LaserGrabDistance: float = 5.0
@export var GrabByDistance: bool = true
@export var GrabDistance: float = 3.0

func _notification(what) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		set_meta(&"typename", "GrabbableDescriptor")

func _exit_tree() -> void:
	remove_meta(&"typename")
