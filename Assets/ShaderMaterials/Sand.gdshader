shader_type canvas_item;

global uniform float percent_visible;

uniform bool is_top = true;
uniform float top_depth : hint_range(0.0, 0.5, 0.001) = 0.1;
uniform vec4 top_colour : source_color = vec4(1.0);
uniform vec4 base_colour : source_color = vec4(vec3(0.5), 1.0);

void fragment() {
	vec4 sample = texture(TEXTURE, UV);
	if (sample.a < 0.5) discard;
	float s = sample.r;
	if (is_top)
	{
		if (s >= percent_visible) discard;
		COLOR = base_colour;
		if (s >= (percent_visible - top_depth)) COLOR = top_colour;
	} else {
		s = 1.0 - s;
		if (s < percent_visible) discard;
		COLOR = base_colour;
		if (s < (percent_visible - top_depth)) COLOR = top_colour;
	}
	COLOR.a = 1.0;
}
