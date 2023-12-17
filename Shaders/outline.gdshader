shader_type canvas_item;

void fragment() {
	vec2 offset = TEXTURE_PIXEL_SIZE;
	
	// 9x9 offsets
	
	vec2 offsets[] = {
		vec2(-offset.x, -offset.y), // top left
		vec2(      0.0, -offset.y), // top middle
		vec2( offset.x, -offset.y), // top right
		vec2(-offset.x,       0.0), // middle left
		vec2(      0.0,       0.0), // middle middle
		vec2( offset.x,       0.0), // middle right
		vec2(-offset.x,  offset.y), // bottom left
		vec2(      0.0,  offset.y), // bottom middle
		vec2( offset.x,  offset.y)  // bottom right
	};
	
	// kernel
	
	float kernel[] = {
		-1.0, -1.0, -1.0,
		-1.0,  8.0, -1.0,
		-1.0, -1.0, -1.0	
	};
	
	// compute the kernel
	
	float sum = 0.0;
	
	for (int i = 0; i < 9; i++) {
		vec3 texColor = texture(TEXTURE, UV + offsets[i]).rgb;
		
		texColor = (texColor != vec3(0.0)) ? vec3(1.0) : vec3(0.0);
		
		float grayScale = (texColor.r + texColor.g + texColor.b) / 3.0;
		sum += grayScale * kernel[i];
	}
	
	sum = clamp(sum, 0.0, 1.0);
	COLOR = vec4(vec3(sum) * texture(TEXTURE, UV).rgb, 1.0);
}
