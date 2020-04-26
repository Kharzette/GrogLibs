using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MaterialLib
{
	public class AlphaPool
	{
		List<AlphaNode>	mAlphas	=new List<AlphaNode>();

		//anything over this attempts to planar sort
		const int	IndexPlanarSortThreshold	=100;


		public void StoreDraw(MatLib mlib, Vector3 sortPoint,
			string matName, VertexBufferBinding vbb, Buffer ib,
			Matrix worldMat, Int32 startIndex, Int32 indexCount)
		{
			AlphaNode	an	=new AlphaNode(mlib, sortPoint, matName,
					vbb, ib, worldMat, startIndex, indexCount);

			mAlphas.Add(an);
		}


		public void StoreDraw(MatLib mlib, Vector3 sortPoint,
			Vector3 sortPlaneNormal, float sortPlaneDistance,
			string matName, VertexBufferBinding vbb, Buffer ib,
			Matrix worldMat, Int32 startIndex, Int32 indexCount)
		{
			AlphaNode	an	=new AlphaNode(mlib, sortPoint,
					sortPlaneNormal, sortPlaneDistance, matName,
					vbb, ib, worldMat, startIndex, indexCount);

			mAlphas.Add(an);
		}


		public void StoreParticleDraw(MatLib mlib,
			Vector3 sortPoint,
			VertexBufferBinding vbb, Int32 vertCount,
			string tex,	Matrix view, Matrix proj)
		{
			AlphaNode	an	=new AlphaNode(mlib, sortPoint, vbb,
				vertCount, tex, view, proj);

			mAlphas.Add(an);
		}


		public void DrawAll(GraphicsDevice gd)
		{
			//get nearest planar sort
			AlphaNode	ps		=null;
			float		nearest	=float.MaxValue;
			Vector3		eyePos	=gd.GCam.Position;
			foreach(AlphaNode an in mAlphas)
			{
				if(!an.IsPlanar())
				{
					continue;
				}

				float	dist	=an.PlaneDistance(eyePos);

				if(dist < nearest)
				{
					nearest	=dist;
					ps		=an;
				}
			}

			if(ps != null)
			{
				//sort into in front and behind bucketses
				List<AlphaNode>	front	=new List<AlphaNode>();
				List<AlphaNode>	back	=new List<AlphaNode>();

				foreach(AlphaNode an in mAlphas)
				{
					if(an == ps)
					{
						continue;
					}

					float	dist	=ps.PlaneDistance(an);
					if(dist < 0f)
					{
						back.Add(an);
					}
					else
					{
						front.Add(an);
					}
				}

				//is eye on front or back of sort plane?
				float	eyeDist	=ps.PlaneDistance(eyePos);
				if(eyeDist < 0f)
				{
					//back side first
					Sort(back, eyePos);
					foreach(AlphaNode an in back)
					{
						an.Draw(gd);
					}

					//plane
					ps.Draw(gd);

					//front side last
					Sort(front, eyePos);
					foreach(AlphaNode an in front)
					{
						an.Draw(gd);
					}
				}
				else
				{
					//front side first
					Sort(front, eyePos);
					foreach(AlphaNode an in front)
					{
						an.Draw(gd);
					}

					//plane
					ps.Draw(gd);

					//back side last
					Sort(back, eyePos);
					foreach(AlphaNode an in back)
					{
						an.Draw(gd);
					}
				}

				front.Clear();
				back.Clear();
			}
			else
			{
				Sort(eyePos);

				foreach(AlphaNode an in mAlphas)
				{
					an.Draw(gd);
				}
			}

			//clear nodes when done
			mAlphas.Clear();
		}


		static void Sort(List<AlphaNode> list, Vector3 eyePos)
		{
			AlphaNodeComparer	anc	=new AlphaNodeComparer(eyePos);

			list.Sort(anc);
		}


		void Sort(Vector3 eyePos)
		{
			Sort(mAlphas, eyePos);
		}
	}
}
