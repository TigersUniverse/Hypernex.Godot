@tool
class_name WorldAsset
extends Resource

@export var name: String
@export var asset: Resource

func _get(property: StringName) -> Variant:
	if property == &"metadata/typename":
		return "WorldAsset"
	return null

func _init() -> void:
	set_meta(&"typename", null)
