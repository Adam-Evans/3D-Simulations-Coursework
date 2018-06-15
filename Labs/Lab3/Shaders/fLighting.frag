#version 330

uniform vec4 uLightPosition;
uniform vec4 uEyePosition;
float ambient = 0.1;

in vec4 oNormal;
in vec4 oSurfacePosition;

out vec4 FragColour;

void main()
{
vec4 lightDir = normalize(uLightPosition - oSurfacePosition);
float diffuseFactor = max(dot(oNormal, lightDir), 0);
vec4 eyeDirection = normalize(uEyePosition - oSurfacePosition);
vec4 reflectedVector = reflect(-lightDir, oNormal);
float specularFactor = pow(max(dot( reflectedVector, eyeDirection), 0.0), 30);
FragColour = vec4(vec3(specularFactor + diffuseFactor + ambient), 1);
}