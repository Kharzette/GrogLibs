using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace TerrainLib
{
	class ChunkState
	{
		internal float	[,]mData;
		internal int	mChunkX, mChunkY;
		internal float	mPolySize;

		internal GraphicsDevice	mGD;
	}


	//constructs and stores multiple heightmaps
	//handles the tiling and other such
	public class Terrain
	{
		const int	CHUNKDIM	=128;		//modify this to affect heightmap data size

		//height maps
		List<HeightMap>	mMaps;

		//tex & shading, there is no 3
		Texture2D			mTEXAtlas;
		Effect				mFXTerrain;
		VertexDeclaration	mVDTerrain;

		//locations
		Vector3	mLevelPos;
		Vector3	mEyePos;

		//thread counter
		int	mThreadCounter;


		//set up textures and such
		public Terrain(Texture2D texAtlas, ContentManager cm)
		{
			mTEXAtlas	=texAtlas;

			InitVertexDeclaration();
			InitEffect(cm);
		}


		//load from a 2D float array
		public void Build(float				[,]data,
						  GraphicsDevice	gd,
						  float				polySize)
		{
			//alloc/clear map list
			mMaps	=new List<HeightMap>();

			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			//set to nearest power of two
			int	pow	=0;
			while(w > 0)
			{
				w	>>=1;
				pow++;
			}
			w	=1 << (pow - 1);

			pow	=0;
			while(h > 0)
			{
				h	>>=1;
				pow++;
			}
			h	=1 << (pow - 1);

			//this seems to be the best for quick loadery
//			ThreadPool.SetMaxThreads(4, 4);

			mThreadCounter	=(h / CHUNKDIM) * (w / CHUNKDIM);

			for(int chunkY=0;chunkY < (h / CHUNKDIM);chunkY++)
			{
				for(int chunkX=0;chunkX < (w / CHUNKDIM);chunkX++)
				{
					ChunkState	cs	=new ChunkState();
					cs.mData		=data;
					cs.mChunkX		=chunkX;
					cs.mChunkY		=chunkY;
					cs.mGD			=gd;
					cs.mPolySize	=polySize;

					ThreadPool.QueueUserWorkItem(DoChunk, cs);
				}
			}

			while(!Interlocked.Equals(mThreadCounter, 0))
			{
				Thread.Sleep(2);
			}

//			ThreadPool.SetMaxThreads(8, 8);
		}


		void DoChunk(object state)
		{
			ChunkState	cs	=state as ChunkState;
			if(cs == null)
			{
				return;
			}

			float	[,]chunk	=new float[CHUNKDIM + 3, CHUNKDIM + 3];

			int	w	=cs.mData.GetLength(1);
			int	h	=cs.mData.GetLength(0);

			int	startY	=(CHUNKDIM * cs.mChunkY);
			int	startX	=(CHUNKDIM * cs.mChunkX);
			if(startY > 0)
			{
				startY--;	//back up one if possible
			}
			if(startX > 0)
			{
				startX--;
			}

			int	endY	=(CHUNKDIM * (cs.mChunkY + 1)) + 1;
			int	endX	=(CHUNKDIM * (cs.mChunkX + 1)) + 1;
			if(endY < h)
			{
				endY++;	//increase by one if possible
			}
			if(endX < w)
			{
				endX++;
			}

			for(int y=startY, t=0;y < endY;y++,t++)
			{
				for(int x=startX, s=0;x < endX;x++,s++)
				{
					chunk[t, s]	=cs.mData[y, x];
				}
			}

			HeightMap	map	=new HeightMap(chunk,
				endX - startX, endY - startY,
				CHUNKDIM + 1, CHUNKDIM + 1,
				(CHUNKDIM * cs.mChunkX) - startX,
				(CHUNKDIM * cs.mChunkY) - startY, cs.mGD, mVDTerrain);

			Vector3	pos	=Vector3.Zero;
			pos.X	=cs.mChunkX * (CHUNKDIM) * cs.mPolySize;
			pos.Z	=cs.mChunkY * (CHUNKDIM) * cs.mPolySize;
			pos.Y	=0.0f;

			map.SetPos(pos);

			lock(mMaps)
			{
				mMaps.Add(map);
			}

			Interlocked.Decrement(ref mThreadCounter);
		}


		public Vector3 GetGoodColorForHeight(float height)
		{
			return	mMaps[0].GetGoodColorForHeight(height);
		}


		public void GetTimings(out long pos, out long norm, out long copy,
			out long texFact, out long index, out long buffer)
		{
			long	posAccum, normAccum, copyAccum;
			long	tfAccum, indAccum, bufAccum;

			pos	=norm	=copy	=texFact	=index	=buffer	=0;

			foreach(HeightMap hm in mMaps)
			{
				hm.GetTimings(out posAccum, out normAccum, out copyAccum,
					out tfAccum, out indAccum, out bufAccum);

				pos		+=posAccum;
				norm	+=normAccum;
				copy	+=copyAccum;
				texFact	+=tfAccum;
				index	+=indAccum;
				buffer	+=bufAccum;
			}
		}


		void InitEffect(ContentManager cm)
		{
			mFXTerrain	=cm.Load<Effect>("Shaders/Terrain");

			//set up shader stuff that won't change for now
			Vector4	colr	=Vector4.Zero;
			colr.X	=1.0f;
			colr.Y	=1.0f;
			colr.Z	=1.0f;
			colr.W	=1.0f;
			mFXTerrain.Parameters["mLightColor"].SetValue(colr);

			colr.X	=0.2f;
			colr.Y	=0.2f;
			colr.Z	=0.2f;
			colr.W	=1.0f;
			mFXTerrain.Parameters["mAmbientColor"].SetValue(colr);

			Vector3	dir	=Vector3.Zero;
			dir.X	=0.4f;
			dir.Z	=-0.2f;
			dir.Y	=-0.4f;
			dir.Normalize();

			mFXTerrain.Parameters["mLightDirection"].SetValue(dir);

			if(mTEXAtlas != null)
			{
				mFXTerrain.Parameters["mTexAtlas"].SetValue(mTEXAtlas);
			}

			//fog stuff
			Vector3	fogColor	=Vector3.Zero;
			fogColor.X	=0.8f;
			fogColor.Y	=0.9f;
			fogColor.Z	=1.0f;
			mFXTerrain.Parameters["mFogEnabled"].SetValue(1.0f);
			mFXTerrain.Parameters["mFogStart"].SetValue(500.0f);
			mFXTerrain.Parameters["mFogEnd"].SetValue(1000.0f);
			mFXTerrain.Parameters["mFogColor"].SetValue(fogColor);
			mFXTerrain.Parameters["mEyePosition"].SetValue(Vector3.Zero);
		}


		void InitVertexDeclaration()
		{
			//set up a 2 texcoord vert element
			VertexElement	[]ve	=new VertexElement[4];

			ve[0]	=new VertexElement(0, VertexElementFormat.Vector3,
						VertexElementUsage.Position, 0);
			ve[1]	=new VertexElement(12, VertexElementFormat.Vector3,
						VertexElementUsage.Normal, 0);
			ve[2]	=new VertexElement(24, VertexElementFormat.Vector4,
						VertexElementUsage.Color, 0);
			ve[3]	=new VertexElement(40, VertexElementFormat.Vector4,
						VertexElementUsage.Color, 1);

			mVDTerrain	=new VertexDeclaration(ve);
		}


		public void Draw(GraphicsDevice gd, BoundingFrustum frust,
			Matrix pupNearViewProj, Matrix pupFarViewProj, Matrix avaLightViewProj,
			RenderTarget2D pupNearShad, RenderTarget2D pupFarShad, RenderTarget2D avaShad)
		{
//			mFXTerrain.CurrentTechnique	=mFXTerrain.Techniques["VertexLightingXSamp"];

			if(pupNearShad == null)
			{
				mFXTerrain.CurrentTechnique	=mFXTerrain.Techniques["VertexLightingAvaShadOnly"];
			}
			else
			{
				mFXTerrain.CurrentTechnique	=mFXTerrain.Techniques["VertexLighting"];
			}

			if(pupNearShad != null)
			{
				mFXTerrain.Parameters["mPUPNearShadowTex"].SetValue(pupNearShad);
			}
			if(pupFarShad != null)
			{
				mFXTerrain.Parameters["mPUPFarShadowTex"].SetValue(pupFarShad);
			}
			if(avaShad != null)
			{
				mFXTerrain.Parameters["mAvaShadowTex"].SetValue(avaShad);
			}
			mFXTerrain.Parameters["mPUPNearLightViewProj"].SetValue(pupNearViewProj);
			mFXTerrain.Parameters["mPUPFarLightViewProj"].SetValue(pupFarViewProj);
			mFXTerrain.Parameters["mAvaLightViewProj"].SetValue(avaLightViewProj);

			foreach(HeightMap m in mMaps)
			{
				if(m.InFrustum(frust))
				{
					m.Draw(gd, mFXTerrain);
				}
			}			
		}


		public void Draw(GraphicsDevice gd, BoundingFrustum frust)
		{
			mFXTerrain.CurrentTechnique	=mFXTerrain.Techniques["Simple"];

			gd.DepthStencilState	=DepthStencilState.Default;

			foreach(HeightMap m in mMaps)
			{
				if(m.InFrustum(frust))
				{
					m.Draw(gd, mFXTerrain);
				}
			}			
		}


		public void DrawWorldY(GraphicsDevice gd, BoundingFrustum frust)
		{
			mFXTerrain.CurrentTechnique	=mFXTerrain.Techniques["WorldY"];

			foreach(HeightMap m in mMaps)
			{
				if(m.InFrustum(frust))
				{
					m.Draw(gd, mFXTerrain);
				}
			}			
		}


		//get the peak of the heightmap closest to pos
		public float GetLocalHeight(Vector3 pos)
		{
			HeightMap	closeMap	=null;
			float		closest		=696969.0f;
			foreach(HeightMap m in mMaps)
			{
				Vector3	dist	=m.GetPos();
				dist	+=mLevelPos;
				dist	+=mEyePos;

				float	len	=dist.Length();

				if(len < closest)
				{
					closest		=len;
					closeMap	=m;
				}
			}
			if(closeMap != null)
			{
				return	closeMap.GetPeak();
			}
			else
			{
				return	6969.0f;
			}
		}


		//matrices, matrii, matrixs?
		public void UpdateMatrices(Matrix world, Matrix view, Matrix proj)
		{
			mFXTerrain.Parameters["mWorld"].SetValue(world);
			mFXTerrain.Parameters["mView"].SetValue(view);
			mFXTerrain.Parameters["mProjection"].SetValue(proj);
		}


		public void UpdateLightColor(Vector4 lightColor)
		{
			mFXTerrain.Parameters["mLightColor"].SetValue(lightColor);
		}


		public void UpdateLightDirection(Vector3 lightDir)
		{
			mFXTerrain.Parameters["mLightDirection"].SetValue(lightDir);
		}


		public void UpdateAmbientColor(Vector4 ambient)
		{
			mFXTerrain.Parameters["mAmbientColor"].SetValue(ambient);
		}


		public void UpdatePosition(Vector3 pos)
		{
			Matrix	mat	=Matrix.CreateTranslation(pos);

			mLevelPos	=pos;

			mFXTerrain.Parameters["mLevel"].SetValue(mat);
		}


		public void UpdateEyePos(Vector3 pos)
		{
			mEyePos	=pos;
			mFXTerrain.Parameters["mEyePosition"].SetValue(pos);
		}


		public void SetFogEnabled(bool bFog)
		{
			if(bFog)
			{
				mFXTerrain.Parameters["mFogEnabled"].SetValue(1.0f);
			}
			else
			{
				mFXTerrain.Parameters["mFogEnabled"].SetValue(0.0f);
			}
		}
	}
}