﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using MaterialLib;
using UtilityLib;
using MeshLib;

using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;



namespace TerrainLib
{
	public class HeightMap
	{
		public class TexData
		{
			float	mBottomElevation;
			float	mTopElevation;
			bool	mbSteep;
			float	mScaleFactor;
			string	mTextureName;

			//uv adjustments from TexAtlas
			public double	mScaleU, mScaleV;
			public double	mUOffs, mVOffs;


			public TexData() { }
			public TexData(TexData copyMe)
			{
				mBottomElevation	=copyMe.mBottomElevation;
				mTopElevation		=copyMe.mTopElevation;
				mbSteep				=copyMe.mbSteep;
				mScaleFactor		=copyMe.mScaleFactor;
				mTextureName		=copyMe.mTextureName;
				mScaleU				=copyMe.mScaleU;
				mScaleV				=copyMe.mScaleV;
				mUOffs				=copyMe.mUOffs;
				mVOffs				=copyMe.mVOffs;
			}

			public float	BottomElevation
			{
				get {	return	mBottomElevation;	}
				set {	mBottomElevation	=value;	}
			}

			public float	TopElevation
			{
				get {	return	mTopElevation;	}
				set	{	mTopElevation	=value; }
			}

			public float	ScaleFactor
			{
				get {	return mScaleFactor;	}
				set	{	mScaleFactor	=value;	}
			}

			public bool	Steep
			{
				get {	return mbSteep;	}
				set {	mbSteep	=value;	}
			}

			public string	TextureName
			{
				get {	return mTextureName;	}
				set {	mTextureName	=value;	}
			}


			internal void Write(BinaryWriter bw)
			{
				bw.Write(mBottomElevation);
				bw.Write(mTopElevation);
				bw.Write(mbSteep);
				bw.Write(mScaleFactor);
				bw.Write(mTextureName);

				bw.Write(mScaleU);
				bw.Write(mScaleV);
				bw.Write(mUOffs);
				bw.Write(mVOffs);
			}

			internal void Read(BinaryReader br)
			{
				mBottomElevation	=br.ReadSingle();
				mTopElevation		=br.ReadSingle();
				mbSteep				=br.ReadBoolean();
				mScaleFactor		=br.ReadSingle();
				mTextureName		=br.ReadString();

				mScaleU	=br.ReadDouble();
				mScaleV	=br.ReadDouble();
				mUOffs	=br.ReadDouble();
				mVOffs	=br.ReadDouble();
			}
		}

		Buffer				mVBTerrain;
		VertexBufferBinding	mVBB;

		int		mNumVerts, mNumTris;

		//location stuff
		Vector3	mPosition;
		Matrix	mMat	=Matrix.Identity;
		float	mPeak;		//max height
		float	mValley;	//min height
		Point	mCellCoordinate;
		bool	mbReady	=false;

		//bounds for frust rejection
		BoundingBox	mBounds, mCellBounds;

		//timings
		long	mPosTime, mTexFactTime, mIndexTime, mBufferTime;

		const float	SteepnessThreshold	=0.7f;


