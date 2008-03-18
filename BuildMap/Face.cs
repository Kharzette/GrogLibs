using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;

namespace BuildMap
{
    public struct Plane
    {
        public Vector3	Normal;
        public float	Dist;
    }

	class LightInfo
	{
		public	Vector3	mTexOrg;
		public	Vector3	mWorldToTexS, mWorldToTexT;
		public	Vector3	mTexToWorldS, mTexToWorldT;
		public	float	mExactMinS, mExactMinT;
		public	float	mExactMaxS, mExactMaxT;
		public	int		mTexMinS, mTexMinT;
		public	int		mTexSizeS, mTexSizeT;

		//lightmap points in 3space
		public	Vector3[]	mSurface;
		public	int			mNumSurfacePoints;

		//actual lightmap points
		public	float[]		mSamples;


		public void WriteToFile(BinaryWriter bw)
		{
			//can't think of anything the game will need
		}


		public	void CalcFaceVectors(Plane p, TexInfo t)
		{
			mWorldToTexS	=t.mTexS;
			mWorldToTexT	=t.mTexT;

			Vector3	texNormal	=Vector3.Cross(mWorldToTexS, mWorldToTexT);
			texNormal.Normalize();

			Debug.Assert(!float.IsNaN(texNormal.X));

			float	d	=Vector3.Dot(texNormal, p.Normal);
			Debug.Assert(d != 0.0f);
			if(d < 0.0f)
			{
				d			=-d;
				texNormal	=-texNormal;
			}

			d	=1.0f / d;

			float	len	=mWorldToTexS.Length();
			float	d2	=Vector3.Dot(mWorldToTexS, p.Normal);
			d2	*=d;
			mTexToWorldS	=mWorldToTexS + (texNormal * -d2);
			mTexToWorldS	*=((1.0f / len) * (1.0f / len));
			Debug.Assert(!float.IsNaN(mTexToWorldS.X));

			len	=mWorldToTexT.Length();
			d2	=Vector3.Dot(mWorldToTexT, p.Normal);
			d2	*=d;
			mTexToWorldT	=mWorldToTexT + (texNormal * -d2);
			mTexToWorldT	*=((1.0f / len) * (1.0f / len));

			mTexOrg.X	=-t.mTexVecs.uOffset * mTexToWorldS.X -
							t.mTexVecs.vOffset * mTexToWorldT.X;
			mTexOrg.Y	=-t.mTexVecs.uOffset * mTexToWorldS.Y -
							t.mTexVecs.vOffset * mTexToWorldT.Y;
			mTexOrg.Z	=-t.mTexVecs.uOffset * mTexToWorldS.Z -
							t.mTexVecs.vOffset * mTexToWorldT.Z;

			d2	=Vector3.Dot(mTexOrg, p.Normal) - p.Dist - 1.0f;
			d2	*=d;
			mTexOrg	=mTexOrg + (texNormal * -d2);
		}


		public	void CalcFaceExtents(List<Vector3> pnts, TexInfo t)
		{
			//figure out the face extents
			float	mins, mint, maxs, maxt;

			mins	=mint	=Bounds.MIN_MAX_BOUNDS;
			maxs	=maxt	=-Bounds.MIN_MAX_BOUNDS;

			foreach(Vector3 pnt in pnts)
			{
				float	val;

				val	=Vector3.Dot(pnt, t.mTexS) + t.mTexVecs.uOffset;

				if(val < mins)
				{
					mins	=val;
				}
				if(val > maxs)
				{
					maxs	=val;
				}

				val	=Vector3.Dot(pnt, t.mTexT) + t.mTexVecs.vOffset;

				if(val < mint)
				{
					mint	=val;
				}
				if(val > maxt)
				{
					maxt	=val;
				}
			}

			mExactMinS	=mins;
			mExactMaxS	=maxs;
			mExactMinT	=mint;
			mExactMaxT	=maxt;

			mins	=(float)Math.Floor(mins / TexInfo.LIGHTMAPSCALE);
			maxs	=(float)Math.Ceiling(maxs / TexInfo.LIGHTMAPSCALE);
			mint	=(float)Math.Floor(mint / TexInfo.LIGHTMAPSCALE);
			maxt	=(float)Math.Ceiling(maxt / TexInfo.LIGHTMAPSCALE);

			mTexMinS	=(int)Math.Floor(mins);
			mTexMinT	=(int)Math.Floor(mint);
			mTexSizeS	=(int)Math.Floor((maxs - mins));
			mTexSizeT	=(int)Math.Floor((maxt - mint));

			if(mTexSizeS > 4096 || mTexSizeT > 4096)
			{
				Debug.WriteLine("Huge ass face needs splitting up!");
			}
		}
	}

    struct TexInfoVectors
    {
	    public float    uScale, vScale;
	    public float    uOffset, vOffset;
    }

