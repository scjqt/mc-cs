#version 440 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in float aLayer;
layout (location = 3) in float aNormalIndex;

out vec2 texCoord;
out float layer;
out float normalIndex;

layout (location = 0) uniform mat4 model;
layout (location = 1) uniform mat4 view;
layout (location = 2) uniform mat4 projection;

void main()
{
    texCoord = aTexCoord;
    layer = aLayer;
    normalIndex = aNormalIndex;
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
}