		//2D float array
		public HeightMap(float			[,]data,
						 Vector3		[,]norms,
						 Point			coord,
						 int			chunkDim,
						 int			w,
						 int			h,
						 int			actualWidth,
						 int			actualHeight,
						 float			polySize,
						 float			transHeight,
						 List<TexData>	texInfo,
						 GraphicsDevice	gd)
		{
			mCellCoordinate	=coord;

			//fed in are usually a nice power of two
			//however an extra edge of verts are needed to fill
			//in the gaps between
			actualWidth++;	actualHeight++;

			mNumVerts	=actualWidth * actualHeight;
			mNumTris	=((actualWidth - 1) * (actualHeight - 1)) * 2;

			//alloc some space for verts and indexs
			//verts need pos, norm, 4 scalars for tex percentage,
			//and 4 intish lookups into tex table (col0)
			Type	vType	=typeof(VPosNormTex04Col0);
			Array	varray	=Array.CreateInstance(vType, actualWidth * actualHeight);

			Stopwatch	sw	=new Stopwatch();
			sw.Start();

			bool	bUToggle	=false;
			bool	bVToggle	=false;
			
			//load the height map
			int	startY	=(chunkDim * coord.Y);
			int	startX	=(chunkDim * coord.X);
			int	endY	=(chunkDim * (coord.Y + 1)) + 1;
			int	endX	=(chunkDim * (coord.X + 1)) + 1;

			Vector3	min	=Vector3.One * float.MaxValue;
			Vector3	max	=Vector3.One * float.MinValue;
			for(int y=startY;y < endY;y++)
			{
				float	vCoord	=(bVToggle)? 1f : 0f;

				for(int x=startX;x < endX;x++)
				{
					float	uCoord	=(bUToggle)? 1f : 0f;

					Vector3	pos	=Vector3.Zero;
					int		dex	=x + (y * w);

					pos.X	=(float)(x - startX);
					pos.Z	=(float)(y - startY);
					pos.Y	=data[y, x];

					pos.X	*=polySize;
					pos.Z	*=polySize;

					Vector3	v3Norm	=norms[y, x];

					Half4	norm;
					norm.X	=v3Norm.X;
					norm.Y	=v3Norm.Y;
					norm.Z	=v3Norm.Z;
					norm.W	=0f;

					int	setDex	=(x - startX) + ((y - startY) * actualWidth);

					VertexTypes.SetArrayField(varray, setDex, "Position", pos);
					VertexTypes.SetArrayField(varray, setDex, "TexCoord0",
						new Half4(uCoord, vCoord, 0f, 0f));
					VertexTypes.SetArrayField(varray, setDex, "Normal", norm);

					bUToggle	=!bUToggle;

					//find bounds
					if(pos.X < min.X)
					{
						min.X	=pos.X;
					}
					if(pos.X > max.X)
					{
						max.X	=pos.X;
					}
					if(pos.Y < min.Y)
					{
						min.Y	=pos.Y;
					}
					if(pos.Y > max.Y)
					{
						max.Y	=pos.Y;
					}
					if(pos.Z < min.Z)
					{
						min.Z	=pos.Z;
					}
					if(pos.Z > max.Z)
					{
						max.Z	=pos.Z;
					}
				}

				bVToggle	=!bVToggle;
				bUToggle	=!bUToggle;
			}
			sw.Stop();

			mPosTime	=sw.ElapsedTicks;

			mPeak	=max.Y;
			mValley	=min.Y;

			mBounds.Maximum	=max;
			mBounds.Minimum	=min;

			sw.Reset();
			sw.Start();
			Array	triListVerts	=BreakIntoTriList(varray, actualWidth, actualHeight);
			sw.Stop();
			mIndexTime	=sw.ElapsedTicks;

			mNumVerts	=mNumTris * 3;

			sw.Reset();
			sw.Start();
			SetTextureFactors(triListVerts, texInfo, actualWidth, actualHeight, transHeight);
			sw.Stop();
			mTexFactTime	=sw.ElapsedTicks;

			sw.Reset();
			sw.Start();
			mVBTerrain	=VertexTypes.BuildABuffer(gd.GD, triListVerts, vType);
			mVBB		=new VertexBufferBinding(mVBTerrain, VertexTypes.GetSizeForType(vType), 0);
			sw.Stop();
			mBufferTime	=sw.ElapsedTicks;
		}


		public void FreeAll()
		{
			if(mVBTerrain != null)
			{
				mVBTerrain.Dispose();
				mVBTerrain	=null;
			}
		}


		internal void GetTimings(out long pos, out long texFact,
			out long index, out long buffer)
		{
			pos		=mPosTime;
			texFact	=mTexFactTime;
			index	=mIndexTime;
			buffer	=mBufferTime;
		}


