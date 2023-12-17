shader_type canvas_item; 

uniform sampler2D paletteImage : filter_nearest;

void fragment() {
	// palette size
	int paletteSize = textureSize(paletteImage, 0).x;
	// texture color
    vec3 col = texture(TEXTURE, UV).rgb;
	
    // loop through all the colors of the palette and see which one is closest to the texture color
    float closest = 9999999.0;
	vec3 finalColor;
	
    for (int i = 0; i < paletteSize; i += 1) {
		vec3 paletteColor = texture(paletteImage, vec2(float(i) * 1.0 / float(paletteSize)), 0.0).rgb;		
        vec3 dCol = col - paletteColor;
		float d = (dCol.r * dCol.r) + (dCol.g * dCol.g) + (dCol.b * dCol.b); // rgb distance squared

        if (d < closest) {
            closest = d;
			finalColor = paletteColor;
        }
    }

    COLOR = vec4(finalColor, 1.0);
}