    struct TexInfo
    {
		public const UInt32		TEX_SPECIAL			=1;
		public const UInt32		FACE_HIDDEN			=2;
		public	const float		LIGHTMAPSCALE		=8.0f;
		public	const float		LIGHTMAPHALFSCALE	=LIGHTMAPSCALE / 2.0f;
		public	const float		LIGHTMAPDOUBLESCALE	=LIGHTMAPSCALE * 2.0f;

        public Vector3          mNormal;
        public TexInfoVectors   mTexVecs;
        public float            mRotationAngle;
        public string           mTexName;   //temporary
		public Vector3			mTexS, mTexT;
		public UInt32			mFlags;
		public Texture2D		mTexture;


		public void WriteToFile(BinaryWriter bw)
		{
			bw.Write(mTexName);
			bw.Write(mFlags);
			bw.Write(mTexS.X);
			bw.Write(mTexS.Y);
			bw.Write(mTexS.Z);
			bw.Write(mTexT.X);
			bw.Write(mTexT.Y);
			bw.Write(mTexT.Z);
		}
    }


	public struct VertexPositionTextureTexture
	{
		public	Vector3	Position;
		public	Vector2	Texture0;
		public	Vector2	Texture1;
	}


    public class Face
    {
        public	const float ON_EPSILON	=0.1f;
        private const float EDGE_LENGTH	=0.1f;
        private const float SCALECOS	=0.5f;
        private const float RANGESCALE	=0.5f;

        private Plane           mFacePlane;
        private TexInfo         mTexInfo;
        private List<Vector3>   mPoints;
		public	bool			mbVisible;
		public	UInt32			mFlags;
		private	Texture2D		mLightMap;
		private	LightInfo		mLightInfo;

        //for drawrings
        private VertexBuffer					mVertBuffer;
        private VertexPositionTextureTexture[]	mPosColor;
        private short[]							mIndexs;
        private IndexBuffer						mIndexBuffer;


		public Plane GetPlane()
		{
			return	mFacePlane;
		}
		
		
		public Face(List<Vector3> pnts)
		{
			mPoints	=new List<Vector3>();
			mPoints	=pnts;
			
			if(SetPlaneFromFace())
			{
				//init texinfo, set dibid, set texture pos
			}
			else
			{
				Debug.WriteLine("assgoblinry?");
			}
		}
		
		
		public Face(Plane p, Face f)
		{
			mPoints	=new List<Vector3>();
			SetFaceFromPlane(p, Bounds.MIN_MAX_BOUNDS);
			mFacePlane	=p;

			if(f != null)
			{
				mTexInfo	=f.mTexInfo;
				mFlags		=f.mFlags;
			}
		}


        public Face(Face f)
        {
            mPoints = new List<Vector3>();

            foreach(Vector3 pnt in f.mPoints)
            {
                mPoints.Add(pnt);
            }

            mFacePlane	=f.mFacePlane;
			mTexInfo	=f.mTexInfo;
			mFlags		=f.mFlags;
        }


        public Face(Face f, bool bInvert)
        {
            mPoints = new List<Vector3>();

            foreach(Vector3 pnt in f.mPoints)
            {
                mPoints.Add(pnt);
            }

            mFacePlane	=f.mFacePlane;
			mTexInfo	=f.mTexInfo;
			mFlags		=f.mFlags;

            if(bInvert)
            {
                mPoints.Reverse();
                mFacePlane.Normal *= -1.0f;
                mFacePlane.Dist *= -1.0f;
            }
        }
		
		
		private void MakeVBuffer(GraphicsDevice g)
		{
			int	i	=0;
			int	j	=0;
			
			//triangulate the brush face points
			mPosColor	=new VertexPositionTextureTexture[mPoints.Count];
			mIndexs		=new short[(3 + ((mPoints.Count - 3) * 3))];
			mIndexBuffer=new IndexBuffer(g, 2 * (3 + ((mPoints.Count - 3) * 3)),
							BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
			
			List<Vector2>	tcrds0;
			List<Vector2>	tcrds1;

			GetTexCoords0(out tcrds0);
			GetTexCoords1(out tcrds1);
			
			foreach(Vector3 pos in mPoints)
			{
				mPosColor[i].Position	=pos;
				mPosColor[i].Texture0	=tcrds0[i];
				mPosColor[i].Texture1	=tcrds1[i];
				i++;
			}
			
			for(i=1;i < mPoints.Count-1;i++)
			{
				//initial vertex
				mIndexs[j++]	=0;
				mIndexs[j++]	=(short)i;
				mIndexs[j++]	=(short)((i + 1) % mPoints.Count);
			}
			
			mIndexBuffer.SetData<short>(mIndexs, 0, mIndexs.Length);
			
			mVertBuffer	=new VertexBuffer(g, 28 * (mPosColor.Length),
							BufferUsage.None);
			
			//Set the vertex buffer data to the array of vertices.
			mVertBuffer.SetData<VertexPositionTextureTexture>(mPosColor);
		}


        public void Draw(GraphicsDevice g, Effect fx)
        {
			if(mPoints.Count < 3 || IsTiny())
			{
				Debug.Assert(false);
			}

            if(mPosColor == null || mPosColor.Length < 1)
            {
				Debug.Assert(false);
            }

			if((mFlags & TexInfo.FACE_HIDDEN) != 0)
			{
				return;
			}
			
			if(mLightMap != null)
			{				
				fx.Parameters["LightMap"].SetValue(mLightMap);
				fx.Parameters["LightMapEnabled"].SetValue(true);
				fx.Parameters["FullBright"].SetValue(false);
			}
			else
			{
				if((mTexInfo.mFlags & TexInfo.TEX_SPECIAL) != 0)
				{
					fx.Parameters["FullBright"].SetValue(true);
				}
				fx.Parameters["LightMapEnabled"].SetValue(false);
			}
			if(mTexInfo.mTexture != null)
			{
				fx.Parameters["Texture"].SetValue(mTexInfo.mTexture);
				fx.Parameters["TextureEnabled"].SetValue(true);
			}
			else
			{
				fx.Parameters["TextureEnabled"].SetValue(false);
			}
			fx.CommitChanges();
			
            g.Vertices[0].SetSource(mVertBuffer, 0, 28);
            g.Indices = mIndexBuffer;

            //g.RenderState.PointSize = 10;
            g.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,                  // index of the first vertex to draw
                mPosColor.Length,
                0,
                mIndexs.Length/3    // number of primitives
            );
        }