		//converts indexy terrain into individual tris
		//this makes texturing easier / more versatile
		//can also do per terrain face normals
		Array BreakIntoTriList(Array verts, int w, int h)
		{
			Array	ret	=Array.CreateInstance(typeof(VPosNormTex04Col0), mNumTris * 3);

			//index the tris
			int	idx	=0;			
			for(UInt16 j=0;j < (h - 1);j++)
			{
				for(UInt16 i=(UInt16)(j * w);i < ((j * w) + (w - 1));i++)
				{
					ret.SetValue(verts.GetValue(i + w), idx++);
					ret.SetValue(verts.GetValue(i + 1), idx++);
					ret.SetValue(verts.GetValue(i), idx++);

					ret.SetValue(verts.GetValue(i + w), idx++);
					ret.SetValue(verts.GetValue((i + 1) + w), idx++);
					ret.SetValue(verts.GetValue(i + 1), idx++);
				}
			}
			return	ret;
		}

		/*
		internal Vector3 GetGoodColorForHeight(float height)
		{
			Vector3	snow	=Color.Snow.ToVector3();
			Vector3	forest	=Color.Brown.ToVector3();	//forest is dirtish
			Vector3	grass	=Color.LawnGreen.ToVector3();
			Vector3	sand	=Color.DarkKhaki.ToVector3();

			if(height >= SnowHeight)
			{
				//in the snowy area
				//See if within transition
				if(height < (SnowHeight + TransitionHeight))
				{
					//transition from snow to forest
					float	transFactor	=
						((SnowHeight + TransitionHeight) - height)
						/ TransitionHeight;

					return	Vector3.Lerp(snow, forest, transFactor);
				}
				else
				{
					//just snow
					return	snow;
				}
			}
			else if(height >= ForestHeight)
			{
				//in the forest zone
				if(height < (ForestHeight + TransitionHeight))
				{
					//transition from forest to grassland
					float	transFactor	=
						((ForestHeight + TransitionHeight) - height)
						/ TransitionHeight;

					return	Vector3.Lerp(forest, grass, transFactor);
				}
				else
				{
					//just forest
					return	forest;
				}
			}
			else if(height >= GrassHeight)
			{
				//in the grass zone
				if(height < (GrassHeight + TransitionHeight))
				{
					//transition from grass to sand
					float	transFactor	=
						((GrassHeight + TransitionHeight) - height)
						/ TransitionHeight;

					return	Vector3.Lerp(grass, sand, transFactor);
				}
				else
				{
					//just grass
					return	grass;
				}
			}
			else
			{
				return	sand;
			}
		}*/


		void GetSomething(List<TexData> texData, Array verts, int index)
		{
			Vector3	vert		=(Vector3)VertexTypes.GetArrayField(verts, index, "Position");
			Half2	texCoord	=(Half2)VertexTypes.GetArrayField(verts, index, "TexCoord0");
			float	height		=vert.Y;
			double	texU		=texCoord.X;
			double	texV		=texCoord.Y;
			bool	bChanged	=false;
			foreach(TexData td in texData)
			{
				if(td.Steep)
				{
					continue;
				}

				if(height >= td.BottomElevation
					&& height < td.TopElevation)
				{
					texU	*=td.mScaleU;
					texV	*=td.mScaleV;
					texU	+=td.mUOffs;
					texV	+=td.mVOffs;

					bChanged	=true;
					break;
				}
			}

			if(bChanged)
			{
				texCoord.X	=(float)texU;
				texCoord.Y	=(float)texV;

				VertexTypes.SetArrayField(verts, index, "TexCoord0", texCoord);
			}
		}


		List<int> GetVertTexures(List<TexData> texData, Vector3 vert, Half4 norm, float transitionHeight)
		{
			List<int>	affecting	=new List<int>();

			float	height		=vert.Y;
			float	halfTrans	=transitionHeight * 0.5f;
			for(int i=0;i < texData.Count;i++)
			{
				TexData	td	=texData[i];
				if(td.Steep)
				{
					//skip steeps for now
					continue;
				}

				if((height + halfTrans) >= td.BottomElevation
					&& (height - halfTrans) < td.TopElevation)
				{
					affecting.Add(i);
				}
			}

			//check for steeps
			for(int i=0;i < texData.Count;i++)
			{
				TexData	td	=texData[i];
				if(!td.Steep)
				{
					continue;
				}

				float	dot	=norm.dot(Vector3.UnitY);

				if(dot >= SteepnessThreshold)
				{
					continue;
				}

				if((height + halfTrans) >= td.BottomElevation
					&& (height - halfTrans) < td.TopElevation)
				{
					affecting.Add(i);
				}
			}

			return	affecting;
		}


