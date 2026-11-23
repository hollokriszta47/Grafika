#version 330 core

layout(location = 0) in vec3 vPos;
layout(location = 1) in vec4 vCol;
layout(location = 2) in vec3 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform float uShininess;

out vec4 vertexColor;

void main()
{
	vec3 worldPosition = vec3(uModel * vec4(vPos, 1.0));
	vec3 norm = normalize(uNormal * vNormal);

	vec3 ambient = 0.1 * uLightColor;

	vec3 lightDir = normalize(uLightPos - worldPosition);
	float diff = max(dot(norm,lightDir),0.0);
	vec3 diffuse = 0.3 * diff * uLightColor;

	vec3 viewDir = normalize(uViewPos - worldPosition);
	vec3 reflectDir = reflect(-lightDir, norm);
	float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);

	vec3 result = (ambient + diffuse + spec) * vCol.rgb;
	vertexColor = vec4(result, vCol.a);

	gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);	
}