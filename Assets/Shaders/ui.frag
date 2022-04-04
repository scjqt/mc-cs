#version 440 core

out vec4 fragColour;

in vec4 colour;

void main()
{
    fragColour = colour;
}