		List<float>	ComputeVertTextureFactor(List<TexData> texData, List<int> affecting,
			Vector3 vert, Half4 norm, float transitionHeight)
		{
			List<float>	ret	=new List<float>();

			float	height		=vert.Y;
			float	halfTrans	=transitionHeight * 0.5f;

			foreach(int aff in affecting)
			{
				if(texData[aff].Steep)
				{
					continue;
				}
				float	min	=texData[aff].BottomElevation;
				float	max	=texData[aff].TopElevation;

				Debug.Assert(height <= (max + halfTrans) && height >= (min - halfTrans));
				Debug.Assert(halfTrans < ((max - min) * 0.5f));

				if(height >= (min + halfTrans)
					&& height <= (max - halfTrans))
				{
					ret.Add(1f);
					continue;
				}

				if(height >= (min - halfTrans)
					&& height < (min + halfTrans))
				{
					ret.Add((height - (min - halfTrans)) / transitionHeight);
					continue;
				}

				if(height > (max - halfTrans)
					&& height <= (max + halfTrans))
				{
					ret.Add(((max + halfTrans) - height) / transitionHeight);
					continue;
				}
				Debug.Assert(false);	//should have hit something
			}

			foreach(int aff in affecting)
			{
				if(!texData[aff].Steep)
				{
					continue;
				}
				float	min	=texData[aff].BottomElevation;
				float	max	=texData[aff].TopElevation;
				float	dot	=norm.dot(Vector3.UnitY);

				Debug.Assert(height <= (max + halfTrans) && height >= (min - halfTrans));
				Debug.Assert(halfTrans < ((max - min) * 0.5f));
				Debug.Assert(dot < SteepnessThreshold);

				float	steepFact	=(SteepnessThreshold - dot) / SteepnessThreshold;

				//bias up a bit, too hard to see
				steepFact	*=affecting.Count;

				if(height >= (min + halfTrans)
					&& height <= (max - halfTrans))
				{
					ret.Add(steepFact);
					continue;
				}

				if(height >= (min - halfTrans)
					&& height < (min + halfTrans))
				{
					float	fact	=(height - (min - halfTrans)) / transitionHeight;
					fact			*=steepFact;
					ret.Add(fact);
					continue;
				}

				if(height > (max - halfTrans)
					&& height <= (max + halfTrans))
				{
					float	fact	=((max + halfTrans) - height) / transitionHeight;
					fact			*=steepFact;
					ret.Add(fact);
					continue;
				}
				Debug.Assert(false);	//should have hit something
			}
			return	ret;
		}


