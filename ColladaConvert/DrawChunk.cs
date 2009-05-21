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


		//this will grow the list of baseverts
		//as polygons all share verts, but sometimes
		//the normals for a particular face are different
		public void AddNormalsToBaseVerts(List<int> posIdxs,
											List<float> norms,
											List<int> normIdxs)
		{
			//make a list for extra verts
			//these will be merged into the array later
			List<TrackedVert>	verts	=new List<TrackedVert>();

			//buzz through the polygons checking for
			//normals that are different on the same vert
			//thus causing a new vert to be created
			for(int i=0;i < posIdxs.Count;i++)
			{
				int	posIdx	=posIdxs[i];
				int	nrmIdx	=normIdxs[i];

				//first see if a normal exists at all
				//for this vertex
				bool	bFound	=false;
				if(mBaseVerts[posIdx].Normal0.LengthSquared() == 0)
				{
					//no normal was here before
					mBaseVerts[posIdx].Normal0.X	=norms[nrmIdx * 3];
					mBaseVerts[posIdx].Normal0.Y	=norms[1 + nrmIdx * 3];
					mBaseVerts[posIdx].Normal0.Z	=norms[2 + nrmIdx * 3];
					bFound							=true;
				}

				if(!bFound)
				{
					//check through the to be added list
					foreach(TrackedVert tv in verts)
					{
						if(tv.mOriginalIndex == posIdx)
						{
							//duplicate vert here, check the normal
							if(tv.Normal0.X	== norms[nrmIdx * 3]
								&& tv.Normal0.Y == norms[1 + nrmIdx * 3]
								&& tv.Normal0.Z == norms[2 + nrmIdx * 3])
							{
								bFound	=true;
								break;
							}
						}
					}
				}

				if(!bFound)
				{
					//if it still hasn't been found, add
					//a new vertex to the to be added list
					TrackedVert	trv	=new TrackedVert();

					//these are structs, so the data will
					//be copied, instead of a ref made
					trv				=mBaseVerts[posIdx];
					trv.Normal0.X	=norms[nrmIdx * 3];
					trv.Normal0.Y	=norms[1 + nrmIdx * 3];
					trv.Normal0.Z	=norms[2 + nrmIdx * 3];

					verts.Add(trv);
				}
			}

			//add all the original verts to the tobeaddedlist
			for(int i=0;i < mNumBaseVerts;i++)
			{
				verts.Add(mBaseVerts[i]);
			}

			//nuke and recreate baseverts
			mNumBaseVerts	=verts.Count;
			mBaseVerts		=new TrackedVert[mNumBaseVerts];

			//copy em all back in
			for(int i=0;i < mNumBaseVerts;i++)
			{
				mBaseVerts[i]	=verts[i];
			}
		}


		public void SetGeometryID(string id)
		{
			mGeometryID	=id;
		}
	}
}