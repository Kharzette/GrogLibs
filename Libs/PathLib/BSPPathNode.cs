using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace SpriteMapLib
{
	class BspPathNode : BspNode
	{
		public List<PathNode>	mPathNodes;


		public void CreateFromBSPTree(BspNode bn)
		{
			mPlane	=bn.mPlane;

			if(bn.mBrush != null)
			{
				mBrush	=new Brush(bn.mBrush);
				return;
			}

			if(bn.mFront != null)
			{
				BspPathNode	bpn	=new BspPathNode();
				mFront	=bpn;
				bpn.CreateFromBSPTree(bn.mFront);
			}
			if(bn.mBack != null)
			{
				BspPathNode	bpn	=new BspPathNode();
				mBack	=bpn;
				bpn.CreateFromBSPTree(bn.mBack);
			}
		}


		public bool AddPathNode(PathNode pn)
		{
			float	d;

			d	=Vector2.Dot(mPlane.mNormal, pn.mPosition) - mPlane.mDistance;

			if(mBrush != null)
			{
				if(mBrush.IsPointInside(pn.mPosition))
				{
					return	false;
				}
				if(mPathNodes == null)
				{
					mPathNodes	=new List<PathNode>();
				}
				mPathNodes.Add(pn);
				return	true;
			}

			if(d > -SpriteUtil.EPSILON)
			{
				if(mFront == null)
				{
					if(mPathNodes == null)
					{
						mPathNodes	=new List<PathNode>();
					}
					mPathNodes.Add(pn);
					return	true;
				}
				return	((BspPathNode)mFront).AddPathNode(pn);
			}
			else if(d < SpriteUtil.EPSILON)
			{
				if(mBack != null)
				{
					return	((BspPathNode)mBack).AddPathNode(pn);
				}
				Debug.Assert(false);
				return	false;
			}
			else
			{
				Debug.Assert(false);
				if(mFront == null)
				{
					return false;	//too close to a face
				}
				return	((BspPathNode)mFront).AddPathNode(pn);
			}
		}


		public PathNode GetNodeNearest(Vector2 pos)
		{
			float	d	=Vector2.Dot(mPlane.mNormal, pos) - mPlane.mDistance;

			if(mBrush != null)
			{
				if(mBrush.IsPointInside(pos))
				{
					return	null;
				}

				if(mPathNodes == null)
				{
					return	null;
				}

				//grab closest
				float		minDist	=float.MaxValue;
				PathNode	nearest	=null;
				foreach(PathNode pn in mPathNodes)
				{
					Vector2	distVec	=pn.mPosition - pos;
					float	len		=distVec.LengthSquared();
					if(len < minDist)
					{
						minDist	=len;
						nearest	=pn;
					}
				}
				return	nearest;
			}

			d	=Vector2.Dot(mPlane.mNormal, pos) - mPlane.mDistance;

			if(d > -SpriteUtil.EPSILON)
			{
				if(mFront == null)
				{
					Debug.Assert(false);
				}
				return	((BspPathNode)mFront).GetNodeNearest(pos);
			}
			else if(d < SpriteUtil.EPSILON)
			{
				return	((BspPathNode)mBack).GetNodeNearest(pos);
			}
			else
			{
				Debug.Assert(false);
				return	((BspPathNode)mFront).GetNodeNearest(pos);
			}
		}


		public void GetNodePoints(ref List<Vector2> pnts)
		{
			if(mBrush != null)
			{
				if(mPathNodes != null)
				{
					foreach(PathNode pn in mPathNodes)
					{
						pnts.Add(pn.mPosition);
					}
				}
				return;
			}

			if(mFront != null)
			{
				((BspPathNode)mFront).GetNodePoints(ref pnts);
			}
			if(mBack != null)
			{
				((BspPathNode)mBack).GetNodePoints(ref pnts);
			}
		}


		public void GetConnectionLines(ref List<Line> cons)
		{
			if(mBrush != null)
			{
				if(mPathNodes != null)
				{
					foreach(PathNode pn in mPathNodes)
					{
						foreach(PathConnection con in pn.mConnections)
						{
							Line	ln;
							ln.mP1	=pn.mPosition;
							ln.mP2	=con.mConnectedTo.mPosition;
							Debug.Assert(!float.IsNaN(ln.mP1.X));
							Debug.Assert(!float.IsNaN(ln.mP1.Y));
							Debug.Assert(!float.IsNaN(ln.mP2.X));
							Debug.Assert(!float.IsNaN(ln.mP2.Y));
							cons.Add(ln);
						}
					}
				}
				return;
			}

			if(mFront != null)
			{
				((BspPathNode)mFront).GetConnectionLines(ref cons);
			}
			if(mBack != null)
			{
				((BspPathNode)mBack).GetConnectionLines(ref cons);
			}
		}


		public override void Read(BinaryReader br)
		{
			mPlane.mNormal.X	=br.ReadSingle();
			mPlane.mNormal.Y	=br.ReadSingle();
			mPlane.mDistance	=br.ReadSingle();

			bool	val	=br.ReadBoolean();
			if(val)
			{
				mBrush	=new Brush();
				mBrush.Read(br);

				int	cnt	=br.ReadInt32();
				if(cnt > 0)
				{
					mPathNodes	=new List<PathNode>();
				}
				for(int i=0;i < cnt;i++)
				{
					PathNode	pn	=new PathNode();
					pn.Read(br);

					mPathNodes.Add(pn);
				}
			}

			val	=br.ReadBoolean();
			if(val)
			{
				mFront	=new BspPathNode();
				mFront.Read(br);
			}

			val	=br.ReadBoolean();
			if(val)
			{
				mBack	=new BspPathNode();
				mBack.Read(br);
			}			
		}


		public override void Write(BinaryWriter bw)
		{
			//write plane
			bw.Write(mPlane.mNormal.X);
			bw.Write(mPlane.mNormal.Y);
			bw.Write(mPlane.mDistance);

			bw.Write(mBrush != null);
			if(mBrush != null)
			{
				mBrush.Write(bw);
				if(mPathNodes == null)
				{
					int	numNodes	=0;
					bw.Write(numNodes);
				}
				else
				{
					bw.Write(mPathNodes.Count);
					foreach(PathNode pn in mPathNodes)
					{
						pn.Write(bw);
					}
				}
			}

			bw.Write(mFront != null);
			if(mFront != null)
			{
				mFront.Write(bw);
			}

			bw.Write(mBack != null);
			if(mBack != null)
			{
				mBack.Write(bw);
			}
		}


		public void PostReadFixUp(BspPathNode root)
		{
			if(mBrush != null)
			{
				if(mPathNodes != null)
				{
					foreach(PathNode pn in mPathNodes)
					{
						foreach(PathConnection pc in pn.mConnections)
						{
							pc.mConnectedTo	=root.GetNodeNearest(pc.mConnectedTo.mPosition);
						}
					}
				}
			}
			if(mFront != null)
			{
				((BspPathNode)mFront).PostReadFixUp(root);
			}
			if(mBack != null)
			{
				((BspPathNode)mBack).PostReadFixUp(root);
			}
		}
	}
}