		void ComputeTriTextureFactors(Array verts, List<TexData> texData,
			int idx1, int idx2, int idx3, float transitionHeight)
		{
			Vector3	vert1	=(Vector3)VertexTypes.GetArrayField(verts, idx1, "Position");
			Half4	norm1	=(Half4)VertexTypes.GetArrayField(verts, idx1, "Normal");

			List<int>	affecting1	=GetVertTexures(texData, vert1, norm1, transitionHeight);
			List<float>	amounts1	=ComputeVertTextureFactor(texData, affecting1, vert1, norm1, transitionHeight);

			Vector3	vert2	=(Vector3)VertexTypes.GetArrayField(verts, idx2, "Position");
			Half4	norm2	=(Half4)VertexTypes.GetArrayField(verts, idx2, "Normal");

			List<int>	affecting2	=GetVertTexures(texData, vert2, norm2, transitionHeight);
			List<float>	amounts2	=ComputeVertTextureFactor(texData, affecting2, vert2, norm2, transitionHeight);

			Vector3	vert3	=(Vector3)VertexTypes.GetArrayField(verts, idx3, "Position");
			Half4	norm3	=(Half4)VertexTypes.GetArrayField(verts, idx3, "Normal");

			List<int>	affecting3	=GetVertTexures(texData, vert3, norm3, transitionHeight);
			List<float>	amounts3	=ComputeVertTextureFactor(texData, affecting3, vert3, norm3, transitionHeight);

			Debug.Assert(affecting1.Count == amounts1.Count);
			Debug.Assert(affecting2.Count == amounts2.Count);
			Debug.Assert(affecting3.Count == amounts3.Count);

			//eliminate any zeros
			while(amounts1.Contains(0f))
			{
				int	index	=amounts1.IndexOf(0f);
				affecting1.RemoveAt(index);
				amounts1.RemoveAt(index);
			}
			while(amounts2.Contains(0f))
			{
				int	index	=amounts2.IndexOf(0f);
				affecting2.RemoveAt(index);
				amounts2.RemoveAt(index);
			}
			while(amounts3.Contains(0f))
			{
				int	index	=amounts3.IndexOf(0f);
				affecting3.RemoveAt(index);
				amounts3.RemoveAt(index);
			}

			List<int>	combinedAff	=new List<int>();
			foreach(int aff in affecting1)
			{
				if(!combinedAff.Contains(aff))
				{
					combinedAff.Add(aff);
				}
			}
			foreach(int aff in affecting2)
			{
				if(!combinedAff.Contains(aff))
				{
					combinedAff.Add(aff);
				}
			}
			foreach(int aff in affecting3)
			{
				if(!combinedAff.Contains(aff))
				{
					combinedAff.Add(aff);
				}
			}

			Dictionary<int, float>	combinedFact	=new Dictionary<int, float>();
			for(int i=0;i < affecting1.Count;i++)
			{
				if(combinedFact.ContainsKey(affecting1[i]))
				{
					combinedFact[affecting1[i]]	+=amounts1[i];
				}
				else
				{
					combinedFact.Add(affecting1[i], amounts1[i]);
				}
			}
			for(int i=0;i < affecting2.Count;i++)
			{
				if(combinedFact.ContainsKey(affecting2[i]))
				{
					combinedFact[affecting2[i]]	+=amounts2[i];
				}
				else
				{
					combinedFact.Add(affecting2[i], amounts2[i]);
				}
			}
			for(int i=0;i < affecting3.Count;i++)
			{
				if(combinedFact.ContainsKey(affecting3[i]))
				{
					combinedFact[affecting3[i]]	+=amounts3[i];
				}
				else
				{
					combinedFact.Add(affecting3[i], amounts3[i]);
				}
			}

			while(combinedAff.Count > 4)
			{
				//need to drop the smallest texture influence for the tri
				float	smallestFact	=float.MaxValue;
				int		smallest		=-1;
				foreach(KeyValuePair<int, float> fact in combinedFact)
				{
					if(fact.Value < smallestFact)
					{
						smallestFact	=fact.Value;
						smallest		=fact.Key;
					}
				}

				//drop smallest
				combinedAff.Remove(smallest);
				combinedFact.Remove(smallest);

				//drop factors from individual verts
				if(affecting1.Contains(smallest))
				{
					amounts1.RemoveAt(affecting1.IndexOf(smallest));
					affecting1.Remove(smallest);
				}
				if(affecting2.Contains(smallest))
				{
					amounts2.RemoveAt(affecting2.IndexOf(smallest));
					affecting2.Remove(smallest);
				}
				if(affecting3.Contains(smallest))
				{
					amounts3.RemoveAt(affecting3.IndexOf(smallest));
					affecting3.Remove(smallest);
				}
			}

			//smooth out the remaining so that all factors add to 1
			float	total	=0f;
			for(int i=0;i < affecting1.Count;i++)
			{
				total	+=amounts1[i];
			}
			for(int i=0;i < affecting1.Count;i++)
			{
				amounts1[i]	/=total;
			}

			total	=0f;
			for(int i=0;i < affecting2.Count;i++)
			{
				total	+=amounts2[i];
			}
			for(int i=0;i < affecting2.Count;i++)
			{
				amounts2[i]	/=total;
			}

			total	=0f;
			for(int i=0;i < affecting3.Count;i++)
			{
				total	+=amounts3[i];
			}
			for(int i=0;i < affecting3.Count;i++)
			{
				amounts3[i]	/=total;
			}

			//set affecting
			//these use the per tri value to
			//prevent interpolation in the pixel shader
			SetVertAffecting(verts, idx1, texData, combinedAff);
			SetVertAffecting(verts, idx2, texData, combinedAff);
			SetVertAffecting(verts, idx3, texData, combinedAff);

			//match up the amounts to combinedAff
			List<float>	temp	=new List<float>();
			foreach(int aff in combinedAff)
			{
				if(!affecting1.Contains(aff))
				{
					temp.Add(0f);
				}
				else
				{
					temp.Add(amounts1[affecting1.IndexOf(aff)]);
				}
			}
			SetVertFactor(verts, idx1, temp);

			temp.Clear();
			foreach(int aff in combinedAff)
			{
				if(!affecting2.Contains(aff))
				{
					temp.Add(0f);
				}
				else
				{
					temp.Add(amounts2[affecting2.IndexOf(aff)]);
				}
			}
			SetVertFactor(verts, idx2, temp);

			temp.Clear();
			foreach(int aff in combinedAff)
			{
				if(!affecting3.Contains(aff))
				{
					temp.Add(0f);
				}
				else
				{
					temp.Add(amounts3[affecting3.IndexOf(aff)]);
				}
			}
			SetVertFactor(verts, idx3, temp);
		}


