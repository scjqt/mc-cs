#version 440 core

out vec4 fragColour;

in vec2 texCoord;
in float layer;
in float normalIndex;

layout (binding = 0) uniform sampler2DArray textures;
layout (location = 3) uniform vec3 sun;
layout (location = 4) uniform float ambience;

vec3 normals[6] = vec3[](vec3(0, 1, 0), vec3(0, -1, 0), vec3(0, 0, 1), vec3(0, 0, -1), vec3(1, 0, 0), vec3(-1, 0, 0));

void main()
{
    vec3 ambient = ambience * vec3(1.0, 1.0, 1.0);
    vec3 diffuse = max(dot(normals[int(round(normalIndex))], -normalize(sun)), 0.0) * vec3(1.0, 1.0, 1.0) * (1 - ambience);

    fragColour = vec4((ambient + diffuse), 1.0) * texture(textures, vec3(texCoord, int(round(layer))));
}