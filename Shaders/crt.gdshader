shader_type canvas_item;

const float warp = 0.5;

void fragment() {
	vec2 uv = UV;
	vec2 dc = abs(0.5-uv);
    dc *= dc;
    
    // warp the fragment coordinates
    uv.x -= 0.5; uv.x *= 1.0+(dc.y*(0.3*warp)); uv.x += 0.5;
    uv.y -= 0.5; uv.y *= 1.0+(dc.x*(0.4*warp)); uv.y += 0.5;

    // sample inside boundaries, otherwise set to black
    if (uv.y > 1.0 || uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0)
        COLOR = vec4(0.0,0.0,0.0,1.0);
    else
    {
        // sample the texture
    	COLOR = texture(TEXTURE, uv);
    }
}