		void SetVertAffecting(Array verts, int index, List<TexData> texData, List<int> affecting)
		{
			if(affecting.Count == 0)
			{
				return;
			}

			VPosNormTex04Col0	vert	=(VPosNormTex04Col0)verts.GetValue(index);

			vert.Color0.R	=(byte)affecting[0];

			if(affecting.Count > 1)
			{
				vert.Color0.G	=(byte)affecting[1];
			}

			if(affecting.Count > 2)
			{
				vert.Color0.B	=(byte)affecting[2];
			}

			if(affecting.Count > 3)
			{
				vert.Color0.A	=(byte)affecting[3];
			}

			verts.SetValue(vert, index);
		}


		void SetVertFactor(Array verts, int index, List<float> amounts)
		{
			if(amounts.Count == 0)
			{
				return;
			}
			VPosNormTex04Col0	v	=(VPosNormTex04Col0)verts.GetValue(index);

			v.TexCoord0	=Vector4.Zero;

			//texcoord0 contains 4 factors
			v.TexCoord0.X	=amounts[0];
			if(amounts.Count > 1)
			{
				v.TexCoord0.Y	=amounts[1];
			}
			if(amounts.Count > 2)
			{
				v.TexCoord0.Z	=amounts[2];
			}
			if(amounts.Count > 3)
			{
				v.TexCoord0.W	=amounts[3];
			}

			verts.SetValue(v, index);
		}


		void SetTextureFactors(Array verts, List<TexData> texData,
			int w, int h, float transitionHeight)
		{
			//each triangle can have up to 4 textures influencing
			//if there are more than 4, the smallest needs to be dropped
			//all factors combined, per vertex, should add up to 1

			//look up per poly how many textures are in use
			for(int i=0;i < verts.Length;i+=3)
			{
				ComputeTriTextureFactors(verts, texData, i, i + 1, i + 2, transitionHeight);
			}
		}

