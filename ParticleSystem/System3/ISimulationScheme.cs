using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace opentk.System3
{
	/// <summary>
	///
	/// </summary>
	[InheritedExport]
	public interface ISimulationScheme
	{
		string Name
		{
			get;
		}

		void Simulate(System3 system, DateTime simulationTime, long simulationStep);
	}
}


