using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Mesh
	{
		private Dictionary<string, Source>		mSources	=new Dictionary<string,Source>();
		private Dictionary<string, Vertices>	mVerts		=new Dictionary<string,Vertices>();
		private List<Polygons>					mPolys		=new List<Polygons>();
		private	PolyList						mPolyList;


		public Mesh()	{}


		public List<float> GetBaseVerts()
		{
			foreach(KeyValuePair<string, Source> src in mSources)
			{
				if(src.Value.IsPosition())
				{
					return	src.Value.GetFloatArray();
				}
			}
			return	null;
		}


		public List<float> GetNormals()
		{
			foreach(KeyValuePair<string, Source> src in mSources)
			{
				if(src.Value.IsNormal())
				{
					return	src.Value.GetFloatArray();
				}
			}
			return	null;
		}


		public List<int> GetPositionIndexs()
		{
			//see if this file is into polys or polylists
			if(mPolys.Count > 0)
			{
				return	null;	//TODO: finish this
			}
			else
			{
				return	mPolyList.GetPositionIndexs();
			}
		}


		public List<int> GetNormalIndexs()
		{
			//see if this file is into polys or polylists
			if(mPolys.Count > 0)
			{
				return	null;	//TODO: finish this
			}
			else
			{
				return	mPolyList.GetNormalIndexs();
			}
		}


		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "source")
				{
					r.MoveToFirstAttribute();

					string	srcID	=r.Value;

					Source	src	=new Source();
					src.Load(r);

					mSources.Add(srcID, src);
				}
				else if(r.Name == "mesh")
				{
					return;
				}
				else if(r.Name == "vertices")
				{
					r.MoveToFirstAttribute();
					string	vertID	=r.Value;

					Vertices	vert	=new Vertices();
					vert.Load(r);
					mVerts.Add(vertID, vert);					
				}
				else if(r.Name == "polygons")
				{
					Polygons	pol	=new Polygons();
					pol.Load(r);

					mPolys.Add(pol);
				}
				else if(r.Name == "polylist")
				{
					mPolyList	=new PolyList();
					mPolyList.Load(r);
				}
			}
		}
	}
}