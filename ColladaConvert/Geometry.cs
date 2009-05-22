using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Geometry
	{
		private string		mName;
		Mesh				mMesh;


		public Geometry()	{}


		public List<float> GetBaseVerts()
		{
			return	mMesh.GetBaseVerts();
		}


		public List<float> GetNormals()
		{
			return	mMesh.GetNormals();
		}


		public List<float> GetTexCoords()
		{
			return	mMesh.GetTexCoords();
		}


		public List<int> GetNormalIndexs()
		{
			return	mMesh.GetNormalIndexs();
		}


		public List<int> GetTexCoordIndexs()
		{
			return	mMesh.GetTexCoordIndexs();
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