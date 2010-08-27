using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Mesh
	{
		string	mName;

		Dictionary<string, Source>		mSources	=new Dictionary<string,Source>();
		Dictionary<string, Vertices>	mVerts		=new Dictionary<string,Vertices>();
		List<Polygons>					mPolys		=new List<Polygons>();


		public Mesh(string name)
		{
			mName	=name;
		}


		public string GetName()
		{
			return	mName;
		}


		public List<float> GetBaseVerts(int idx)
		{
			//find key
			string	key	=mPolys[idx].GetPositionSourceKey();

			//strip #
			key	=key.Substring(1);

			//use key to look up in mVerts
			Vertices v	=mVerts[key];

			key	=v.GetPositionKey();

			//strip #
			key	=key.Substring(1);

			return	mSources[key].GetFloatArray();
		}


		public List<float> GetNormals(int idx)
		{
			//find key
			string	key	=mPolys[idx].GetNormalSourceKey();

			//strip #
			key	=key.Substring(1);

			return	mSources[key].GetFloatArray();
		}


		public List<float> GetTexCoords(int idx, int set)
		{
			//find texcoord key
			string	key	=mPolys[idx].GetTexCoordSourceKey(set);

			if(key == "")
			{
				return	null;
			}

			//strip #
			key	=key.Substring(1);

			return	mSources[key].GetFloatArray();
		}


		public List<float> GetColors(int idx, int set)
		{
			//find color key
			string	key	=mPolys[idx].GetColorSourceKey(set);

			if(key == "")
			{
				return	null;
			}

			//strip #
			key	=key.Substring(1);

			return	mSources[key].GetFloatArray();
		}


		public List<int> GetPositionIndexs(int idx)
		{
			return	mPolys[idx].GetPositionIndexs();
		}


		public List<int> GetNormalIndexs(int idx)
		{
			return	mPolys[idx].GetNormalIndexs();
		}


		public List<int> GetTexCoordIndexs(int idx, int set)
		{
			return	mPolys[idx].GetTexCoordIndexs(set);
		}


		public List<int> GetColorIndexs(int idx, int set)
		{
			return	mPolys[idx].GetColorIndexs(set);
		}


		public List<int> GetVertCounts(int idx)
		{
			return	mPolys[idx].GetVertCounts();
		}


		public int GetNumParts()
		{
			return	mPolys.Count;
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
					Polygons	p	=new Polygons();
					p.Load(r);

					//make sure there's actual data here
					//sometimes the exporter likes to give
					//an empty set
					if(p.GetCount() != 0)
					{
						mPolys.Add(p);
					}
				}
				else if(r.Name == "polylist")
				{
					Polygons	p	=new Polygons();
					p.LoadList(r);
					if(p.GetCount() != 0)
					{
						mPolys.Add(p);
					}
				}
			}
		}
	}
}