#version 330
uniform mat4 modelview_transform;
uniform float particle_scale_factor;

layout (points) in;
layout (triangle_strip, max_vertices = 12) out;

in vec3[] color;
in float[] z_orig;
in float[] z_maxdelta;
in vec2[] xy_maxdelta;
in vec4[] world_cam_z_dir;
in vec4[] world_cam_x_dir;
in vec4[] world_cam_y_dir;

//
out VertexData
{
	//-1, 1 parameterization
	vec2 param;

	//vertex color
	vec3 fcolor;

	//original z coordinate in camera space
	float z_orig;

	//delta in z coordinate in camera space
	float z_maxdelta;

	vec4 world_cam_x_dir;
	vec4 world_cam_y_dir;
	vec4 world_cam_z_dir;
} OUT;

//generates quad shaped sprite,
void main ()
{
	vec4 dx = vec4(xy_maxdelta[0].x, 0, 0, 0);
	vec4 dy = vec4(0, xy_maxdelta[0].y, 0, 0);

	OUT.param = vec2(0, 0);
	OUT.fcolor = color[0];
	OUT.z_orig = z_orig[0];
	OUT.z_maxdelta = z_maxdelta[0];
	OUT.world_cam_x_dir = world_cam_x_dir[0];
	OUT.world_cam_y_dir = world_cam_y_dir[0];
	OUT.world_cam_z_dir = world_cam_z_dir[0];
	gl_Position = gl_in[0].gl_Position - dx - dy;
	EmitVertex();

	OUT.param = vec2(0, 1);
	OUT.fcolor = color[0];
	OUT.z_orig = z_orig[0];
	OUT.z_maxdelta = z_maxdelta[0];
	OUT.world_cam_x_dir = world_cam_x_dir[0];
	OUT.world_cam_y_dir = world_cam_y_dir[0];
	OUT.world_cam_z_dir = world_cam_z_dir[0];
	gl_Position = gl_in[0].gl_Position - dx + dy;
	EmitVertex();

	OUT.param = vec2(1, 0);
	OUT.fcolor = color[0];
	OUT.z_orig = z_orig[0];
	OUT.z_maxdelta = z_maxdelta[0];
	OUT.world_cam_x_dir = world_cam_x_dir[0];
	OUT.world_cam_y_dir = world_cam_y_dir[0];
	OUT.world_cam_z_dir = world_cam_z_dir[0];
	gl_Position = gl_in[0].gl_Position + dx - dy;
	EmitVertex();

	OUT.param = vec2(1, 1);
	OUT.fcolor = color[0];
	OUT.z_orig = z_orig[0];
	OUT.z_maxdelta = z_maxdelta[0];
	OUT.world_cam_x_dir = world_cam_x_dir[0];
	OUT.world_cam_y_dir = world_cam_y_dir[0];
	OUT.world_cam_z_dir = world_cam_z_dir[0];
	gl_Position = gl_in[0].gl_Position + dx + dy;
	EmitVertex();
}