		bool	GetTextureAxis(out Vector3 xv, out Vector3 yv)
		{
			int		bestaxis;
			float	dot, best;
			int		i;
			
			best		=0;
			bestaxis	=0;
			
			for(i=0 ; i<3 ; i++)
			{
				dot	=(float)Math.Abs(VecIdx(mFacePlane.Normal, i));
				if(dot > best)
				{
					best		=dot;
					bestaxis	=i;
				}
			}

			switch(bestaxis)
			{
				case 0:						// X
					xv.X	=0.0f;
					xv.Y	=1.0f;
					xv.Z	=0.0f;

					yv.X	=0.0f;
					yv.Y	=0.0f;
					yv.Z	=1.0f;
					break;
				case 1:						// Y
					xv.X	=0.0f;
					xv.Y	=0.0f;
					xv.Z	=1.0f;

					yv.X	=1.0f;
					yv.Y	=0.0f;
					yv.Z	=0.0f;
					break;
				case 2:						// Z
					xv.X	=0.0f;
					xv.Y	=1.0f;
					xv.Z	=0.0f;

					yv.X	=1.0f;
					yv.Y	=0.0f;
					yv.Z	=0.0f;
					break;
				default:
					xv.X	=0.0f;
					xv.Y	=0.0f;
					xv.Z	=1.0f;

					yv.X	=0.0f;
					yv.Y	=-1.0f;
					yv.Z	=0.0f;
					Debug.WriteLine("GetTextureAxis: No Axis found.");
					return false;
			}

			return	true;
		}


        public void Expand()
        {
            SetFaceFromPlane(mFacePlane, Bounds.MIN_MAX_BOUNDS);
        }


        public bool IsValid()
        {
            return (mPoints.Count > 2);
        }


        public bool IsTiny()
        {
            int i, j;
            int edges   =0;
            for(i=0;i < mPoints.Count;i++)
            {
                j   =(i + 1) % mPoints.Count;

                Vector3 delta = mPoints[j] - mPoints[i];
                float len = delta.Length();

                if(len > EDGE_LENGTH)
                {
                    edges++;
                    if(edges == 3)
                    {
                        return  false;
                    }
                }
            }
            return  true;
        }


        private bool SetPlaneFromFace()
        {
            int     i;

            //catches colinear points now
			for(i=0;i < mPoints.Count;i++)
			{
                //gen a plane normal from the cross of edge vectors
                Vector3 v1  =mPoints[i] - mPoints[(i + 1) % mPoints.Count];
                Vector3 v2  =mPoints[(i + 2) % mPoints.Count] - mPoints[(i + 1) % mPoints.Count];

                mFacePlane.Normal   =Vector3.Cross(v1, v2);

                if(!mFacePlane.Normal.Equals(Vector3.Zero))
                {
                    break;
                }
                //try the next three if there are three
            }
            if(i >= mPoints.Count)
            {
                //need a talky flag
                //in some cases this isn't worthy of a warning
                //Debug.WriteLine("Face with no normal!");
                return  false;
            }

            mFacePlane.Normal.Normalize();
            mFacePlane.Dist =Vector3.Dot(mPoints[1], mFacePlane.Normal);
            mTexInfo.mNormal =mFacePlane.Normal;

            return true;
        }


