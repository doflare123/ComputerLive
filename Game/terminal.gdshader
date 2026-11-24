shader_type spatial;
render_mode unshaded;

void fragment() {
	vec2 pixel = floor(UV * vec2(92.0, 64.0));
	vec2 scan = abs(fract(UV * vec2(92.0, 64.0)) - 0.5);
	vec2 from_center = UV - 0.5;
	float falloff = 1.0 - dot(from_center, from_center) * 0.76;
	float frame = float(pixel.x > 7.0 && pixel.x < 84.0 && pixel.y > 7.0 && pixel.y < 55.0);
	float heading = float(pixel.x > 11.0 && pixel.x < 61.0 && pixel.y > 12.0 && pixel.y < 17.0);
	float row = floor((pixel.y - 24.0) / 6.0);
	float text_line = float(pixel.x > 12.0 && pixel.x < 70.0 - 6.0 * row && row >= 0.0 && row < 4.0);
	float spaces = float(mod(pixel.x, 7.0) != 0.0 && mod(pixel.y, 6.0) < 3.0);
	float scanline = mix(1.0, 0.73, smoothstep(0.30, 0.49, scan.y));
	ALBEDO = (vec3(0.018, 0.055, 0.042) + vec3(0.032, 0.103, 0.071) * frame
		+ vec3(0.17, 0.68, 0.39) * (heading + text_line * spaces)) * falloff * scanline;
}
