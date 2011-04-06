using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;

namespace opentk.System3
{
	public partial class System3
	{
		//
		private ArrayObject m_ParticleRenderingState;
		//
		private Program m_ParticleRenderingProgram;
		//
		private UniformState m_UniformState;
		//
		private MatrixStack m_TransformationStack;
		//
		private MatrixStack m_Projection;
		//
		private BufferObject<Vector4> PositionBuffer;
		//
		private BufferObject<Vector4> DimensionBuffer;
		//
		private State m_SystemState;
		//
		private Grid m_Grid;

		unsafe void PrepareState ()
		{
			if (m_ParticleRenderingState != null)
			{
				Simulate (DateTime.Now);

				PositionBuffer.Publish ();
				DimensionBuffer.Publish ();
				m_SystemState.Activate ();
				return;
			}

			unsafe
			{
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				DimensionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			var lookat = Matrix4.LookAt(100, 100, 100, 0,0,0, 1, 0, 0);
			lookat.Transpose();
			var ortho = Matrix4.CreateOrthographic (1,1, NEAR, FAR);
			ortho.Transpose();

			m_Projection = new MatrixStack ().Push (ortho);
			m_TransformationStack = new MatrixStack (m_Projection).Push (lookat).Push (Matrix4.Identity);

			m_UniformState = new UniformState ()
			.Set ("color", new Vector4 (0, 0, 1, 1))
			.Set ("red", 1.0f)
			.Set ("green", 0.0f)
			.Set ("blue", 1.0f)
			.Set ("colors", new float[] { 0, 1, 0, 1 })
			.Set ("colors2", new Vector4[] { new Vector4 (1, 0.1f, 0.1f, 0), new Vector4 (1, 0, 0, 0), new Vector4 (1, 1, 0.1f, 0) })
			.Set ("modelview_transform", m_TransformationStack);
			
			m_ParticleRenderingState = new ArrayObject (
			                                            new VertexAttribute { AttributeName = "sprite_pos", Buffer = PositionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
			                                            new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = DimensionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float });
			m_ParticleRenderingProgram = new Program ("main_program", GetShaders().ToArray ());

			m_SystemState = new State (null, m_ParticleRenderingState, m_ParticleRenderingProgram, m_UniformState);
			
			var hnd = PositionBuffer.Handle;
			hnd = DimensionBuffer.Handle;

			m_Grid = new Grid(m_TransformationStack);
			InitializeSystem();
			PrepareState ();
		}

		private void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = VIEWPORT_WIDTH;
			GL.Viewport (0, 0, window.Width, window.Height);
			
			if (m_Projection != null)
			{
				var ortho = Matrix4.CreateOrthographic (projw, projw * aspect, NEAR, FAR);
				ortho.Transpose();
				m_Projection.Stack[0] = ortho;
			}
		}
	}
}