        //parse map file stuff
        public Face(string szLine)
        {
            mPoints = new List<Vector3>();
            //gank (
            szLine.TrimStart('(');

            szLine.Trim();

            string  []tokens    =szLine.Split(' ');

            List<float> numbers =new List<float>();

            //grab all the numbers out
            foreach(string tok in tokens)
            {
                //skip ()
                if(tok[0] == '(' || tok[0] == ')')
                {
                    continue;
                }

                //grab tex name if avail
				if(tok[0] == '*')
				{
					mTexInfo.mFlags	|=TexInfo.TEX_SPECIAL;
					mTexInfo.mTexName	=tok.Substring(1);
					continue;
				}
				else if(tok[0] == '#')
				{
					mTexInfo.mFlags	|=TexInfo.TEX_SPECIAL;
					mTexInfo.mTexName	=tok;
					continue;
				}
                else if(char.IsLetter(tok, 0))
                {
                    mTexInfo.mTexName = tok;
                    continue;
                }

                float   num;

				//NOTE: This is how TryParse is intended to be used -- Kyth
                if(Single.TryParse(tok, out num))
                {
                    //rest are numbers
					//numbers.Add(System.Convert.ToSingle(tok));
					numbers.Add(num);
                }
            }

            //deal with the numbers
            //invert x and swap y and z
            //to convert to left handed
            mPoints.Add(new Vector3(-numbers[0], numbers[2], numbers[1]));
            mPoints.Add(new Vector3(-numbers[3], numbers[5], numbers[4]));
            mPoints.Add(new Vector3(-numbers[6], numbers[8], numbers[7]));

            mTexInfo.mTexVecs.uOffset   =numbers[9];
            mTexInfo.mTexVecs.vOffset   =numbers[10];
            mTexInfo.mRotationAngle     =numbers[11];
            mTexInfo.mTexVecs.uScale    =numbers[12];
            mTexInfo.mTexVecs.vScale    =numbers[13];

            SetPlaneFromFace();

			//do quake style texture axis stuff
			Vector3	vecs, vect;
			GetTextureAxis(out vecs, out vect);

			Debug.Assert(vecs != Vector3.Zero);

			if(mTexInfo.mTexVecs.uScale == 0.0f)
			{
				mTexInfo.mTexVecs.uScale	=1.0f;
			}
			if(mTexInfo.mTexVecs.vScale == 0.0f)
			{
				mTexInfo.mTexVecs.vScale	=1.0f;
			}

			//test
			if(mTexInfo.mTexVecs.uScale < 0.0f)
			{
				mTexInfo.mTexVecs.uScale	=-mTexInfo.mTexVecs.uScale;
			}
			if(mTexInfo.mTexVecs.vScale < 0.0f)
			{
				mTexInfo.mTexVecs.vScale	=-mTexInfo.mTexVecs.vScale;
			}

			float	ang, sinv, cosv, ns, nt;
			int		sv, tv;

			if(mTexInfo.mRotationAngle == 0)
				{ sinv = 0 ; cosv = 1; }
			else if(mTexInfo.mRotationAngle == 90)
				{ sinv = 1 ; cosv = 0; }
			else if(mTexInfo.mRotationAngle == 180)
				{ sinv = 0 ; cosv = -1; }
			else if(mTexInfo.mRotationAngle == 270)
				{ sinv = -1 ; cosv = 0; }
			else
			{	
				ang = mTexInfo.mRotationAngle / 180.0f * (float)Math.PI;
				sinv = (float)Math.Sin(ang);
				cosv = (float)Math.Cos(ang);
			}
		
			if(vecs.X != 0.0f)
			{
				sv = 0;
			}
			else if(vecs.Y != 0.0f)
			{
				sv = 1;
			}
			else
			{
				sv = 2;
			}
			if(vect.X != 0.0f)
			{
				tv = 0;
			}
			else if(vect.Y != 0.0f)
			{
				tv = 1;
			}
			else
			{
				tv = 2;
			}

			ns	=cosv * VecIdx(vecs, sv) - sinv * VecIdx(vecs, tv);
			nt	=sinv * VecIdx(vecs, sv) + cosv * VecIdx(vecs, tv);
			VecIdxAssign(ref vecs, sv, ns);
			VecIdxAssign(ref vecs, tv, nt);
						
			ns	=cosv * VecIdx(vect, sv) - sinv * VecIdx(vect, tv);
			nt	=sinv * VecIdx(vect, sv) + cosv * VecIdx(vect, tv);
			VecIdxAssign(ref vect, sv, ns);
			VecIdxAssign(ref vect, tv, nt);

			vecs	=vecs * (1.0f / mTexInfo.mTexVecs.uScale);
			vect	=vect * (1.0f / mTexInfo.mTexVecs.vScale);
			Debug.Assert(vecs != Vector3.Zero);

			mTexInfo.mTexS	=vecs;
			mTexInfo.mTexT	=vect;
        }


		public void BuildVertexInfo(GraphicsDevice g)
		{
			if(mPoints.Count < 3 || IsTiny())
			{
				Debug.Assert(false);
			}
			MakeVBuffer(g);
		}


