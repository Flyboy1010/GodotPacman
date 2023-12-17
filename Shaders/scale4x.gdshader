shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : filter_nearest_mipmap;

const float offset_x = 1.0 / 224.0;
const float offset_y = 1.0 / 288.0;

const float offset2_x = 1.0 / (224.0 * 2.0);
const float offset2_y = 1.0 / (288.0 * 2.0);

vec4 scale2x(sampler2D t, vec2 pos)
{
    vec4 c;

    vec4 A = texture(t, pos + vec2(0.0, offset_y));
    vec4 B = texture(t, pos + vec2(offset_x, 0.0));
    vec4 C = texture(t, pos + vec2(-offset_x, 0.0));
    vec4 D = texture(t, pos + vec2(0.0, -offset_y));
    vec4 e = texture(t, pos + vec2(0.0, 0.0));

    vec2 p = pos * vec2(224, 288);
    // p = the position within a pixel [0...1]
    p = fract(p);
     if (p.x > 0.5) {
        if (p.y > 0.5) {
            // Top Right

            c = (A==B && A!=C && B!=D) ? B : e;
        } else {
            // Bottom Right

            c = (B==D && B!=A && D!=C) ? D : e;
        }
    } else {
        if (p.y > 0.5) {
            // Top Left
            c = (C==A && C!=D && A!=B) ? A : e;
        } else {
            // Bottom Left
            c = (D==C && D!=B && C!=A) ? C : e;
        }
    }

    return c;
}

void fragment()
{
    vec4 A = scale2x(SCREEN_TEXTURE, UV + vec2(0.0, offset2_y));
    vec4 B = scale2x(SCREEN_TEXTURE, UV + vec2(offset2_x, 0.0));
    vec4 C = scale2x(SCREEN_TEXTURE, UV + vec2(-offset2_x, 0.0));
    vec4 D = scale2x(SCREEN_TEXTURE, UV + vec2(0.0, -offset2_y));
    vec4 e = scale2x(SCREEN_TEXTURE, UV + vec2(0.0, 0.0));

    vec2 p = UV * vec2(224, 288) * 2.0;
    // p = the position within a pixel [0...1]
    p = fract(p);
     if (p.x > 0.5) {
        if (p.y > 0.5) {
            // Top Right

            COLOR = (A==B && A!=C && B!=D) ? B : e;
        } else {
            // Bottom Right

            COLOR = (B==D && B!=A && D!=C) ? D : e;
        }
    } else {
        if (p.y > 0.5) {
            // Top Left
            COLOR = (C==A && C!=D && A!=B) ? A : e;
        } else {
            // Bottom Left
            COLOR = (D==C && D!=B && C!=A) ? C : e;
        }
    }
}