		/*
		Vector3 CalcVertNormal(TerrainVert []v, int x, int y, int w, int h)
		{
			//find all the neighboring verts
			Vector3	upper		=Vector3.UnitY;
			Vector3	left		=Vector3.UnitY;
			Vector3	center		=Vector3.UnitY;
			Vector3	right		=Vector3.UnitY;
			Vector3	lower		=Vector3.UnitY;

			if(y > 0)
			{
				upper	=v[((y - 1) * w) + x].Position;
			}

			if(x > 0)
			{
				left	=v[(y * w) + (x - 1)].Position;
			}

			center	=v[(y * w) + x].Position;

			if(x < (w - 1))
			{
				right	=v[(y * w) + (x + 1)].Position;
			}

			if(y < (h - 1))
			{
				lower	=v[((y + 1) * w) + x].Position;
			}

			center	=v[(y * w) + x].Position;

			//face normals
			Vector3	ulNorm	=Vector3.UnitY;
			Vector3	urNorm	=Vector3.UnitY;
			Vector3	llNorm	=Vector3.UnitY;
			Vector3	lrNorm	=Vector3.UnitY;

			//gen face normals
			Vector3	edge0	=Vector3.Zero;
			Vector3	edge1	=Vector3.Zero;

			//gen upper left tri normal
			if(x > 0 && y > 0)
			{
				edge0	=upper - center;
				edge1	=center - left;

				ulNorm	=Vector3.Cross(edge0, edge1);
			}

			//gen upper right normal
			if(x < (w - 1) && y > 0)
			{
				edge0	=right - center;
				edge1	=center - upper;

				urNorm	=Vector3.Cross(edge0, edge1);
			}

			//gen lower left tri normal
			if(x > 0 && y < (h - 1))
			{
				edge0	=left - center;
				edge1	=center - lower;

				llNorm	=Vector3.Cross(edge0, edge1);
			}

			//gen lower right tri normal
			if(x < (w - 1) && y < (h - 1))
			{
				edge0	=center - right;
				edge1	=lower - center;

				lrNorm	=Vector3.Cross(edge0, edge1);
			}

			Vector3	ret	=ulNorm + urNorm + llNorm + lrNorm;

			ret.Normalize();

			return	ret;
		}*/


		public void Draw(DeviceContext dc, MatLib mats,
			Matrix world, Matrix view, Matrix proj)
		{
			if(mNumTris <= 0 || !mbReady)
			{
				return;
			}

			mats.SetMaterialParameter("Terrain", "mLocal", mMat);

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mats.ApplyMaterialPass("Terrain", dc, 0);

			dc.Draw(mNumVerts, 0);
		}


		public void SetRelativePos(Point p, int cw, int ch, int chunkDim, int polySize)
		{
			Point	relative, relativeWrapNeg, relativeWrapPos;

			Point	cellWrapNeg	=mCellCoordinate;
			Point	cellWrapPos	=mCellCoordinate;

			cellWrapNeg.X	-=cw;
			cellWrapNeg.Y	-=ch;
			cellWrapPos.X	+=cw;
			cellWrapPos.Y	+=ch;

			relative.X	=mCellCoordinate.X - p.X;
			relative.Y	=mCellCoordinate.Y - p.Y;

			relativeWrapNeg.X	=cellWrapNeg.X - p.X;
			relativeWrapNeg.Y	=cellWrapNeg.Y - p.Y;

			relativeWrapPos.X	=cellWrapPos.X - p.X;
			relativeWrapPos.Y	=cellWrapPos.Y - p.Y;

			//take the nearest
			if(Math.Abs(relative.X) > Math.Abs(relativeWrapNeg.X))
			{
				relative.X	=relativeWrapNeg.X;
			}
			if(Math.Abs(relative.Y) > Math.Abs(relativeWrapNeg.Y))
			{
				relative.Y	=relativeWrapNeg.Y;
			}
			if(Math.Abs(relative.X) > Math.Abs(relativeWrapPos.X))
			{
				relative.X	=relativeWrapPos.X;
			}
			if(Math.Abs(relative.Y) > Math.Abs(relativeWrapPos.Y))
			{
				relative.Y	=relativeWrapPos.Y;
			}

			Vector3	pos	=Vector3.Zero;

			pos.X	=relative.X * chunkDim * polySize;
			pos.Z	=relative.Y * chunkDim * polySize;

			//TODO: remove DEBUG
//			pos.Y	=relative.Y;	//for testing, to check edges

			SetPos(pos);

			mbReady	=true;
		}


		void SetPos(Vector3 pos)
		{
			mPosition	=pos;

			//update matrix
			mMat	=Matrix.Translation(mPosition);

			//update bounds
			mCellBounds.Minimum	=mBounds.Minimum + pos;
			mCellBounds.Maximum	=mBounds.Maximum + pos;
		}


		public Vector3 GetPos()
		{
			return	mPosition;
		}


		public float GetPeak()
		{
			return	mPeak;
		}


		public bool InFrustum(BoundingFrustum frust)
		{
			ContainmentType	ct	=frust.Contains(mCellBounds);

			return	(ct != ContainmentType.Disjoint);
		}
	}
}
