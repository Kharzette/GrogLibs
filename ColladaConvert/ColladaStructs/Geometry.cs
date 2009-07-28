using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Geometry
	{
		Mesh	mMesh;


		public Geometry()	{}


		public List<float> GetBaseVerts()
		{
			return	mMesh.GetBaseVerts();
		}


		public List<float> GetNormals()
		{
			return	mMesh.GetNormals();
		}


		public List<float> GetTexCoords(int set)
		{
			return	mMesh.GetTexCoords(set);
		}


		public List<float> GetColors(int set)
		{
			return	mMesh.GetColors(set);
		}


		public List<int> GetNormalIndexs()
		{
			return	mMesh.GetNormalIndexs();
		}


		public List<int> GetTexCoordIndexs(int set)
		{
			return	mMesh.GetTexCoordIndexs(set);
		}


		public List<int> GetColorIndexs(int set)
		{
			return	mMesh.GetColorIndexs(set);
		}


		public List<int> GetPositionIndexs()
		{
			return	mMesh.GetPositionIndexs();
		}


		public List<int> GetVertCounts()
		{
			return	mMesh.GetVertCounts();
		}


		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "mesh")
				{
					//only one mesh per geometry allowed
					mMesh	=new Mesh();
					mMesh.Load(r);
				}
				else if(r.Name == "geometry")
				{
					return;
				}
			}
		}
	}
}