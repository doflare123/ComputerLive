shader_type spatial;

uniform vec4 tint : source_color = vec4(0.45, 0.39, 0.33, 1.0);
uniform float wear = 1.0;
varying vec3 surface_position;

float grain(vec3 cell) {
	return fract(sin(dot(cell, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float mottling(vec3 p) {
	vec3 cell = floor(p);
	vec3 f = smoothstep(vec3(0.0), vec3(1.0), fract(p));
	vec2 a = mix(vec2(grain(cell), grain(cell + vec3(0, 0, 1))),
		vec2(grain(cell + vec3(1, 0, 0)), grain(cell + vec3(1, 0, 1))), f.x);
	vec2 b = mix(vec2(grain(cell + vec3(0, 1, 0)), grain(cell + vec3(0, 1, 1))),
		vec2(grain(cell + vec3(1, 1, 0)), grain(cell + vec3(1, 1, 1))), f.x);
	vec2 pair = mix(a, b, f.y);
	return mix(pair.x, pair.y, f.z);
}

void vertex() {
	surface_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	float patches = mottling(surface_position * vec3(1.4, 0.6, 1.4));
	float specks = mottling(surface_position * 43.0) - 0.5;
	float damp = (1.0 - smoothstep(0.22, 1.28, surface_position.y)) * smoothstep(0.38, 0.72, patches);
	float value = 1.0 + (patches - 0.5) * 0.36 * wear + specks * 0.15 - damp * 0.23 * wear;
	ALBEDO = tint.rgb * value;
	ROUGHNESS = 0.96;
}
