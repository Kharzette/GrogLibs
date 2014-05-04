using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MaterialLib
{
	public class AlphaPool
	{
		//render shadowing objects
		public delegate void RenderShadows(int shadIndex);

		List<AlphaNode>	mAlphas	=new List<AlphaNode>();


		public void StoreDraw(MatLib mlib, Vector3 sortPoint, string matName,
			VertexBufferBinding vbb, Buffer ib, Matrix worldMat, Int32 indexCount)
		{
			AlphaNode	an	=new AlphaNode(mlib, sortPoint, matName,
				vbb, ib, worldMat, indexCount);

			mAlphas.Add(an);
		}


		public void StoreParticleDraw(MatLib mlib,
			Vector3 sortPoint,
			VertexBufferBinding vbb, Int32 vertCount,
			Vector4 color, ShaderResourceView tex,
			Matrix view, Matrix proj)
		{
			AlphaNode	an	=new AlphaNode(mlib, sortPoint, vbb,
				vertCount, color, tex, view, proj);

			mAlphas.Add(an);
		}


		public void DrawAll(GraphicsDevice gd, Vector3 eyePos,
			int numShadows, RenderShadows rendShad)
		{
			Sort(eyePos);

			foreach(AlphaNode an in mAlphas)
			{
				an.Draw(gd);
			}

			//clear nodes when done
			mAlphas.Clear();
		}


		void Sort(Vector3 eyePos)
		{
			AlphaNodeComparer	anc	=new AlphaNodeComparer(eyePos);

			mAlphas.Sort(anc);
		}
	}
}
