using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BSPLib
{
	//grind up a map into gpu friendly data
	public class MapGrinder
	{
		GraphicsDevice	mGD;

		List<Vector3>	mFaceVerts	=new List<Vector3>();
		List<Vector2>	mFaceTex0	=new List<Vector2>();
		List<Vector2>	mFaceTex1	=new List<Vector2>();
		List<Int32>		mIndexes	=new List<Int32>();


		public MapGrinder(GraphicsDevice gd)
		{
			mGD	=gd;
		}


		internal Int32 GetNumVerts()
		{
			return	mFaceVerts.Count;
		}


		internal Int32 GetNumTris()
		{
			return	mIndexes.Count / 3;
		}


		internal void GetBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			VPosTex0Tex1	[]solidVArray	=new VPosTex0Tex1[mFaceVerts.Count];
			for(int i=0;i < mFaceVerts.Count;i++)
			{
				solidVArray[i].Position		=mFaceVerts[i];
				solidVArray[i].TexCoord0	=mFaceTex0[i];
				solidVArray[i].TexCoord1	=mFaceTex0[i];	//duping texcoord0!
			}

			vb	=new VertexBuffer(mGD, 28 * solidVArray.Length, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1>(solidVArray);

			ib	=new IndexBuffer(mGD, 4 * mIndexes.Count, BufferUsage.WriteOnly,
					IndexElementSize.ThirtyTwoBits);
			ib.SetData<Int32>(mIndexes.ToArray());
		}


		internal void BuildFaceData(Vector3 []verts, int[] indexes,
			GFXTexInfo []texInfos, GFXFace []faces)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();

			foreach(GFXFace f in faces)
			{
				GFXTexInfo	tex	=texInfos[f.mTexInfo];

				List<Vector2>	coords	=new List<Vector2>();

				int		nverts	=f.mNumVerts;
				int		fvert	=f.mFirstVert;
				int		k		=0;
				for(k=0;k < nverts;k++)
				{
					int		idx	=indexes[fvert + k];
					Vector3	pnt	=verts[idx];
					Vector2	crd;
					crd.X	=Vector3.Dot(tex.mVecs[0], pnt);
					crd.Y	=Vector3.Dot(tex.mVecs[1], pnt);

					coords.Add(crd);

					mFaceVerts.Add(pnt);
				}

				Bounds	bnd	=new Bounds();
				foreach(Vector2 crd in coords)
				{
					bnd.AddPointToBounds(crd);
				}

				for(k=0;k < nverts;k++)
				{
					int	idx	=indexes[fvert + k];

					Vector2	tc	=Vector2.Zero;
					tc.X	=coords[k].X - bnd.mMins.X;
					tc.Y	=coords[k].Y - bnd.mMins.Y;
					mFaceTex0.Add(tc);

					//tex1 here for now
					mFaceTex1.Add(tc);
				}
				firstVert.Add(mFaceVerts.Count - f.mNumVerts);
				numVert.Add(f.mNumVerts);
			}

			for(int i=0;i < numVert.Count;i++)
			{
				int		nverts	=numVert[i];
				int		fvert	=firstVert[i];
				int		k		=0;

				//triangulate
				for(k=1;k < nverts-1;k++)
				{
					mIndexes.Add(fvert);
					mIndexes.Add(fvert + k);
					mIndexes.Add(fvert + ((k + 1) % nverts));
				}
			}
		}
	}
}
