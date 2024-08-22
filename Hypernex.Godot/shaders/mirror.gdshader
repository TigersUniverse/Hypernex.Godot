shader_type spatial;
render_mode cull_back, unshaded;

uniform sampler2D mirror_tex_left : source_color, repeat_disable;
uniform sampler2D mirror_tex_right : source_color, repeat_disable;

void vertex() {
	UV.x = 1.0 - UV.x;
}

void fragment() {
	vec2 m_UV = UV;
	
	if (!FRONT_FACING) {
		m_UV.x = 1.0 - m_UV.x;
	}
	
	vec4 mirror_sample_l = texture(mirror_tex_left, m_UV);
	vec4 mirror_sample_r = texture(mirror_tex_right, m_UV);
	ALBEDO = mix(mirror_sample_l.rgb, mirror_sample_r.rgb, float(VIEW_INDEX));
	ALPHA = mix(mirror_sample_l.a, mirror_sample_r.a, float(VIEW_INDEX));
}
