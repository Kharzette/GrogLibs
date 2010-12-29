using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	public class AlphaPool
	{
		List<AlphaNode>	mAlphas	=new List<AlphaNode>();


		public void StoreDraw(Vector3 sortPoint, Material matRef,
			VertexBuffer vb, IndexBuffer ib, VertexDeclaration vd,
			Int32 baseVert, Int32 minVertIndex,
			Int32 numVerts, Int32 startIndex, Int32 primCount)
		{
			AlphaNode	an	=new AlphaNode(sortPoint, matRef,
				vb, ib, vd, baseVert, minVertIndex,
				numVerts, startIndex, primCount);

			mAlphas.Add(an);
		}


		public void DrawAll(GraphicsDevice g, MaterialLib mlib, Vector3 eyePos)
		{
			Sort(eyePos);

			foreach(AlphaNode an in mAlphas)
			{
				an.Draw(g, mlib);
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