		public void WriteToFile(BinaryWriter bw)
		{
			mTexInfo.WriteToFile(bw);

			bw.Write(mPoints.Count);

			for(int i=0;i < mPoints.Count;i++)
			{
				bw.Write(mPosColor[i].Position.X);
				bw.Write(mPosColor[i].Position.Y);
				bw.Write(mPosColor[i].Position.Z);

				bw.Write(mPosColor[i].Texture0.X);
				bw.Write(mPosColor[i].Texture0.Y);
				bw.Write(mPosColor[i].Texture1.X);
				bw.Write(mPosColor[i].Texture1.Y);
			}

			//write indexs length
			bw.Write(mIndexs.GetLength(0));

			//write indexs
			for(int i=0;i < mIndexs.GetLength(0);i++)
			{
				bw.Write(mIndexs[i]);
			}

			//lightmapped?
			bw.Write(mLightMap != null);

			//write lightmap
			if(mLightMap != null)
			{
				bw.Write(mLightMap.Width);
				bw.Write(mLightMap.Height);

				Color[]	existing	=new Color[mLightMap.Width * mLightMap.Height];

				//grab data
				mLightMap.GetData<Color>(existing);

				for(int i=0;i < (mLightMap.Width * mLightMap.Height);i++)
				{
					bw.Write(existing[i].PackedValue);
				}
			}
		}


		private	static float VecIdx(Vector3 v, int idx)
		{
			if(idx == 0)
			{
				return	v.X;
			}
			else if(idx == 1)
			{
				return	v.Y;
			}
			return	v.Z;
		}


		private	static void VecIdxAssign(ref Vector3 v, int idx, float val)
		{
			if(idx == 0)
			{
				v.X	=val;
			}
			else if(idx == 1)
			{
				v.Y	=val;
			}
			else
			{
				v.Z	=val;
			}
		}


        public void GetFaceMinMaxDistancesFromPlane(Plane p, ref float front, ref float back)
        {
            float d;

            foreach(Vector3 pnt in mPoints)
            {
                d =Vector3.Dot(pnt, p.Normal) - p.Dist;

                if(d > front)
                {
                    front = d;
                }
                else if(d < back)
                {
                    back = d;
                }
            }
        }


        public void SetFaceFromPlane(Plane p, float dist)
        {
            float   v;
            Vector3 vup, vright, org;

            //find the major axis of the plane normal
            vup.X = vup.Y = 0.0f;
            vup.Z = 1.0f;
            if((System.Math.Abs(p.Normal.Z) > System.Math.Abs(p.Normal.X))
                && (System.Math.Abs(p.Normal.Z) > System.Math.Abs(p.Normal.Y)))
            {
                vup.X = 1.0f;
                vup.Y = vup.Z = 0.0f;
            }

            v = Vector3.Dot(vup, p.Normal);

            vup = vup + p.Normal * -v;
            vup.Normalize();

            org = p.Normal * p.Dist;

            vright  =Vector3.Cross(vup, p.Normal);

            vup *= dist;
            vright *= dist;

            mPoints.Clear();

            mPoints.Add(org - vright + vup);
            mPoints.Add(org + vright + vup);
            mPoints.Add(org + vright - vup);
            mPoints.Add(org - vright - vup);
        }


		public void GetSplitInfo(Face		f,
								 out int	pointsOnFront,
								 out int	pointsOnBack,
								 out int	pointsOnPlane)
		{
			pointsOnPlane = pointsOnFront = pointsOnBack = 0;
			foreach(Vector3 pnt in mPoints)
			{
				float	dot	=Vector3.Dot(pnt, mFacePlane.Normal) - mFacePlane.Dist;

                if(dot > ON_EPSILON)
                {
                    pointsOnFront++;
                }
                else if(dot < -ON_EPSILON)
                {
                    pointsOnBack++;
                }
                else
                {
					pointsOnPlane++;
                }
			}
		}


        //clip this face in front or behind face f
        public void ClipByFace(Face f, bool bFront)
        {
            List<Vector3> frontSide = new List<Vector3>();
            List<Vector3> backSide = new List<Vector3>();

            for(int i = 0;i < mPoints.Count;i++)
            {
                int j = (i + 1) % mPoints.Count;
                Vector3 p1, p2;

                p1 = mPoints[i];
                p2 = mPoints[j];

                float dot = Vector3.Dot(p1, f.mFacePlane.Normal)
                                    - f.mFacePlane.Dist;
                float dot2 = Vector3.Dot(p2, f.mFacePlane.Normal)
                                    - f.mFacePlane.Dist;

                if(dot > ON_EPSILON)
                {
                    frontSide.Add(p1);
                }
                else if(dot < -ON_EPSILON)
                {
                    backSide.Add(p1);
                }
                else
                {
                    frontSide.Add(p1);
                    backSide.Add(p1);
                    continue;
                }

                //skip ahead if next point is onplane
                if(dot2 < ON_EPSILON && dot2 > -ON_EPSILON)
                {
                    continue;
                }

                //skip ahead if next point is on same side
                if(dot2 > ON_EPSILON && dot > ON_EPSILON)
                {
                    continue;
                }

                //skip ahead if next point is on same side
                if(dot2 < -ON_EPSILON && dot < -ON_EPSILON)
                {
                    continue;
                }

                float splitRatio = dot / (dot - dot2);
                Vector3 mid = p1 + (splitRatio * (p2 - p1));

                frontSide.Add(mid);
                backSide.Add(mid);
            }

            //dump our point list
            mPoints.Clear();

            //copy in the front side
            if (bFront)
            {
                mPoints = frontSide;
            }
            else
            {
                mPoints = backSide;
            }

            if(!SetPlaneFromFace())
            {
                //whole face was clipped away, no big deal
                mPoints.Clear();
            }
        }


