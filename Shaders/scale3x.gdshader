shader_type canvas_item;
  
uniform sampler2D SCREEN_TEXTURE : filter_nearest;

const vec2 texture_size = vec2(224.0, 288.0);
const vec2 offset = 1.0 / texture_size;

const float one_third = 1.0 / 3.0;
const float two_thirds = 2.0 / 3.0;

void fragment()
{
    vec4 a = texture(SCREEN_TEXTURE, UV + vec2(-offset.x, offset.y));
    vec4 b = texture(SCREEN_TEXTURE, UV + vec2(0.0, offset.y));
    vec4 c = texture(SCREEN_TEXTURE, UV + vec2(offset.x, offset.y));
    vec4 d = texture(SCREEN_TEXTURE, UV + vec2(-offset.x, 0.0));
    vec4 e = texture(SCREEN_TEXTURE, UV + vec2(0.0, 0.0));
    vec4 f = texture(SCREEN_TEXTURE, UV + vec2(offset.x, 0.0));
    vec4 g = texture(SCREEN_TEXTURE, UV + vec2(-offset.x, -offset.y));
    vec4 h = texture(SCREEN_TEXTURE, UV + vec2(0.0, -offset.y));
    vec4 i = texture(SCREEN_TEXTURE, UV + vec2(offset.x, -offset.y));

    vec2 p = UV * texture_size;

    // p = the position within a pixel [0...1]

    p = fract(p);

    // choose texel

    if (p.x > two_thirds)
    {
        if (p.y > two_thirds)
        {
            // top right

            COLOR = (b==f && b!=d && f!=h) ? f : e;
        }
        else if (p.y > one_third)
        {
            // center right

            COLOR = ((b==f && b!=d && f!=h && e!=i) || (f==h && f!=b && h!=d && e!=c)) ? f : e;
        }
        else
        {
            // bottom right

            COLOR = (f==h && f!=b && h!=d) ? f : e;
        }
    }
    else if (p.x > one_third)
    {
        if (p.y > two_thirds)
        {
            // center top

            COLOR = ((d==b && d!=h && b!=f && e!=c) || (b==f && b!=d && f!=h && e!=a)) ? b : e;
        }
        else if (p.y > one_third)
        {
            // center center

            COLOR = e;
        }
        else
        {
            // center bottom

            COLOR = ((f==h && f!=b && h!=d && e!=g) || (h==d && h!=f && d!=b && e!=i)) ? h : e;
        }
    }
    else 
    {
        if (p.y > two_thirds)
        {
            // left top

            COLOR = (d==b && d!=h && b!=f) ? d : e;
        }
        else if (p.y > one_third)
        {
            // left center

            COLOR = ((h==d && h!=f && d!=b && e!=a) || (d==b && d!=h && b!=f && e!=g)) ? d : e;
        }
        else
        {
            // left bottom

            COLOR = (h==d && h!=f && d!=b) ? d : e;
        }
    }
}