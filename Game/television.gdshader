shader_type spatial;
render_mode unshaded;

uniform sampler2D signal_image : source_color, filter_linear;

void fragment() {
	vec2 centered = UV - 0.5;
	vec2 screen_uv = UV + centered * dot(centered, centered) * 0.1;
	float bezel = float(all(greaterThanEqual(screen_uv, vec2(0.0))) && all(lessThanEqual(screen_uv, vec2(1.0))));
	screen_uv.x += sin(screen_uv.y * 86.0 + TIME * 8.0) * 0.0009;
	vec3 frame = texture(signal_image, screen_uv).rgb;
	float scan = 0.9 + 0.1 * sin(screen_uv.y * 1170.0);
	float roll = 0.92 + 0.08 * smoothstep(0.0, 0.13, abs(fract(TIME * 0.16) - screen_uv.y));
	ALBEDO = frame * scan * roll * bezel;
}
