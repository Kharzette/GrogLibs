using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace BuildMap
{
    public struct Plane
    {
        public Vector3	Normal;
        public float	Dist;
    }

    struct TexInfoVectors
    {
	    public float    uScale, vScale;
	    public float    uOffset, vOffset;
    }

    struct TexInfo
    {
        public Vector3          mNormal;
        public TexInfoVectors   mTexVecs;
        public float            mRotationAngle;
        public string           mTexName;   //temporary

        //need a texture reference here
    }

    public class Face
    {
        private const float ON_EPSILON	=0.1f;
        private const float EDGE_LENGTH	=0.1f;

        private Plane           mFacePlane;
        private TexInfo         mTexInfo;
        private List<Vector3>   mPoints;
		public	bool			mbVisible;
		public	UInt32			mFlags;
		private	UInt32[]		mLightMap;

        //for drawrings
        private VertexBuffer					mVertBuffer;
        private VertexPositionNormalTexture[]   mPosColor;
        private short[]							mIndexs;
        private IndexBuffer						mIndexBuffer;


        public Plane GetPlane()
        {
            return  mFacePlane;
        }


        public Face(List<Vector3> pnts)
        {
            mPoints = new List<Vector3>();
            mPoints = pnts;

            if(SetPlaneFromFace())
            {
                //init texinfo, set dibid, set texture pos
            }
            else
            {
                Debug.WriteLine("assgoblinry?");
            }
        }


        public Face(Plane p)
        {
            mPoints = new List<Vector3>();
            SetFaceFromPlane(p, Bounds.MIN_MAX_BOUNDS);
			mFacePlane	=p;
        }


        public Face(Face f)
        {
            mPoints = new List<Vector3>();

            foreach(Vector3 pnt in f.mPoints)
            {
                mPoints.Add(pnt);
            }

            mFacePlane = f.mFacePlane;
        }


        public Face(Face f, bool bInvert)
        {
            mPoints = new List<Vector3>();

            foreach(Vector3 pnt in f.mPoints)
            {
                mPoints.Add(pnt);
            }

            mFacePlane = f.mFacePlane;

            if(bInvert)
            {
                mPoints.Reverse();
                mFacePlane.Normal *= -1.0f;
                mFacePlane.Dist *= -1.0f;
            }
        }


        private void MakeVBuffer(GraphicsDevice g, Color c)
        {
            int i = 0;
            int j = 0;


            //triangulate the brush face points
//            mPosColor = new VertexPositionColor[3 + ((mPoints.Count - 3) * 3)];
            mPosColor = new VertexPositionNormalTexture[mPoints.Count];
            mIndexs = new short[(3 + ((mPoints.Count - 3) * 3))];
            mIndexBuffer =new IndexBuffer(g, 2 * (3 + ((mPoints.Count - 3) * 3)),
                BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

            foreach (Vector3 pos in mPoints)
            {
                mPosColor[i].Position = pos;
                mPosColor[i++].Normal = mFacePlane.Normal;
            }

            for(i = 1;i < mPoints.Count - 1;i++)
            {
                //initial vertex
                mIndexs[j++] = 0;
                mIndexs[j++] = (short)i;
                mIndexs[j++] = (short)((i + 1) % mPoints.Count);
            }

            mIndexBuffer.SetData<short>(mIndexs, 0, mIndexs.Length);

            mVertBuffer = new VertexBuffer(g,
                VertexPositionNormalTexture.SizeInBytes * (mPosColor.Length),
                BufferUsage.None);

            // Set the vertex buffer data to the array of vertices.
            mVertBuffer.SetData<VertexPositionNormalTexture>(mPosColor);
        }


        public void Draw(GraphicsDevice g, Color c)
        {
			if(mPoints.Count < 3)
			{
				return;
			}

            if(mPosColor == null || mPosColor.Length < 1)
            {
                MakeVBuffer(g, c);
            }

            g.Vertices[0].SetSource(mVertBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
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
                if(char.IsLetter(tok, 0))
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


		public	void LightFace(BspNode root, Vector3 lightPos, float lightVal, Vector3 color)
		{
			//figure out the face extents
			Bounds	bnd	=new Bounds();

			AddToBounds(ref bnd);

			//get the top left point in the plane
			//project maxs onto the face plane
			float	d	=Vector3.Dot(bnd.mMaxs, mFacePlane.Normal) - mFacePlane.Dist;

			Vector3	delta	=mFacePlane.Normal * d;

			Vector3	maxInPlane	=bnd.mMaxs - delta;

			d	=Vector3.Dot(bnd.mMins, mFacePlane.Normal) - mFacePlane.Dist;

			delta	=mFacePlane.Normal * d;

			Vector3	minInPlane	=bnd.mMins - delta;

			//make sure the light is in range
			if((Vector3.Distance(lightPos, minInPlane) > lightVal)
				&&(Vector3.Distance(lightPos, maxInPlane) > lightVal))
			{
				return;
			}

			//use the delta of the two in plane minmax points
			//to generate a cross with the face's normal
			//then add them together and normalize to get
			//a lightmap axis.  Then cross with the normal
			//to get the other axis
			Vector3	deltaMinMax	=maxInPlane - minInPlane;

			Vector3	deltaMinMaxUnit	=deltaMinMax;
			deltaMinMaxUnit.Normalize();

			Vector3 YAxis	=Vector3.Cross(deltaMinMaxUnit, mFacePlane.Normal);
			YAxis.Normalize();
			YAxis	+=deltaMinMaxUnit;
			YAxis.Normalize();

			Vector3 XAxis	=Vector3.Cross(YAxis, mFacePlane.Normal);
			XAxis.Normalize();

			//now find the number of lightmap units along
			//both axis vectors in the face extents
			float	XExtents	=-Vector3.Dot(XAxis, deltaMinMax);
			float	YExtents	=-Vector3.Dot(YAxis, deltaMinMax);

			XExtents	=Math.Abs(XExtents);
			YExtents	=Math.Abs(YExtents);

			//we'll do a light point every 8 world units
			int	numXPoints	=(int)XExtents / 8 + 1;
			int	numYPoints	=(int)YExtents / 8 + 1;

			//scale axis vectors
			XAxis	*=8.0f;
			YAxis	*=8.0f;

			if(mLightMap == null)
			{
				mLightMap	=new UInt32[numXPoints * numYPoints];
			}

			for(int y=0;y < numYPoints;y++)
			{
				for(int x=0;x < numXPoints;x++)
				{
					Vector3	lmPoint	=maxInPlane + (XAxis * x);
					lmPoint	+=(YAxis * y);

					//shoot a ray from lmPoint to the light
					Vector3	impact	=root.RayCast(lmPoint, lightPos);

					//hit something along the way?
					if(impact == Vector3.Zero)
					{
						float	dist	=Vector3.Distance(lmPoint, lightPos);
						Vector3	clr		=color * dist;
						
						clr	/=lightVal;

						UInt32	rB, bB, gB;
						rB	=(UInt32)(clr.X * 255.0f);
						bB	=(UInt32)(clr.Y * 255.0f);
						gB	=(UInt32)(clr.Z * 255.0f);
						if(rB > 255)
						{
							rB	=255;
						}
						if(bB > 255)
						{
							bB	=255;
						}
						if(gB > 255)
						{
							gB	=255;
						}
						mLightMap[(y * numXPoints) + x]	=(byte)rB;
						mLightMap[(y * numXPoints) + x]	|=(bB << 8);
						mLightMap[(y * numXPoints) + x]	|=(gB << 16);
						mLightMap[(y * numXPoints) + x]	|=((UInt32)255 << 24);
					}
				}
			}
		}


		public void AddToBounds(ref Bounds bnd)
		{
			foreach(Vector3 pnt in mPoints)
			{
				bnd.AddPointToBounds(pnt);
			}
		}
    }
}