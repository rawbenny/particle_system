using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OpenTK
{
	/// <summary>
	///
	/// </summary>
	public class Program : StatePart, IHandle, IEnumerable<Shader>
	{
		public readonly string Name;
		public int Handle
		{
			get;
			private set;
		}

		public bool? Linked
		{
			get;
			private set;
		}
		
		public IEnumerable<Shader> Shaders
		{
			get;
			private set;
		}

		public IEnumerable<string> ShaderLogs
		{
			get { return Shaders.Select (x => string.Format ("shader {0}:\n{1}", x.Name, x.Log)); }
		}
		
		public IEnumerable<string> Uniforms
		{
			get{
				var result = new List<string>();
				for(int i = 0; i < GLExtensions.GetProgramInterfaceiv(Handle, ProgramInterface.Uniform, InterfaceProperty.ActiveResources); i++)
				{
					result.Add (GLExtensions.GetProgramResourceName(Handle, ProgramInterface.Uniform, i));
				}
				
				return result;
			}
		}
		
		public IEnumerable<string> ShaderStorage
		{
			get{
				var result = new List<string>();
				for(int i = 0; i < GLExtensions.GetProgramInterfaceiv(Handle, ProgramInterface.ShaderStorageBlock, InterfaceProperty.ActiveResources); i++)
				{
					result.Add (GLExtensions.GetProgramResourceName(Handle, ProgramInterface.ShaderStorageBlock, i));
				}
				
				return result;
			}
		}
		
		public IEnumerable<string> BufferVariables
		{
			get{
				var result = new List<string>();
				for(int i = 0; i < GLExtensions.GetProgramInterfaceiv(Handle, ProgramInterface.BufferVariable, InterfaceProperty.ActiveResources); i++)
				{
					result.Add (GLExtensions.GetProgramResourceName(Handle, ProgramInterface.BufferVariable, i));
				}
				
				return result;
			}
		}

		public string Log
		{
			get { return GL.GetProgramInfoLog (Handle); }
		}

		public Program (string name)
		{
			Handle = GL.CreateProgram ();
			Name = name;
			Shaders = new Shader[0];
		}

		public Program (string name, params Shader[] shaders) : this(name)
		{
			Shaders = Array.AsReadOnly (shaders);
			
			Console.WriteLine ("Program <{0}> declared, shaders: {1}", Name, String.Join (", ", shaders.Select (x => x.Name)));
		}
		
		public Program (string name, IEnumerable<Shader> shaders) : this(name)
		{
			Shaders = Array.AsReadOnly (shaders.ToArray());
			
			Console.WriteLine ("Program <{0}> declared, shaders: {1}", Name, String.Join (", ", shaders.Select (x => x.Name)));
		}
		
		public void Add(Shader shader)
		{
			Shaders = Array.AsReadOnly(Shaders.Concat(new []{ shader }).ToArray ());
			Console.WriteLine ("Program <{0}> declared, shaders: {1}", Name, shader.Name);
		}
		
		public void Add(IEnumerable<Shader> shaders)
		{
			Shaders = Array.AsReadOnly(Shaders.Concat(shaders).ToArray ());
			Console.WriteLine ("Program <{0}> declared, shaders: {1}", Name, String.Join (", ", shaders.Select (x => x.Name)));
		}

		private void Link ()
		{
			foreach (var item in Shaders)
			{
				if (!item.Compiled.HasValue)
					item.Compile ();
				
				GL.AttachShader (Handle, item.Handle);
			}
			
			GL.LinkProgram (Handle);
			
			int result;
			GL.GetProgram (Handle, ProgramParameter.LinkStatus, out result);
			Linked = result == 1;
			
			if (Linked.Value)
				Console.WriteLine ("Program <{0}> linked:\n{1}\nshader logs:\n{2}", Name, Log, string.Join(Environment.NewLine, ShaderLogs));
			else
				Console.WriteLine ("Program <{0}> error:\n{1}\n----------\n{2}", Name, Log, string.Join(Environment.NewLine, ShaderLogs));
		}

		internal void EnsureLinked ()
		{
			if (!Linked.HasValue ||
			   Shaders.Any (s => !s.Compiled.HasValue))
				Link ();
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			return new Tuple<Action, Action> (() =>
			{
				EnsureLinked ();
				GL.UseProgram (Handle);
			}, null);
		}

		#region IEnumerable implementation

		IEnumerator<Shader> IEnumerable<Shader>.GetEnumerator ()
		{
			return Shaders.GetEnumerator();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return Shaders.GetEnumerator();
		}

		#endregion

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
			GL.DeleteProgram (Handle);
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public class Shader : IDisposable, IHandle
	{
		//
		private static readonly Regex m_IncludeRegex = new Regex(
			@"\#pragma include \<(?<includename>.+)\>",
			RegexOptions.Compiled);
		//
		private static readonly Dictionary<string, Shader> m_ShaderPool = new Dictionary<string, Shader>();
		/// <summary>
		///
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		///
		/// </summary>
		public readonly string Name;
		/// <summary>
		///
		/// </summary>
		public int Handle
		{
			get;
			private set;
		}
		/// <summary>
		/// The dynamic code.
		/// </summary>
		public readonly IValueProvider<string>[] DynamicCode;		
		/// <summary>
		///
		/// </summary>
		public string Code 
		{
			get; 
			private set;
		}
		/// <summary>
		///
		/// </summary>
		public bool? Compiled
		{
			get;
			private set;
		}
		/// <summary>
		///
		/// </summary>
		public string Log
		{
			get;
			private set;
		}
		/// <summary>
		///
		/// </summary>
		public string ExpandedCode
		{
			get; private set;
		}
		/// <summary>
		///
		/// </summary>
		private Shader (string name, ShaderType type, IValueProvider<string>[] dynamiccode)
		{
			Name = name;
			DynamicCode = dynamiccode;
			Type = type;
			
			foreach (var item in dynamiccode) 
			{
				item.PropertyChanged += (sender, args) => Compiled = null;
			}
			
			Handle = GL.CreateShader (Type);			
			Console.WriteLine ("Shader {0}:{1} declared", Name, Type);
		}
		/// <summary>
		/// Gets the shader.
		/// </summary>
		/// <returns>
		/// The shader.
		/// </returns>
		/// <param name='name'>
		/// Name.
		/// </param>
		/// <param name='code'>
		/// Code.
		/// </param>
		/// <param name='dynamiccode'>
		/// Dynamiccode.
		/// </param>
		public static Shader GetShader(string name, ShaderType type, string code)
		{
			Shader result = null;

			if(!m_ShaderPool.TryGetValue(name, out result))
				m_ShaderPool.Add(name, result =  new Shader(name, type, new []{ValueProvider.Create (code)}));

			return result;
		}
		/// <summary>
		/// Gets the shader.
		/// </summary>
		/// <returns>
		/// The shader.
		/// </returns>
		/// <param name='name'>
		/// Name.
		/// </param>
		/// <param name='type'>
		/// Type.
		/// </param>
		/// <param name='code'>
		/// Code.
		/// </param>
		/// <param name='dynamiccode'>
		/// Dynamiccode.
		/// </param>
		public static Shader GetShader(string name, ShaderType type, params IValueProvider<string>[] dynamiccode)
		{
			Shader result = null;

			if(!m_ShaderPool.TryGetValue(name, out result))
				m_ShaderPool.Add(name, result =  new Shader(name, type, dynamiccode));

			return result;
		}
		/// <summary>
		/// Compile this instance.
		/// </summary>
		public void Compile ()
		{
			foreach (var item in DynamicCode) 
			{
				Code = item.Value;
				ExpandedCode =
					m_IncludeRegex.Replace(Code,
					m =>
					{
						try
						{
							return opentk.ResourcesHelper.GetTexts(m.Groups["includename"].Value, "", System.Text.Encoding.UTF8).Single();
						}
						catch (Exception ex)
						{
							throw new ApplicationException(string.Format("cannot find resource for inclusion: {0}", m.Groups["includename"].Value), ex);
						}
					});
	
				GL.ShaderSource (Handle, ExpandedCode);
				GL.CompileShader (Handle);
				
				int result;
				GL.GetShader (Handle, ShaderParameter.CompileStatus, out result);
				
				var log = GL.GetShaderInfoLog (Handle);
				Compiled = result == 1 && !log.Contains ("error");
				
				if(Compiled.Value)
				break;
				
				Log = log;
			}
		}

		public static ShaderType GetShaderTypeFromName (string name)
		{
			if (name.Contains ("fragment") || name.Contains ("frag"))
				return ShaderType.FragmentShader;
			else if (name.Contains ("vertex") || name.Contains ("vert"))
				return ShaderType.VertexShader;
			else if (name.Contains ("geom") || name.Contains ("geometry"))
				return ShaderType.GeometryShader;
			else if (name.Contains ("comp") || name.Contains ("compute"))
				return (ShaderType) 0x91B9;
			else if (name.Contains ("tese") || name.Contains ("tesseval"))
				return (ShaderType) 0x8E87;
			else if (name.Contains ("tesc") || name.Contains ("tesscont"))
				return (ShaderType) 0x8E88;
			
			throw new ArgumentOutOfRangeException ();
		}


		#region IDisposable implementation
		public void Dispose ()
		{
			m_ShaderPool.Remove(this.Name);
			Compiled = false;
			GL.DeleteShader (Handle);
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public class Pipeline : StatePart, IHandle
	{
		public int Handle
		{
			get;
			private set;
		}

		public Pipeline (params Program[] innerState)
		{
			
		}

		protected override void DisposeCore ()
		{
			
		}
	}
}