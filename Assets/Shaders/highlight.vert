#version 440 core

layout (location = 0) in vec3 aPosition;

layout (location = 0) uniform mat4 model;
layout (location = 1) uniform mat4 view;
layout (location = 2) uniform mat4 projection;

void main()
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
}