using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BuildMap
{
    public class Brush
    {
        private List<Face>  mFaces;

        public Brush()
        {
            mFaces  =new List<Face>();
        }


        public Brush(Brush b)
        {
            mFaces = new List<Face>();

            foreach(Face f in b.mFaces)
            {
                mFaces.Add(new Face(f));
            }
        }

        public void MakeFaceFromMapLine(string szLine)
        {
            mFaces.Add(new Face(szLine));
        }


		public void GetThriceLightmaps(out List<Texture2D> list)
		{
			list	=new List<Texture2D>();
			list.Add(mFaces[3].GetLightMap());
			list.Add(mFaces[4].GetLightMap());
			list.Add(mFaces[5].GetLightMap());
		}


		public int GetFirstSurface(out Vector3[] surfPoints)
		{
			foreach(Face f in mFaces)
			{
				int	np	=f.GetSurfPoints(out surfPoints);
				if(np > 0)
				{
					return	np;
				}
			}
			surfPoints	=null;
			return	0;
		}


        public void Draw(GraphicsDevice g, Effect fx)
        {
			foreach (Face f in mFaces)
			{
				f.Draw(g, fx);
			}
        }


        public bool IsValid()
        {
            return (mFaces.Count >= 4);
        }


		public bool GetSplitPlane(int fNum, out Plane p)
		{
			if(fNum < mFaces.Count)
			{
				p	=mFaces[fNum].GetPlane();
				return	true;
			}
			p	=new Plane();	//value type, gotta do something
			return	false;
		}


		public Plane GetCrappySplittingPlane()
		{
			foreach(Face f in mFaces)
			{
				Plane	p	=f.GetPlane();

				//find an axial
				if(Math.Abs(Vector3.Dot(p.Normal, Vector3.Left)) == 1.0f)
				{
					return	p;
				}
				else if (Math.Abs(Vector3.Dot(p.Normal, Vector3.Forward)) == 1.0f)
				{
					return	p;
				}
				else if (Math.Abs(Vector3.Dot(p.Normal, Vector3.Up)) == 1.0f)
				{
					return	p;
				}
			}

			//just return the first one, whatever
			return	mFaces[0].GetPlane();
		}


		//return the best score in the brush
		public float GetBestSplittingFaceScore(List<Brush> brushList)
		{
			float	BestScore	=696969.0f;	//0.5f is the goal

			foreach(Face f in mFaces)
			{
				Plane	p	=f.GetPlane();

				//test balance
				List<Brush>	copyList	=new List<Brush>(brushList);
				List<Brush> front		=new List<Brush>();
				List<Brush> back		=new List<Brush>();

				foreach(Brush b2 in copyList)
				{
					Brush bf, bb;

					b2.SplitBrush(f, out bf, out bb);

					if(bb != null)
					{
						back.Add(bb);
					}
					if(bf != null)
					{
						front.Add(bf);
					}
				}

				if(front.Count !=0 && back.Count != 0)
				{
					float	score;
					if(front.Count > back.Count)
					{
						score	=(float)front.Count / back.Count;
					}
					else
					{
						score	=(float)back.Count / front.Count;
					}

					if(score < BestScore)
					{
						BestScore	=score;
					}
				}
			}

			return	BestScore;
		}


		//return the best plane in the brush
		public Face GetBestSplittingFace(List<Brush> brushList)
		{
			float	BestScore	=696969.0f;	//0.5f is the goal
			int		BestIndex	=0;

			foreach(Face f in mFaces)
			{
				Plane	p	=f.GetPlane();

				//test balance
				List<Brush>	copyList	=new List<Brush>(brushList);
				List<Brush> front		=new List<Brush>();
				List<Brush> back		=new List<Brush>();

				foreach(Brush b in copyList)
				{
					Brush bf, bb;

					b.SplitBrush(f, out bf, out bb);

					if(bb != null)
					{
						back.Add(bb);
					}
					if(bf != null)
					{
						front.Add(bf);
					}
				}

				if(front.Count !=0 && back.Count != 0)
				{
					float	score;
					if(front.Count > back.Count)
					{
						score	=(float)(front.Count / back.Count);
					}
					else
					{
						score	=(float)(back.Count / front.Count);
					}

					if(score < BestScore)
					{
						BestScore	=score;
						BestIndex	=mFaces.IndexOf(f);
					}
				}
			}

			//return the best plane
			return	mFaces[BestIndex];
		}


        public void MarkFacesNotVisible()
        {
            foreach(Face f in mFaces)
            {
				f.mbVisible	=false;
			}
		}


        public bool Intersects(Brush b)
        {
            foreach(Face f in mFaces)
            {
                Face tempFace = new Face(f);

                foreach(Face f2 in b.mFaces)
                {
                    tempFace.ClipByFace(f2, false);
                }

                //if a piece remains, then some part
                //of the brush was poking into the other
                if(tempFace.IsValid())
                {
                    return true;
                }
            }
            return false;
        }


		public void LightBrush(GraphicsDevice g, BspNode root, Vector3 lightPos, float lightVal, Vector3 color)
		{
			foreach(Face f in mFaces)
			{
				f.LightFace(g, root, lightPos, lightVal);//, color);
			}
		}


        //brush b gobbles thisbrush
        //returns a bunch of parts
        public bool SubtractBrush(Brush b, out List<Brush> outside)
        {
            Brush   bf, bb, inside;

            outside =new List<Brush>();
            inside  =new Brush(this);

            bf = bb = null;

            foreach(Face f2 in b.mFaces)
            {
                inside.SplitBrush(f2, out bf, out bb);

                if(bf != null && bf.IsValid())
                {
                    outside.Add(bf);
                }
                inside = bb;
                if(bb == null)
                {
                    break;
                }
            }

            if(inside == null || !inside.IsValid())
            {
                return false;
            }
            return true;
        }


        //expands all faces to seal cracks
        public void SealFaces()
        {
RESTART2:
            //check that faces are valid
            foreach(Face f in mFaces)
            {
                if (!f.IsValid() || f.IsTiny())
                {
                    mFaces.Remove(f);
                    goto RESTART2;
                }
            }

RESTART:
            //clip every face behind every other face
            foreach(Face f in mFaces)
            {
                f.Expand();

                foreach(Face f2 in mFaces)
                {
                    if(f == f2)
                    {
                        continue;
                    }

                    f.ClipByFace(f2, false);
                    if(!f.IsValid())
                    {
                        mFaces.Remove(f);
                        goto RESTART;
                    }
                }
            }
        }


        private bool IsBrushMostlyOnFrontSide(Plane p)
        {
            float frontDist, backDist;

            frontDist = 0.0f;
            backDist = 0.0f;
            foreach (Face f in mFaces)
            {
                f.GetFaceMinMaxDistancesFromPlane(p, ref frontDist, ref backDist);
            }
            if (frontDist > (-backDist))
            {
                return true;
            }
            return false;
        }


		public void GetNodesFromFaces(out List<BspNode> nodes)
		{
			nodes	=new List<BspNode>();

			foreach(Face f in mFaces)
			{
				nodes.Add(new BspNode(f));
			}
		}


		public void AddFacesToList(ref List<Face> faceList)
		{
			foreach(Face f in mFaces)
			{
				faceList.Add(new Face(f));
			}
		}


		public void GetTexFileNames(ref List<string> fn)
		{
			foreach(Face f in mFaces)
			{
				f.GetTexFileNames(ref fn);
			}
		}


		public void SetTexturePointers(List<KeyValuePair<string, Texture2D>> tl)
		{
			foreach(Face f in mFaces)
			{
				f.SetTexturePointers(tl);
			}
		}


		public void WriteToFile(BinaryWriter bw)
		{
			bw.Write(mFaces.Count);

			foreach(Face f in mFaces)
			{
				f.WriteToFile(bw);
			}
		}


		public void BuildVertexInfo(GraphicsDevice g)
		{
			foreach(Face f in mFaces)
			{
				f.BuildVertexInfo(g);
			}
		}


		public void AddFacesToLeaf(BspNode bn)
		{
			BspNode	walk	=bn;

			//assign face zero to the node
			walk.mFace	=new Face(mFaces[0]);
			walk.mPlane	=mFaces[0].GetPlane();
			for(int i=1;i < mFaces.Count;i++)
			{
				BspNode	b	=new BspNode(mFaces[i]);
				walk.mBack	=b;
				b.mParent	=walk;
				walk		=walk.mBack;
			}
			walk.mbLeaf	=true;
		}


        public void SplitBrush(Face splitBy, out Brush bf, out Brush bb)
        {
            float   fDist, bDist;
			Plane	p	=splitBy.GetPlane();

            fDist = bDist = 0.0f;

            foreach(Face f in mFaces)
            {
                f.GetFaceMinMaxDistancesFromPlane(p, ref fDist, ref bDist);
            }

            if(fDist < 0.1f)
            {
                //all behind
                bb  =new Brush(this);
                bf  =null;
                return;
            }
            if(bDist > -0.1f)
            {
                bf  =new Brush(this);
                bb  =null;
                return;
            }

            //create a split face
            Face splitFace = new Face(p, splitBy);

            //clip against all brush faces
            foreach(Face f in mFaces)
            {
                splitFace.ClipByFace(f, false);
            }

            if(!splitFace.IsValid() || splitFace.IsTiny())
            {
                Debug.WriteLine("Doing the mostly on side thing");
                if (IsBrushMostlyOnFrontSide(p))
                {
                    bf = new Brush(this);
                    bb = null;
                }
                else
                {
                    bb = new Brush(this);
                    bf = null;
                }
                return;
            }

            bb  =new Brush(this);
            bf  =new Brush(this);

            foreach(Face f in bb.mFaces)
            {
                f.ClipByFace(splitFace, false);
            }
            foreach(Face f in bf.mFaces)
            {
                f.ClipByFace(splitFace, true);
            }

            //add split poly to both sides
            bb.mFaces.Add(new Face(splitFace));
            bf.mFaces.Add(new Face(splitFace, true));

            bb.SealFaces();
            bf.SealFaces();
        }
	}
}
