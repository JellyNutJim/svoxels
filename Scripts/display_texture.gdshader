shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, unshaded;

uniform sampler2D compute_texture : source_color;

void fragment() {
    vec4 color = texture(compute_texture, UV);
    ALBEDO = color.rgb;
    ALPHA = color.a;
}