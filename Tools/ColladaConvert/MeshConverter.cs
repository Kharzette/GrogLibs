using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MeshLib;

namespace ColladaConvert
{
	//keeps track of original pos index
	public struct TrackedVert
	{
		public Vector3	Position0;
		public Vector3	Normal0;
		public Vector4	BoneIndex;
		public Vector4	BoneWeights;
		public Vector2	TexCoord0;
		public Vector2	TexCoord1;
		public Vector2	TexCoord2;
		public Vector2	TexCoord3;
		public Vector4	Color0;
		public Vector4	Color1;
		public Vector4	Color2;
		public Vector4	Color3;

		public int		mOriginalIndex;


		public static bool operator==(TrackedVert a, TrackedVert b)
		{
			return	(
				(a.BoneIndex == b.BoneIndex) &&
				(a.BoneWeights == b.BoneWeights) &&
				(a.Position0 == b.Position0) &&
				(a.Normal0 == b.Normal0) &&
				(a.TexCoord0 == b.TexCoord0) &&
				(a.TexCoord1 == b.TexCoord1) &&
				(a.TexCoord2 == b.TexCoord2) &&
				(a.TexCoord3 == b.TexCoord3) &&
				(a.Color0 == b.Color0) &&
				(a.Color1 == b.Color1) &&
				(a.Color2 == b.Color2) &&
				(a.Color3 == b.Color3) &&
				(a.mOriginalIndex == b.mOriginalIndex));
		}


