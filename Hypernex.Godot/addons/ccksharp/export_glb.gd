@tool
extends EditorScript

func _run() -> void:
	var avi := GltfAvatarExt.new()
	var vid := GltfVideoExt.new()
	var aud := GltfAudioExt.new()
	var grb := GltfGrabbableExt.new()
	GLTFDocument.register_gltf_document_extension(avi)
	GLTFDocument.register_gltf_document_extension(vid)
	GLTFDocument.register_gltf_document_extension(aud)
	GLTFDocument.register_gltf_document_extension(grb)
	var doc := GLTFDocument.new()
	var state := GLTFState.new()
	doc.append_from_scene(EditorInterface.get_edited_scene_root(), state)
	doc.write_to_filesystem(state, "user://%s.glb" % [EditorInterface.get_edited_scene_root().name])
	GLTFDocument.unregister_gltf_document_extension(avi)
	GLTFDocument.unregister_gltf_document_extension(vid)
	GLTFDocument.unregister_gltf_document_extension(aud)
	GLTFDocument.unregister_gltf_document_extension(grb)
