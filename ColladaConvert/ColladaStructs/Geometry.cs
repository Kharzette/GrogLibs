using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Geometry
	{
		string	mName;
		Mesh	mMesh;


		public Geometry(string name)
		{
			mName	=name;
		}


		public int GetNumMeshParts()
		{
			return	mMesh.GetNumParts();
		}


		public List<float> GetBaseVerts(int idx)
		{
			return	mMesh.GetBaseVerts(idx);
		}


		public List<float> GetNormals(int idx)
		{
			return	mMesh.GetNormals(idx);
		}


		public List<float> GetTexCoords(int idx, int set)
		{
			return	mMesh.GetTexCoords(idx, set);
		}


		public List<float> GetColors(int idx, int set)
		{
			return	mMesh.GetColors(idx, set);
		}


		public List<int> GetNormalIndexs(int idx)
		{
			return	mMesh.GetNormalIndexs(idx);
		}


		public List<int> GetTexCoordIndexs(int idx, int set)
		{
			return	mMesh.GetTexCoordIndexs(idx, set);
		}


		public List<int> GetColorIndexs(int idx, int set)
		{
			return	mMesh.GetColorIndexs(idx, set);
		}


		public List<int> GetPositionIndexs(int idx)
		{
			return	mMesh.GetPositionIndexs(idx);
		}


		public List<int> GetVertCounts(int idx)
		{
			return	mMesh.GetVertCounts(idx);
		}


		public string GetMeshName()
		{
			return	mMesh.GetName();
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
					mMesh	=new Mesh(mName);
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