using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	internal class StateBlockPool
	{
		//list of unique states
		List<BlendState>		mBlendStates	=new List<BlendState>();
		List<DepthStencilState>	mDepthStates	=new List<DepthStencilState>();
		List<RasterizerState>	mRasterStates	=new List<RasterizerState>();


		internal BlendState FindBlendState(BlendFunction abf, Blend adb,
			Blend asb, Color bf, BlendFunction cbf, Blend cdb, Blend csb)
		{
			foreach(BlendState bs in mBlendStates)
			{
				if(bs.AlphaBlendFunction != abf)
				{
					continue;
				}
				if(bs.AlphaDestinationBlend != adb)
				{
					continue;
				}
				if(bs.AlphaSourceBlend != asb)
				{
					continue;
				}
				if(bs.BlendFactor != bf)
				{
					continue;
				}
				if(bs.ColorBlendFunction != cbf)
				{
					continue;
				}
				if(bs.ColorDestinationBlend != cdb)
				{
					continue;
				}
				if(bs.ColorSourceBlend != csb)
				{
					continue;
				}
				return	bs;
			}

			//not found, create a new one
			BlendState	nbs				=new BlendState();
			nbs.AlphaBlendFunction		=abf;
			nbs.AlphaDestinationBlend	=adb;
			nbs.AlphaSourceBlend		=asb;
			nbs.BlendFactor				=bf;
			nbs.ColorBlendFunction		=cbf;
			nbs.ColorDestinationBlend	=cdb;
			nbs.ColorSourceBlend		=csb;

			mBlendStates.Add(nbs);

			return	nbs;
		}


		internal DepthStencilState FindDepthStencilState(bool de, CompareFunction df, bool dwe)
		{
			foreach(DepthStencilState dss in mDepthStates)
			{
				if(dss.DepthBufferEnable != de)
				{
					continue;
				}
				if(dss.DepthBufferFunction != df)
				{
					continue;
				}
				if(dss.DepthBufferWriteEnable != dwe)
				{
					continue;
				}
				return	dss;
			}

			//not found
			DepthStencilState	ndss	=new DepthStencilState();
			ndss.DepthBufferEnable		=de;
			ndss.DepthBufferFunction	=df;
			ndss.DepthBufferWriteEnable	=dwe;

			mDepthStates.Add(ndss);

			return	ndss;
		}


		internal RasterizerState FindRasterizerState(CullMode cm)
		{
			foreach(RasterizerState rs in mRasterStates)
			{
				if(rs.CullMode != cm)
				{
					continue;
				}
				return	rs;
			}

			//not found
			RasterizerState	nrs	=new RasterizerState();
			nrs.CullMode		=cm;

			mRasterStates.Add(nrs);

			return	nrs;
		}
	}
}