		public static bool operator!=(TrackedVert a, TrackedVert b)
		{
			return	(
				(a.BoneIndex != b.BoneIndex) ||
				(a.BoneWeights != b.BoneWeights) ||
				(a.Position0 != b.Position0) ||
				(a.Normal0 != b.Normal0) ||
				(a.TexCoord0 != b.TexCoord0) ||
				(a.TexCoord1 != b.TexCoord1) ||
				(a.TexCoord2 != b.TexCoord2) ||
				(a.TexCoord3 != b.TexCoord3) ||
				(a.Color0 != b.Color0) ||
				(a.Color1 != b.Color1) ||
				(a.Color2 != b.Color2) ||
				(a.Color3 != b.Color3) ||
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

	public class MeshConverter
	{
		string			mName;
		TrackedVert		[]mBaseVerts;
		int				mNumBaseVerts;
		List<ushort>	mIndexList	=new List<ushort>();

		public string	mGeometryID;
		public int		mNumVerts, mNumTriangles, mVertSize;
		public int		mPartIndex;

		//the converted meshes
		Mesh	mConverted;


		public MeshConverter(string name)
		{
			mName	=name;
		}


		public Mesh	GetConvertedMesh()
		{
			return	mConverted;
		}


		public string GetName()
		{
			return	mName;
		}


		//this will build a base list of verts
		//eventually these will need to expand
		public void CreateBaseVerts(float_array verts, bool bSkinned)
		{
			mNumBaseVerts	=(int)verts.count / 3;
			mBaseVerts		=new TrackedVert[mNumBaseVerts];

			for(int i=0;i < (int)verts.count;i+=3)
			{
				mBaseVerts[i / 3].Position0.X		=verts.Values[i];
				mBaseVerts[i / 3].Position0.Y		=verts.Values[i + 1];
				mBaseVerts[i / 3].Position0.Z		=verts.Values[i + 2];
				mBaseVerts[i / 3].mOriginalIndex	=i / 3;
			}

			//create a new gamemesh
			if(bSkinned)
			{
				mConverted	=new SkinnedMesh(mName);
			}
			else
			{
				mConverted	=new StaticMesh(mName);
			}
		}


		//this totally doesn't work at all
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
			for(int i=0;i < verts.Count;i++)
			{
				for(int j=0;j < verts.Count;j++)
				{
					if(i == j)
					{
						continue;
					}
					if(verts[i] == verts[j])
					{
						verts.RemoveAt(j);

						//search through the polygon
						//index list to remove any instances
						//of j and replace them with i
						ReplaceIndex((ushort)j, (ushort)i);
						goto restart;
					}
				}
			}
		}


		public void BakeTransformIntoVerts(Matrix mat)
		{
			for(int i=0;i < mBaseVerts.Length;i++)
			{
				mBaseVerts[i].Position0	=Vector3.Transform(mBaseVerts[i].Position0, mat);
			}
		}


		public void BakeTransformIntoNormals(Matrix mat)
		{
			for(int i=0;i < mBaseVerts.Length;i++)
			{
				mBaseVerts[i].Normal0	=Vector3.TransformNormal(mBaseVerts[i].Normal0, mat);
				mBaseVerts[i].Normal0.Normalize();
			}
		}


		public void FlipNormals()
		{
			for(int i=0;i < mBaseVerts.Length;i++)
			{
				mBaseVerts[i].Normal0	=-mBaseVerts[i].Normal0;
			}
		}


		//fill baseverts with bone indices and weights
		internal void AddWeightsToBaseVerts(skin sk)
		{
			//break out vert weight counts
			List<int>	influenceCounts	=new List<int>();

			string[] tokens	=sk.vertex_weights.vcount.Split(' ','\n');

			//copy vertex weight counts
			foreach(string tok in tokens)
			{
				int numInfluences;

				if(int.TryParse(tok, out numInfluences))
				{
					influenceCounts.Add(numInfluences);
				}
			}

			//copy weight and bone indexes
			List<List<int>>	boneIndexes		=new List<List<int>>();
			List<List<int>>	weightIndexes	=new List<List<int>>();
			tokens	=sk.vertex_weights.v.Split(' ', '\n');

			int			curVert		=0;
			bool		bEven		=true;
			int			numInf		=0;
			List<int>	pvBone		=new List<int>();
			List<int>	pvWeight	=new List<int>();

			//copy float weights
			string	weightKey	="";
			foreach(InputLocalOffset ilo in sk.vertex_weights.input)
			{
				if(ilo.semantic == "WEIGHT")
				{
					weightKey	=ilo.source.Substring(1);
				}
			}
			float_array	weightArray	=null;
			foreach(source src in sk.source)
			{
				if(src.id != weightKey)
				{
					continue;
				}
				weightArray	=src.Item as float_array;
				if(weightArray == null)
				{
					continue;
				}
			}
			
			//copy vertex weight bones
			foreach(string tok in tokens)
			{
				int	val;

				if(int.TryParse(tok, out val))
				{
					if(bEven)
					{
						pvBone.Add(val);
					}
					else
					{
						pvWeight.Add(val);
						numInf++;
					}
					bEven	=!bEven;
					if(numInf >= influenceCounts[curVert])
					{
						boneIndexes.Add(pvBone);
						weightIndexes.Add(pvWeight);
						numInf		=0;
						pvBone		=new List<int>();
						pvWeight	=new List<int>();
						curVert++;
					}
				}
			}


			for(int i=0;i < mNumBaseVerts;i++)
			{
				int	numInfluences	=influenceCounts[i];

				//fix weights over 4
				List<int>	indexes	=new List<int>();
				List<float>	weights	=new List<float>();
				for(int j=0;j < numInfluences;j++)
				{
					//grab bone indices and weights
					int		boneIdx		=boneIndexes[i][j];
					int		weightIdx	=weightIndexes[i][j];
					float	boneWeight	=weightArray.Values[weightIdx];

					indexes.Add(boneIdx);
					weights.Add(boneWeight);
				}

				while(weights.Count > 4)
				{
					//find smallest weight
					float	smallest	=6969.69f;
					int		smIdx		=-1;
					for(int wt=0;wt < weights.Count;wt++)
					{
						if(weights[wt] < smallest)
						{
							smIdx		=wt;
							smallest	=weights[wt];
						}
					}

					//drop smallest weight
					weights.RemoveAt(smIdx);
					indexes.RemoveAt(smIdx);

					//boost other weights by the amount
					//diminished by the loss of the
					//smallest weight
					float	boost	=smallest / weights.Count;

					for(int wt=0;wt < weights.Count;wt++)
					{
						weights[wt]	+=boost;
					}
				}

				for(int j=0;j < numInfluences;j++)
				{
					if(j >= 4)
					{
						//todo: fix weights to add up to 1
						Debug.WriteLine("Too many influences on vertex, tossing influence");
						continue;
					}

					//grab bone indices and weights
					int		boneIdx		=indexes[j];
					float	boneWeight	=weights[j];

					switch(j)
					{
						case	0:
							mBaseVerts[i].BoneIndex.X	=boneIdx;
							mBaseVerts[i].BoneWeights.X	=boneWeight;
							break;
						case	1:
							mBaseVerts[i].BoneIndex.Y	=boneIdx;
							mBaseVerts[i].BoneWeights.Y	=boneWeight;
							break;
						case	2:
							mBaseVerts[i].BoneIndex.Z	=boneIdx;
							mBaseVerts[i].BoneWeights.Z	=boneWeight;
							break;
						case	3:
							mBaseVerts[i].BoneIndex.W	=boneIdx;
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
		internal void AddNormTexByPoly(List<int>		posIdxs,
									   float_array		norms,
									   List<int>		normIdxs,
									   float_array		texCoords0,
									   List<int>		texIdxs0,
									   float_array		texCoords1,
									   List<int>		texIdxs1,
									   float_array		texCoords2,
									   List<int>		texIdxs2,
									   float_array		texCoords3,
									   List<int>		texIdxs3,
									   float_array		colors0,
									   List<int>		colIdxs0,
									   float_array		colors1,
									   List<int>		colIdxs1,
									   float_array		colors2,
									   List<int>		colIdxs2,
									   float_array		colors3,
									   List<int>		colIdxs3,
									   List<int>		vertCounts)
		{
			//make sure there are at least positions and vertCounts
			if(posIdxs == null || vertCounts == null)
			{
				return;
			}

			List<TrackedVert>	verts	=new List<TrackedVert>();

			//track the polygon in use
			int	polyIndex	=0;
			int	curVert		=0;
			int	vCnt		=vertCounts[polyIndex];
			for(int i=0;i < posIdxs.Count;i++)
			{
				int	pidx, nidx;
				int	tidx0, tidx1, tidx2, tidx3;
				int	cidx0, cidx1, cidx2, cidx3;

				pidx	=posIdxs[i];
				nidx	=0;
				tidx0	=tidx1	=tidx2	=tidx3	=0;
				cidx0	=cidx1	=cidx2	=cidx3	=0;

				if(normIdxs != null && norms != null)
				{
					nidx	=normIdxs[i];
				}
				if(texIdxs0 != null && texCoords0 != null)
				{
					tidx0	=texIdxs0[i];
				}
				if(texIdxs1 != null && texCoords1 != null)
				{
					tidx1	=texIdxs1[i];
				}
				if(texIdxs2 != null && texCoords2 != null)
				{
					tidx2	=texIdxs2[i];
				}
				if(texIdxs3 != null && texCoords3 != null)
				{
					tidx3	=texIdxs3[i];
				}
				if(colIdxs0 != null && colors0 != null)
				{
					cidx0	=colIdxs0[i];
				}
				if(colIdxs1 != null && colors1 != null)
				{
					cidx1	=colIdxs1[i];
				}
				if(colIdxs2 != null && colors2 != null)
				{
					cidx2	=colIdxs2[i];
				}
				if(colIdxs3 != null && colors3 != null)
				{
					cidx3	=colIdxs3[i];
				}

				TrackedVert	tv	=new TrackedVert();

				//copy the basevertex, this will ensure we
				//get the right position and bone indexes
				//and vertex weights
				tv	=mBaseVerts[pidx];

				//copy normal if exists
				if(normIdxs != null && norms != null)
				{
					tv.Normal0.X	=norms.Values[nidx * 3];
					tv.Normal0.Y	=norms.Values[1 + nidx * 3];
					tv.Normal0.Z	=norms.Values[2 + nidx * 3];
				}
				//copy texcoords
				if(texIdxs0 != null && texCoords0 != null)
				{
					tv.TexCoord0.X	=texCoords0.Values[tidx0 * 2];
					tv.TexCoord0.Y	=-texCoords0.Values[1 + tidx0 * 2];
				}
				if(texIdxs1 != null && texCoords1 != null)
				{
					tv.TexCoord1.X	=texCoords1.Values[tidx1 * 2];
					tv.TexCoord1.Y	=-texCoords1.Values[1 + tidx1 * 2];
				}
				if(texIdxs2 != null && texCoords2 != null)
				{
					tv.TexCoord2.X	=texCoords2.Values[tidx2 * 2];
					tv.TexCoord2.Y	=-texCoords2.Values[1 + tidx2 * 2];
				}
				if(texIdxs3 != null && texCoords3 != null)
				{
					tv.TexCoord3.X	=texCoords3.Values[tidx3 * 2];
					tv.TexCoord3.Y	=-texCoords3.Values[1 + tidx3 * 2];
				}
				if(colIdxs0 != null && colors0 != null)
				{
					tv.Color0.X	=colors0.Values[cidx0 * 4];
					tv.Color0.Y	=colors0.Values[1 + cidx0 * 4];
					tv.Color0.Z	=colors0.Values[2 + cidx0 * 4];
					tv.Color0.W	=colors0.Values[3 + cidx0 * 4];
				}
				if(colIdxs1 != null && colors1 != null)
				{
					tv.Color1.X	=colors1.Values[cidx1 * 4];
					tv.Color1.Y	=colors1.Values[1 + cidx1 * 4];
					tv.Color1.Z	=colors1.Values[2 + cidx1 * 4];
					tv.Color1.W	=colors1.Values[3 + cidx1 * 4];
				}
				if(colIdxs2 != null && colors2 != null)
				{
					tv.Color2.X	=colors2.Values[cidx2 * 4];
					tv.Color2.Y	=colors2.Values[1 + cidx2 * 4];
					tv.Color2.Z	=colors2.Values[2 + cidx2 * 4];
					tv.Color2.W	=colors2.Values[3 + cidx2 * 4];
				}
				if(colIdxs3 != null && colors3 != null)
				{
					tv.Color3.X	=colors3.Values[cidx3 * 4];
					tv.Color3.Y	=colors3.Values[1 + cidx3 * 4];
					tv.Color3.Z	=colors3.Values[2 + cidx3 * 4];
					tv.Color3.W	=colors3.Values[3 + cidx3 * 4];
				}

				verts.Add(tv);
				mIndexList.Add((ushort)(verts.Count - 1));
				curVert++;

				if(curVert >= vCnt)
				{
					polyIndex++;
					if(polyIndex >= vertCounts.Count)
					{
						break;
					}
					vCnt	=vertCounts[polyIndex];
					curVert	=0;
				}
			}

			//dump verts back into baseverts
			mBaseVerts		=new TrackedVert[verts.Count];
			mNumBaseVerts	=verts.Count;
			for(int i=0;i < verts.Count;i++)
			{
				mBaseVerts[i]	=verts[i];
			}
			//EliminateDuplicateVerts();

			Triangulate(vertCounts);

			mNumVerts		=verts.Count;
			mNumTriangles	=mIndexList.Count / 3;
		}


		public void SetGeometryID(string id)
		{
			mGeometryID	=id;
		}


		void ReplaceIndex(ushort find, ushort replace)
		{
			for(int i=0;i < mIndexList.Count;i++)
			{
				if(mIndexList[i] == find)
				{
					mIndexList[i]	=replace;
				}
			}
		}


		void Triangulate(List<int> vertCounts)
		{
			List<ushort>	newIdxs	=new List<ushort>();

			int	curIdx	=0;
			for(int i=0;i < vertCounts.Count;i++)
			{
				//see how many verts in this polygon
				int	vCount	=vertCounts[i];

				for(int j=1;j < (vCount - 1);j++)
				{
					newIdxs.Add(mIndexList[curIdx]);
					newIdxs.Add(mIndexList[j + curIdx]);
					newIdxs.Add(mIndexList[j + 1 + curIdx]);
				}
				curIdx	+=vCount;
			}

			//dump back into regular list
			mIndexList.Clear();
			for(int i=newIdxs.Count - 1;i >= 0;i--)
			{
				mIndexList.Add(newIdxs[i]);
			}
		}


		VertexDeclaration DetermineVertexDeclaration(
			bool bPositions, bool bNormals, bool bBoneIndices,
			bool bBoneWeights, bool bTexCoord0, bool bTexCoord1,
			bool bTexCoord2, bool bTexCoord3, bool bColor0,
			bool bColor1, bool bColor2, bool bColor3)
		{
			//don't really see any need to continue if there
			//are no positions?
			if(!bPositions)
			{
				return	null;
			}

			//count up the number of vertex elements needed
			int		numElements	=0;
			short	offset		=0;
			int		index		=0;
			if(bPositions)		numElements++;
			if(bNormals)		numElements++;
			if(bBoneIndices)	numElements++;
			if(bBoneWeights)	numElements++;
			if(bTexCoord0)		numElements++;
			if(bTexCoord1)		numElements++;
			if(bTexCoord2)		numElements++;
			if(bTexCoord3)		numElements++;
			if(bColor0)			numElements++;
			if(bColor1)			numElements++;
			if(bColor2)			numElements++;
			if(bColor3)			numElements++;

			VertexElement[] ve	=new VertexElement[numElements];

			if(bPositions)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector3,
					VertexElementUsage.Position, 0);
				index++;
				offset	+=12;
			}
			if(bNormals)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector3,
					VertexElementUsage.Normal, 0);
				index++;
				offset	+=12;
			}
			if(bBoneIndices)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector4,
					VertexElementUsage.BlendIndices, 0);
				index++;
				offset	+=16;
			}
			if(bBoneWeights)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector4,
					VertexElementUsage.BlendWeight, 0);
				index++;
				offset	+=16;
			}
			if(bTexCoord0)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector2,
					VertexElementUsage.TextureCoordinate, 0);
				index++;
				offset	+=8;
			}
			if(bTexCoord1)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector2,
					VertexElementUsage.TextureCoordinate, 1);
				index++;
				offset	+=8;
			}
			if(bTexCoord2)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector2,
					VertexElementUsage.TextureCoordinate, 2);
				index++;
				offset	+=8;
			}
			if(bTexCoord3)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector2,
					VertexElementUsage.TextureCoordinate, 3);
				index++;
				offset	+=8;
			}
			if(bColor0)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector4,
					VertexElementUsage.Color, 0);
				index++;
				offset	+=16;
			}
			if(bColor1)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector4,
					VertexElementUsage.Color, 1);
				index++;
				offset	+=16;
			}
			if(bColor2)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector4,
					VertexElementUsage.Color, 2);
				index++;
				offset	+=16;
			}
			if(bColor3)
			{
				ve[index]	=new VertexElement(offset, VertexElementFormat.Vector4,
					VertexElementUsage.Color, 3);
				index++;
				offset	+=16;
			}

			VertexDeclaration	ret	=new VertexDeclaration(ve);

			mConverted.SetVertexDeclaration(ret);

			return	ret;
		}


		//take the munged data and stuff it into
		//the vertex and index buffers
		public void BuildBuffers(GraphicsDevice gd,
			bool bPositions, bool bNormals, bool bBoneIndices,
			bool bBoneWeights, bool bTexCoord0, bool bTexCoord1,
			bool bTexCoord2, bool bTexCoord3, bool bColor0,
			bool bColor1, bool bColor2, bool bColor3)
		{
			//build vertex decl
			VertexDeclaration	dec	=DetermineVertexDeclaration(bPositions,
				bNormals, bBoneIndices, bBoneWeights,
				bTexCoord0, bTexCoord1, bTexCoord2,
				bTexCoord3, bColor0, bColor1,
				bColor2, bColor3);

			int	numTex		=0;
			int	numColor	=0;

			if(bTexCoord0)	numTex++;
			if(bTexCoord1)	numTex++;
			if(bTexCoord2)	numTex++;
			if(bTexCoord3)	numTex++;
			if(bColor0)		numColor++;
			if(bColor1)		numColor++;
			if(bColor2)		numColor++;
			if(bColor3)		numColor++;
			Type vtype	=VertexTypes.GetMatch(bPositions, bNormals, bBoneIndices, bBoneWeights, numTex, numColor);

			Array	verts	=Array.CreateInstance(vtype, mNumBaseVerts);

			for(int i=0;i < mNumBaseVerts;i++)
			{
				if(bPositions)
				{
					VertexTypes.SetArrayField(verts, i, "Position", mBaseVerts[i].Position0);
				}
				if(bNormals)
				{
					VertexTypes.SetArrayField(verts, i, "Normal", mBaseVerts[i].Normal0);
				}
				if(bBoneIndices)
				{
					VertexTypes.SetArrayField(verts, i, "BoneIndex", mBaseVerts[i].BoneIndex);
				}
				if(bBoneWeights)
				{
					VertexTypes.SetArrayField(verts, i, "BoneWeights", mBaseVerts[i].BoneWeights);
				}
				if(bTexCoord0)
				{
					VertexTypes.SetArrayField(verts, i, "TexCoord0", mBaseVerts[i].TexCoord0);
				}
				if(bTexCoord1)
				{
					VertexTypes.SetArrayField(verts, i, "TexCoord1", mBaseVerts[i].TexCoord1);
				}
				if(bTexCoord2)
				{
					VertexTypes.SetArrayField(verts, i, "TexCoord2", mBaseVerts[i].TexCoord2);
				}
				if(bTexCoord3)
				{
					VertexTypes.SetArrayField(verts, i, "TexCoord3", mBaseVerts[i].TexCoord3);
				}
				if(bColor0)
				{
					VertexTypes.SetArrayField(verts, i, "Color0", mBaseVerts[i].Color0);
				}
				if(bColor1)
				{
					VertexTypes.SetArrayField(verts, i, "Color1", mBaseVerts[i].Color1);
				}
				if(bColor2)
				{
					VertexTypes.SetArrayField(verts, i, "Color2", mBaseVerts[i].Color2);
				}
				if(bColor3)
				{
					VertexTypes.SetArrayField(verts, i, "Color3", mBaseVerts[i].Color3);
				}
			}

			mConverted.SetVertSize(VertexTypes.GetSizeForType(vtype));
			mConverted.SetNumVerts(mNumBaseVerts);
			mConverted.SetNumTriangles(mNumTriangles);
			mConverted.SetTypeIndex(VertexTypes.GetIndex(vtype));

			//set bufferusage here so that getdata can be called
			//we'll need it to save the mesh to a file
			VertexBuffer vb	=new VertexBuffer(gd, dec, mNumBaseVerts, BufferUsage.None);


			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "SetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			typedMethod.Invoke(vb, new object[] {verts});

			mConverted.SetVertexBuffer(vb);

			ushort	[]idxs	=new ushort[mIndexList.Count];

			for(int i=0;i < mIndexList.Count;i++)
			{
				idxs[i]	=mIndexList[i];
			}

			//set bufferusage here so that getdata can be called
			//we'll need it to save the mesh to a file
			IndexBuffer	indbuf	=new IndexBuffer(gd,
						IndexElementSize.SixteenBits,
						mIndexList.Count,
						BufferUsage.None);

			indbuf.SetData<ushort>(idxs);

			mConverted.SetIndexBuffer(indbuf);
		}
	}
}