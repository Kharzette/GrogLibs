using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX;


namespace MaterialLib
{
	public class AlphaPool
	{
		//render shadowing objects
		public delegate void RenderShadows(int shadIndex);

		List<AlphaNode>	mAlphas	=new List<AlphaNode>();


		public void StoreDraw(Vector3 sortPoint, Material matRef,
			VertexBuffer vb, IndexBuffer ib, Matrix worldMat,
			Int32 baseVert, Int32 minVertIndex,
			Int32 numVerts, Int32 startIndex, Int32 primCount)
		{
			AlphaNode	an	=new AlphaNode(sortPoint, matRef,
				vb, ib, worldMat, baseVert, minVertIndex,
				numVerts, startIndex, primCount);

			mAlphas.Add(an);
		}


		public void StoreParticleDraw(Vector3 sortPoint,
			VertexBuffer vb, Int32 primCount,
			bool bCel, Vector4 color,
			Effect fx, Texture2D tex,
			Matrix view, Matrix proj)
		{
			AlphaNode	an	=new AlphaNode(sortPoint, vb,
				primCount, bCel, color, fx, tex, view, proj);

			mAlphas.Add(an);
		}


		public void DrawAll(GraphicsDevice g, MaterialLib mlib, Vector3 eyePos,
			int numShadows, RenderShadows rendShad)
		{
			Sort(eyePos);

			foreach(AlphaNode an in mAlphas)
			{
				an.Draw(g, mlib, numShadows, rendShad);
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
