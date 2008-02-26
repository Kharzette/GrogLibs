using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace BuildMap
{
    struct Plane
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

    class Face
    {
        private const float ON_EPSILON = 0.1f;
        private const float EDGE_LENGTH = 0.1f;

        private Plane           mFacePlane;
        private TexInfo         mTexInfo;
        private List<Vector3>   mPoints;

        //for drawrings
        private VertexBuffer            mVertBuffer;
        private VertexPositionNormalTexture[]   mPosColor;
        private short[]                 mIndexs;
        private IndexBuffer             mIndexBuffer;


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
            SetFaceFromPlane(p, 8000.0f);
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
            SetFaceFromPlane(mFacePlane, 8000);
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

                if(Single.TryParse(tok, out num))
                {
                    //rest are numbers
                    numbers.Add(System.Convert.ToSingle(tok));
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


        private void SetFaceFromPlane(Plane p, float dist)
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
    }
}