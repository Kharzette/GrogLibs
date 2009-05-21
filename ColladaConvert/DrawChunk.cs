using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	//keeps track of original pos index
	public struct TrackedVert
	{
		public Vector3	Position0;
		public Vector3	Normal0;
		public Vector4	BoneIndex0;
		public Vector4	BoneWeights;
		public Vector2	TexCoord0;
		public int		mOriginalIndex;


		public static bool operator==(TrackedVert a, TrackedVert b)
		{
			return	(
				(a.BoneIndex0 == b.BoneIndex0) &&
				(a.BoneWeights == b.BoneWeights) &&
				(a.Position0 == b.Position0) &&
				(a.Normal0 == b.Normal0) &&
				(a.TexCoord0 == b.TexCoord0) &&
				(a.mOriginalIndex == b.mOriginalIndex));
		}


		public static bool operator!=(TrackedVert a, TrackedVert b)
		{
			return	(
				(a.BoneIndex0 != b.BoneIndex0) ||
				(a.BoneWeights != b.BoneWeights) ||
				(a.Position0 != b.Position0) ||
				(a.Normal0 != b.Normal0) ||
				(a.TexCoord0 != b.TexCoord0) ||
				(a.mOriginalIndex != b.mOriginalIndex));
		}


		public override bool Equals(object obj)
		{
			return	base.Equals(obj);
		}


		public override int GetHashCode()
		{
			return	base.GetHashCode();
		}
	}

	public class DrawChunk
	{
		public VertexBuffer			mVerts;
		public IndexBuffer			mIndexs;
		public VertexBuffer			mSkinData;
		public VertexDeclaration	mVD;
		public Matrix				[]mBones;
		public int					mNumVerts, mNumTriangles, mVertSize, mTexChannel;
		public string				mTexName;
		public string				mSkinName;
		public string				mGeometryID;	//for mapping geoms to skins

		private TrackedVert		[]mBaseVerts;
		private	int				mNumBaseVerts;


		//this will build a base list of verts
		//eventually these will need to expand
		public void CreateBaseVerts(List<float> verts)
		{
			mNumBaseVerts	=verts.Count / 3;
			mBaseVerts		=new TrackedVert[mNumBaseVerts];

			for(int i=0;i < verts.Count;i+=3)
			{
				mBaseVerts[i / 3].Position0.X		=verts[i];
				mBaseVerts[i / 3].Position0.Y		=verts[i + 1];
				mBaseVerts[i / 3].Position0.Z		=verts[i + 2];
				mBaseVerts[i / 3].mOriginalIndex	=i / 3;
			}
		}


		public void EliminateDuplicateVerts()
		{
			//throw these in a list to make it easier
			//to throw some out
			List<TrackedVert>	verts	=new List<TrackedVert>();
			for(int i=0;i < mNumBaseVerts;i++)
			{
				verts.Add(mBaseVerts[i]);
			}

			restart:
			for(int i=0;i < mNumBaseVerts;i++)
			{
				for(int j=0;j < mNumBaseVerts;j++)
				{
					if(verts[i] == verts[j])
					{
						verts.RemoveAt(j);
						goto restart;
					}
				}
			}
		}


		//fill baseverts with bone indices and weights
		public void AddWeightsToBaseVerts(Skin sk)
		{
			for(int i=0;i < mNumBaseVerts;i++)
			{
				int	numInf	=sk.GetNumInfluencesForVertIndex(i);

				for(int j=0;j < numInf;j++)
				{
					if(j >= 4)
					{
						Debug.WriteLine("Too many influences on vertex, tossing influence");
						continue;
					}

					//grab bone indices and weights
					int		boneIdx		=sk.GetBoneIndexForVertIndex(i, j);
					float	boneWeight	=sk.GetBoneWeightForVertIndex(i, j);

					switch(j)
					{
						case	0:
							mBaseVerts[i].BoneIndex0.X	=boneIdx;
							mBaseVerts[i].BoneWeights.X	=boneWeight;
							break;
						case	1:
							mBaseVerts[i].BoneIndex0.Y	=boneIdx;
							mBaseVerts[i].BoneWeights.Y	=boneWeight;
							break;
						case	2:
							mBaseVerts[i].BoneIndex0.Z	=boneIdx;
							mBaseVerts[i].BoneWeights.Z	=boneWeight;
							break;
						case	3:
							mBaseVerts[i].BoneIndex0.W	=boneIdx;
							mBaseVerts[i].BoneWeights.W	=boneWeight;
							break;
					}
				}
			}
		}


		//this copies all pertinent per polygon information
		//into the trackedverts.  Every vert indexed by a
		//polygon will be duplicated as the normals and
		//texcoords can vary on a particular position in a mesh
		//depending on which polygon is being drawn.
		//This also constructs a list of indices
		public void AddNormTexByPoly(List<int>		posIdxs,
									List<float>		norms,
									List<int>		normIdxs,
									List<float>		texCoords,
									List<int>		texIdxs,
									List<int>		vertCounts)
		{
			List<TrackedVert>	verts	=new List<TrackedVert>();

			Debug.Assert(posIdxs.Count == normIdxs.Count && posIdxs.Count == texIdxs.Count);

			//track the polygon in use
			int	polyIndex	=0;
			int	curVert		=0;
			for(int i=0;i < posIdxs.Count;i++)
			{
				int	pidx	=posIdxs[i];
				int	nidx	=normIdxs[i];
				int	tidx	=texIdxs[i];

				TrackedVert	tv	=new TrackedVert();
				
				//copy the basevertex, this will ensure we
				//get the right position and bone indexes
				//and vertex weights
				tv	=mBaseVerts[pidx];

				//copy normal
				tv.Normal0.X	=norms[nidx * 3];
				tv.Normal0.Y	=norms[1 + nidx * 3];
				tv.Normal0.Z	=norms[2 + nidx * 3];

				//copy texcoords
				tv.TexCoord0.X	=texIdxs[tidx * 2];
				tv.TexCoord0.Y	=texIdxs[1 + tidx * 2];

				verts.Add(tv);


			}
		}


		public void SetGeometryID(string id)
		{
			mGeometryID	=id;
		}
	}
}