		private	void	GetTexCoords0(out List<Vector2> coords)
		{
			coords	=new List<Vector2>();

			float	minS, minT;

			minS	=Bounds.MIN_MAX_BOUNDS;
			minT	=Bounds.MIN_MAX_BOUNDS;

			//calculate the min values for s and t
			foreach(Vector3 pnt in mPoints)
			{
				float	d;
				d	=Vector3.Dot(mTexInfo.mTexS, pnt);

				if(d < minS)
				{
					minS	=d;
				}

				d	=Vector3.Dot(mTexInfo.mTexT, pnt);
				if(d < minT)
				{
					minT	=d;
				}
			}

			float	shiftU	=-minS + TexInfo.LIGHTMAPSCALE;
			float	shiftV	=-minT + TexInfo.LIGHTMAPSCALE;

			foreach(Vector3 pnt in mPoints)
			{
				Vector2	crd;
				crd.X	=Vector3.Dot(mTexInfo.mTexS, pnt);
				crd.Y	=Vector3.Dot(mTexInfo.mTexT, pnt);

				crd.X	+=shiftU;
				crd.Y	+=shiftV;

				if(mTexInfo.mTexture != null)
				{
					crd.X	/=mTexInfo.mTexture.Width;
					crd.Y	/=mTexInfo.mTexture.Height;
				}

				coords.Add(crd);
			}
		}


		private	void	GetTexCoords1(out List<Vector2> coords)
		{
			coords	=new List<Vector2>();

			float	minS, minT;

			minS	=Bounds.MIN_MAX_BOUNDS;
			minT	=Bounds.MIN_MAX_BOUNDS;

			//calculate the min values for s and t
			foreach(Vector3 pnt in mPoints)
			{
				float	d;
				d	=Vector3.Dot(mTexInfo.mTexS, pnt);

				if(d < minS)
				{
					minS	=d;
				}

				d	=Vector3.Dot(mTexInfo.mTexT, pnt);
				if(d < minT)
				{
					minT	=d;
				}
			}

			float	shiftU	=-minS + TexInfo.LIGHTMAPHALFSCALE;
			float	shiftV	=-minT + TexInfo.LIGHTMAPHALFSCALE;

			foreach(Vector3 pnt in mPoints)
			{
				Vector2	crd;
				crd.X	=Vector3.Dot(mTexInfo.mTexS, pnt);
				crd.Y	=Vector3.Dot(mTexInfo.mTexT, pnt);

				crd.X	+=shiftU;
				crd.Y	+=shiftV;

				//scale down to a zero to one range
				if(mLightMap != null)
				{
					crd.X	/=((float)mLightInfo.mTexSizeS * TexInfo.LIGHTMAPSCALE);
					crd.Y	/=((float)mLightInfo.mTexSizeT * TexInfo.LIGHTMAPSCALE);

					//ratio between the size
					//of the lightmap and the padded
					//texture2d used in rendering
					crd.X	/=((float)mLightMap.Width / ((float)mLightInfo.mTexSizeS));
					crd.Y	/=((float)mLightMap.Height / ((float)mLightInfo.mTexSizeT));
				}

				coords.Add(crd);
			}
		}


		public Texture2D GetLightMap()
		{
			return	mLightMap;
		}


