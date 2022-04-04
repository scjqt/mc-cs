#version 440 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec4 aColour;

out vec4 colour;

void main() 
{
    colour = aColour;
    gl_Position = vec4(aPosition, 0.0, 1.0);
}