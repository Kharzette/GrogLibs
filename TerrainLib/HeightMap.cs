using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;


namespace TerrainLib
{
	public struct VPNTT
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Vector4	TexFactor0;
		public Vector4	TexFactor1;
	};


	public class HeightMap
	{
		const float	UNITS_PER_HEIGHT	=0.1f;		//res of the output in units
		const float	HEIGHT_SCALAR		=1.0f;		//a good mountain setting

		//height settings
		const float	SnowHeight			=150.0f;
		const float	StoneHighHeight		=150.0f;
		const float	StoneMossyHeight	=80.0f;
		const float	ForestHeight		=80.0f;
		const float	GrassHeight			=30.0f;
		const float	SandHeight			=0.0f;
		const float	WaterHeight			=25.0f;
		const float	TransitionHeight	=40.0f;
		const float	SteepnessThreshold	=0.7f;

		VertexBuffer		mVBTerrain;
		IndexBuffer			mIBTerrain;

		int		mNumIndex, mNumVerts, mNumTris;

		//location stuff
		Vector3	mPosition;
		Matrix	mMat;
		float	mPeak;		//max height
		float	mValley;	//min height

		//bounds for frust rejection
		BoundingBox	mBounds;

		//timings
		long	mPosTime, mNormTime, mCopyTime;
		long	mTexFactTime, mIndexTime, mBufferTime;


		//2D float array
		public HeightMap(float				[,]data,
						 int				w,
						 int				h,
						 int				actualWidth,
						 int				actualHeight,
						 int				offsetX,
						 int				offsetY,
						 GraphicsDevice		gd,
						 VertexDeclaration	vd)
		{
			mNumVerts	=actualWidth * actualHeight;
			mNumTris	=((actualWidth - 1) * (actualHeight - 1)) * 2;
			mNumIndex	=mNumTris * 3;

			//alloc some space for verts and indexs
			VPNTT	[]verts		=new VPNTT[w * h];
			ushort	[]indexs	=new ushort[mNumIndex];

			Stopwatch	sw	=new Stopwatch();

			sw.Start();
			
			//load the height map
			Vector3	min	=Vector3.One * float.MaxValue;
			Vector3	max	=Vector3.One * float.MinValue;
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					Vector3	pos	=Vector3.Zero;
					int		dex	=x + (y * w);

					pos.X	=(float)(x - offsetX);
					pos.Z	=(float)(y - offsetY);
					pos.Y	=data[y, x] * HEIGHT_SCALAR;

					verts[dex].Position	=pos;

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

					//texfactors
					verts[dex].TexFactor0	=Vector4.Zero;
					verts[dex].TexFactor1	=Vector4.Zero;
				}
			}
			sw.Stop();

			mPosTime	=sw.ElapsedTicks;

			mPeak	=max.Y;
			mValley	=min.Y;

			mBounds.Max	=max;
			mBounds.Min	=min;

			sw.Reset();
			sw.Start();
			//build normals with the full set
			BuildNormals(verts, w, h);
			sw.Stop();
			mNormTime	=sw.ElapsedTicks;

			sw.Reset();
			sw.Start();
			//reduce down to the active set
			VPNTT	[]actualVerts	=new VPNTT[mNumVerts];
			int	cnt	=0;
			for(int y=offsetY;y < (actualHeight + offsetY);y++)
			{
				for(int x=offsetX;x < (actualWidth + offsetX);x++)
				{
					actualVerts[cnt++]	=verts[(y * w) + x];
				}
			}
			sw.Stop();
			mCopyTime	=sw.ElapsedTicks;

			sw.Reset();
			sw.Start();
			SetTextureFactors(actualVerts);
			sw.Stop();
			mTexFactTime	=sw.ElapsedTicks;

			sw.Reset();
			sw.Start();
			IndexTris(actualWidth, actualHeight, gd);
			sw.Stop();
			mIndexTime	=sw.ElapsedTicks;

			sw.Reset();
			sw.Start();
			mVBTerrain	=new VertexBuffer(gd, vd, mNumVerts, BufferUsage.WriteOnly);
			mVBTerrain.SetData<VPNTT>(actualVerts);
			sw.Stop();
			mBufferTime	=sw.ElapsedTicks;

			//see if a water plane is needed
			if(mValley < WaterHeight)
			{
//				VPNTT
			}
		}


		internal void GetTimings(out long pos, out long norm, out long copy,
			out long texFact, out long index, out long buffer)
		{
			pos		=mPosTime;
			norm	=mNormTime;
			copy	=mCopyTime;
			texFact	=mTexFactTime;
			index	=mIndexTime;
			buffer	=mBufferTime;
		}


		void IndexTris(int w, int h, GraphicsDevice gd)
		{
			ushort	[]indexs	=new ushort[mNumIndex];

			//index the tris
			ushort	idx	=0;
			for(int j=0;j < (h - 1);j++)
			{
				for(int i=(j * w);i < ((j * w) + (w - 1));i++)
				{
					indexs[idx++]	=(ushort)i;
					indexs[idx++]	=(ushort)(i + 1);
					indexs[idx++]	=(ushort)(i + w);

					indexs[idx++]	=(ushort)(i + 1);
					indexs[idx++]	=(ushort)((i + 1) + w);
					indexs[idx++]	=(ushort)(i + w);
				}
			}
			mIBTerrain	=new IndexBuffer(gd, IndexElementSize.SixteenBits, mNumIndex, BufferUsage.WriteOnly);
			mIBTerrain.SetData<ushort>(indexs);
		}


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
		}


		void SetTextureFactors(VPNTT []v)
		{
			//assign basic texturing based on height
			for(int i=0;i < v.Length;i++)
			{
				float	height	=v[i].Position.Y;

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

						v[i].TexFactor0.Z	=transFactor;
						v[i].TexFactor1.W	=transFactor;
						v[i].TexFactor1.Z	=1.0f - transFactor;
						v[i].TexFactor0.Z	*=0.5f;
						v[i].TexFactor1.W	*=0.5f;
					}
					else
					{
						//just snow
						v[i].TexFactor1.Z	=1.0f;
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

						v[i].TexFactor0.Y	=transFactor;
						v[i].TexFactor0.Z	=1.0f - transFactor;
						v[i].TexFactor1.W	=1.0f - transFactor;
						v[i].TexFactor0.Z	*=0.5f;
						v[i].TexFactor1.W	*=0.5f;
					}
					else
					{
						//just forest
						v[i].TexFactor0.Z	=0.5f;
						v[i].TexFactor1.W	=0.5f;
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

						v[i].TexFactor0.X	=transFactor;
						v[i].TexFactor0.Y	=1.0f - transFactor;
					}
					else
					{
						//just grass
						v[i].TexFactor0.Y	=1.0f;
					}
				}
				else
				{
					v[i].TexFactor0.X	=1.0f;
				}
			}

			//look for steep surfaces
			for(int i=0;i < v.Length;i++)
			{
				float	dot	=Vector3.Dot(v[i].Normal, Vector3.UnitY);

				if(dot < SteepnessThreshold)
				{
					float	fact	=(SteepnessThreshold - dot) / SteepnessThreshold;
					fact	=(SteepnessThreshold - dot)
						/ (SteepnessThreshold / 6.0f);

					MathHelper.Clamp(fact, 0.0f, 1.0f);

					if(v[i].TexFactor0.X > 0.0f)
					{
						//there's a bit of sand, use high stone
						float	val	=v[i].TexFactor0.X;

						v[i].TexFactor1.Y	+=val * fact;
						v[i].TexFactor0.X	=val * (1.0f - fact);
					}

					if(v[i].TexFactor0.Y > 0.0f)
					{
						//grass is here, use mossy stone
						float	val	=v[i].TexFactor0.Y;

						v[i].TexFactor1.X	+=val * fact;
						v[i].TexFactor0.Y	=val * (1.0f - fact);
					}

					if(v[i].TexFactor0.Z > 0.0f)
					{
						//forest is here, use mossy stone
						float	val	=v[i].TexFactor0.Z;

						v[i].TexFactor1.X	+=val * fact;
						v[i].TexFactor0.Z	=val * (1.0f - fact);
					}

					if(v[i].TexFactor1.Z > 0.0f)
					{
						//snow, use high stone
						//snow needs a harder transition
						fact	=(SteepnessThreshold - dot)
							/ (SteepnessThreshold / 6.0f);

						MathHelper.Clamp(fact, 0.0f, 1.0f);

						float	val	=v[i].TexFactor1.Z;

						v[i].TexFactor1.Y	+=val * fact;
						v[i].TexFactor1.Z	=val * (1.0f - fact);
					}

					//check for mossy and granite mix
					if(v[i].TexFactor1.X > 0.0f && v[i].TexFactor1.Y > 0.0f)
					{
						//boost both, as they were taking a share
						//of the tex factor from each other

						//add up the factors
						float	val	=v[i].TexFactor0.X;
						val	+=v[i].TexFactor0.Y;
						val	+=v[i].TexFactor0.Z;
						val	+=v[i].TexFactor0.W;
						val	+=v[i].TexFactor1.X;
						val	+=v[i].TexFactor1.Y;
						val	+=v[i].TexFactor1.Z;
						val	+=v[i].TexFactor1.W;

						val	=1.0f - val;
						val	*=0.5f;

						v[i].TexFactor1.X	+=val;
						v[i].TexFactor1.Y	+=val;
					}
				}
			}
		}


		Vector3 CalcVertNormal(VPNTT []v, int x, int y, int w, int h)
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
		}


		void BuildNormals(VPNTT []v, int w, int h)
		{
			Vector3	[]adjacent	=new Vector3[8];
			bool	[]valid		=new bool[8];

			//generate normals
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					//get the positions of the 8
					//adjacent verts, numbered clockwise
					//from upper right on a grid

					//grab first 3 spots which
					//are negative in Y
					if(y > 0)
					{
						if(x > 0)
						{
							adjacent[0]	=v[(x - 1) + ((y - 1) * w)].Position;
							valid[0]	=true;
						}
						else
						{
							valid[0]	=false;
						}

						adjacent[1]	=v[x + ((y - 1) * w)].Position;
						valid[1]	=true;

						if(x < (w - 1))
						{
							adjacent[2]	=v[(x + 1) + ((y - 1) * w)].Position;
							valid[2]	=true;
						}
						else
						{
							valid[2]	=false;
						}
					}
					else
					{
						valid[0]	=false;
						valid[1]	=false;
						valid[2]	=false;
					}

					//next two are to the sides of
					//the calcing vert in X
					if(x > 0)
					{
						adjacent[7]	=v[(x - 1) + (y * w)].Position;
						valid[7]	=true;
					}
					else
					{
						valid[7]	=false;
					}

					if(x < (w - 1))
					{
						adjacent[3]	=v[(x + 1) + (y * w)].Position;
						valid[3]	=true;
					}
					else
					{
						valid[3]	=false;
					}

					//next three are positive in Y
					if(y < (h - 1))
					{
						if(x > 0)
						{
							adjacent[6]	=v[(x - 1) + ((y + 1) * w)].Position;
							valid[6]	=true;
						}
						else
						{
							valid[6]	=false;
						}

						adjacent[5]	=v[x + ((y + 1) * w)].Position;
						valid[5]	=true;

						if(x < (w - 1))
						{
							adjacent[4]	=v[(x + 1) + ((y + 1) * w)].Position;
							valid[4]	=true;
						}
						else
						{
							valid[4]	=false;
						}
					}
					else
					{
						valid[5]	=false;
						valid[6]	=false;
						valid[4]	=false;
					}

					//use the edges between adjacents
					//to determine a good normal
					Vector3	norm, edge1, edge2;

					norm	=Vector3.Zero;

					for(int i=0;i < 8;i++)
					{
						//find next valid adjacent
						while(i < 8 && !valid[i])
						{
							i++;
						}
						if(i >= 8)
						{
							break;
						}

						//note the i++
						edge1	=adjacent[i++] - v[x + (y * w)].Position;

						//find next valid adjacent
						while(i < 8 && !valid[i])
						{
							i++;
						}
						if(i >= 8)
						{
							break;
						}
						edge2	=adjacent[i] - v[x + (y * w)].Position;

						norm	+=Vector3.Cross(edge2, edge1);
					}

					//average
					norm.Normalize();

					v[x + (y * w)].Normal	=norm;
				}
			}
		}


		public void Draw(GraphicsDevice gd, Effect fx)
		{
			gd.SetVertexBuffer(mVBTerrain);
			gd.Indices				=mIBTerrain;

			//set local matrix
			fx.Parameters["mLocal"].SetValue(mMat);

			fx.CurrentTechnique.Passes[0].Apply();

			//draw shizzle here
			gd.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0, mNumVerts, 0, mNumTris);
		}


		public void SetPos(Vector3 pos)
		{
			mPosition	=pos;

			//update matrix
			mMat	=Matrix.CreateTranslation(mPosition);

			//update bounds
			mBounds.Min	+=pos;
			mBounds.Max	+=pos;
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
			ContainmentType	ct	=frust.Contains(mBounds);

			return	(ct != ContainmentType.Disjoint);
		}
	}
}