		private	void CalcSurfacePoints(BspNode root)
		{
			float	mids, midt;
			Vector3	faceMid;
			int		s, t, w, c, h, i, startS, startT, step;

			mids	=(mLightInfo.mExactMaxS + mLightInfo.mExactMinS) / 2.0f;
			midt	=(mLightInfo.mExactMaxT + mLightInfo.mExactMinT) / 2.0f;

			faceMid	=mLightInfo.mTexOrg +
						(mLightInfo.mTexToWorldS * mids) +
						(mLightInfo.mTexToWorldT * midt);

			w		=mLightInfo.mTexSizeS + 1;
			h		=mLightInfo.mTexSizeT + 1;
			startS	=(int)(mLightInfo.mTexMinS * TexInfo.LIGHTMAPSCALE);
			startT	=(int)(mLightInfo.mTexMinT * TexInfo.LIGHTMAPSCALE);
			step	=(int)TexInfo.LIGHTMAPSCALE;

			mLightInfo.mSurface				=new Vector3[w * h];
			mLightInfo.mNumSurfacePoints	=w * h;

			bool	bAllInSolid	=true;

			for(t=c=0;t < h;t++)
			{
				for(s=0;s < w;s++,c++)
				{
					float	us	=startS + s * step;
					float	ut	=startT + t * step;

					//here quake / genesis can guarantee
					//that the face will for the mostpart
					//be in empty space.  I cannot guarantee
					//that as I don't do any sort of hidden
					//leaf removal.  So I use a classify
					//instead of a raycast, and try only
					//axial shifts in s and t
					for(i=0;i < 8;i++)
					{
						mLightInfo.mSurface[c]
							=mLightInfo.mTexOrg + mLightInfo.mTexToWorldS * us
								+mLightInfo.mTexToWorldT * ut;

						bool	solid	=root.ClassifyPoint(mLightInfo.mSurface[c]);
						if(!solid)
						{
							bAllInSolid	=false;
							break;
						}
						if(i < 4)
						{
							if(i < 2)	//try a shift left
							{
								us	-=TexInfo.LIGHTMAPSCALE;
							}
							else if(i < 3)
							{
								//recenter + half
								us	+=TexInfo.LIGHTMAPDOUBLESCALE;
								us	+=TexInfo.LIGHTMAPSCALE;
							}
							else
							{
								us	+=TexInfo.LIGHTMAPSCALE;
							}
						}
						else
						{
							if(i < 5)
							{
								//recenter
								us	-=TexInfo.LIGHTMAPDOUBLESCALE;
								ut	-=TexInfo.LIGHTMAPSCALE;
							}
							else if(i < 6)	//try shift down
							{
								ut	-=TexInfo.LIGHTMAPSCALE;
							}
							else if(i < 7)	
							{
								ut	+=TexInfo.LIGHTMAPDOUBLESCALE;
								ut	+=TexInfo.LIGHTMAPSCALE;
							}
							else
							{
								ut	+=TexInfo.LIGHTMAPSCALE;
							}
						}
					}
					Debug.Assert(!float.IsNaN(mLightInfo.mSurface[c].X));
					Debug.Assert(!float.IsNaN(mLightInfo.mSurface[c].Y));
					Debug.Assert(!float.IsNaN(mLightInfo.mSurface[c].Z));
				}
			}
			if(bAllInSolid)
			{
				this.mFlags	|=TexInfo.FACE_HIDDEN;
			}
		}


		private	void	SingleLightFace(BspNode root, Vector3 lightPos, float lightVal)
		{
			int		i, c;
			float	dist	=Vector3.Dot(lightPos, mFacePlane.Normal) - mFacePlane.Dist;
			bool	bNewSamples	=false;

			if(dist <= 0.0f)
			{
				return;
			}

			if(dist > lightVal)
			{
				return;
			}
			int	size	=(mLightInfo.mTexSizeT + 1) * (mLightInfo.mTexSizeS + 1);

			if(mLightInfo.mSamples == null)
			{
				bNewSamples			=true;
				mLightInfo.mSamples	=new float[size];

				for(i=0;i < size;i++)
				{
					mLightInfo.mSamples[i]	=0.0f;
				}
			}

			bool	hit	=false;

			for(c=0;c < mLightInfo.mNumSurfacePoints;c++)
			{
				if(!root.RayCastBool(lightPos, mLightInfo.mSurface[c]))
				{
					continue;
				}

				Vector3	incoming	=lightPos - mLightInfo.mSurface[c];
				incoming.Normalize();

				float	angle	=Vector3.Dot(incoming, mFacePlane.Normal);
				angle	=(1.0f - SCALECOS) + SCALECOS * angle;
				float	add	=lightVal - dist;

				add	*=angle;
				if(add < 0.0f)
				{
					continue;
				}
				mLightInfo.mSamples[c]	+=add;
				Debug.Assert(!float.IsNaN(mLightInfo.mSamples[c]));
				if(mLightInfo.mSamples[c] > 1.0f)
				{
					hit	=true;
				}
			}
			
			if(!hit && bNewSamples)
			{
				//trash the lightmap, too dim
				mLightInfo.mSamples	=null;
			}
		}


		public	void LightFace(GraphicsDevice gd, BspNode root,
								Vector3 lightPos, float lightVal)
		{
			int	c, t, s;

			if((mTexInfo.mFlags & TexInfo.TEX_SPECIAL) != 0)
			{
				return;
			}

			mLightInfo	=new LightInfo();

			mLightInfo.CalcFaceVectors(mFacePlane, mTexInfo);
			mLightInfo.CalcFaceExtents(mPoints, mTexInfo);
			CalcSurfacePoints(root);

			SingleLightFace(root, lightPos, lightVal);

			if(mLightInfo.mSamples == null)
			{
				return;
			}

			int	xPadded	=(int)Math.Pow(2, Math.Ceiling(Math.Log(mLightInfo.mTexSizeS + 1) / Math.Log(2)));
			int	yPadded	=(int)Math.Pow(2, Math.Ceiling(Math.Log(mLightInfo.mTexSizeT + 1) / Math.Log(2)));

			Color[]	lm	=new Color[xPadded * yPadded];

			for(t=0,c=0;t < (mLightInfo.mTexSizeT + 1);t++)
			{
				for(s=0;s < (mLightInfo.mTexSizeS + 1);s++, c++)
				{
					float	total	=mLightInfo.mSamples[c];

					total	*=RANGESCALE;
					if(total > 255.0f)
					{
						total	=255.0f;
					}
					Debug.Assert(total >= 0.0f);

					lm[(t * xPadded) + s]	=new Color((byte)total, (byte)total, (byte)total);
				}
			}

			//clamp the colors out beyond the real border
			for(t=0;t < mLightInfo.mTexSizeT + 1;t++)
			{
				Color	padColor	=lm[(t * xPadded) + mLightInfo.mTexSizeS];
				for(s=mLightInfo.mTexSizeS;s < xPadded;s++)
				{
					lm[(t * xPadded) + s]	=padColor;
				}
			}
			for(t=mLightInfo.mTexSizeT + 1;t < yPadded;t++)
			{
				for(s=0;s < xPadded;s++)
				{
					Color	padXColor	=lm[((t - 1) * xPadded) + s];
					lm[(t * xPadded) + s]	=padXColor;
				}
			}


			if(mLightMap == null)
			{
				mLightMap	=new Texture2D(gd, xPadded, yPadded, 0, TextureUsage.None, SurfaceFormat.Color);

				mLightMap.SetData<Color>(lm);
			}
			else
			{
				Color[]	existing	=new Color[xPadded * yPadded];

				//grab data
				mLightMap.GetData<Color>(existing);

				for(t=0,c=0;t < (mLightInfo.mTexSizeT + 1);t++)
				{
					for(s=0;s < (mLightInfo.mTexSizeS + 1);s++, c++)
					{
						int	r	=lm[(t * xPadded) + s].R + existing[(t * xPadded) + s].R;
						int	g	=lm[(t * xPadded) + s].G + existing[(t * xPadded) + s].G;
						int	b	=lm[(t * xPadded) + s].B + existing[(t * xPadded) + s].B;
						
						if(r > 255)
						{
							r	=255;
						}
						if(g > 255)
						{
							g	=255;
						}
						if(b > 255)
						{
							b	=255;
						}
						existing[(t * xPadded) + s]	=new Color((byte)r, (byte)g, (byte)b);
					}
				}

				//put existing back in
				mLightMap.SetData<Color>(existing);
			}
		}


		public void AddToBounds(ref Bounds bnd)
		{
			foreach(Vector3 pnt in mPoints)
			{
				bnd.AddPointToBounds(pnt);
			}
		}

		#region Unused
		public int GetSurfPoints(out Vector3[] surfPoints)
		{
			if(mLightMap != null)
			{
				surfPoints	=mLightInfo.mSurface;
				return	mLightInfo.mNumSurfacePoints;
			}
			else
			{
				surfPoints	=null;
				return 0;
			}
		}


		public bool MiddleInSolid(BspNode root)
		{
			float	mids, midt;
			Vector3	faceMid;

			mids	=(mLightInfo.mExactMaxS + mLightInfo.mExactMinS) / 2.0f;
			midt	=(mLightInfo.mExactMaxT + mLightInfo.mExactMinT) / 2.0f;

			faceMid	=mLightInfo.mTexOrg +
						(mLightInfo.mTexToWorldS * mids) +
						(mLightInfo.mTexToWorldT * midt);

			return	root.ClassifyPoint(faceMid);
		}


		public void SetTexturePointers(List<KeyValuePair<string, Texture2D>> tl)
		{
			foreach(KeyValuePair<string, Texture2D> k in tl)
			{
				if(k.Key == mTexInfo.mTexName)
				{
					mTexInfo.mTexture	=k.Value;
					return;
				}
			}
		}


		public void GetTexFileNames(ref List<string> fn)
		{
			if(mTexInfo.mTexName != null)
			{
				if(!fn.Contains(mTexInfo.mTexName))
				{
					fn.Add(mTexInfo.mTexName);
				}
			}
		}


        private bool IsPointBehindFacePlane(Vector3 pnt)
        {
            float dot = Vector3.Dot(pnt, mFacePlane.Normal)
                                - mFacePlane.Dist;

            return (dot < -ON_EPSILON);
        }


        public bool IsAnyPointBehindAllPlanes(Face f, List<Plane> planes)
        {
            foreach(Vector3 pnt in f.mPoints)
            {
                foreach(Plane p in planes)
                {
                    if(!IsPointBehindFacePlane(pnt))
                    {
                    }
                }
            }
            return false;
		}
		#endregion
	}
}
