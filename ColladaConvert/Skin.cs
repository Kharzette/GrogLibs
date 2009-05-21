using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Skin
	{
		private string						mSource;
		private Matrix						mBindShapeMatrix;
		private Dictionary<string, Source>	mSources	=new Dictionary<string,Source>();
		private Joints						mJoints;
		private VertexWeights				mVertWeights;


		//returns the number of bones that influence the vert indexed
		public int GetNumInfluencesForVertIndex(int vertIndex)
		{
			return	mVertWeights.GetCount(vertIndex);
		}


		//returns the infIndexd bone influencing vertex at vertIndex
		public int GetBoneIndexForVertIndex(int vertIndex, int infIndex)
		{
			return	mVertWeights.GetBoneIndex(vertIndex, infIndex);
		}


		//returns the infIndexd weight for vertex at vertIndex
		public float GetBoneWeightForVertIndex(int vertIndex, int infIndex)
		{
			//grab the index
			int	idx	=mVertWeights.GetWeightIndex(vertIndex, infIndex);

			//look into source with the index into the weight array
			string	key	=mSource.Substring(1) + "-skin-weights";

			Source	src	=mSources[key];

			return	src.GetFloatArray()[idx];
		}


		//returns the source without the #
		public string GetGeometryID()
		{
			return	mSource.Substring(1);
		}


		public void Load(XmlReader r)
		{
			if(r.AttributeCount > 0)
			{
				r.MoveToFirstAttribute();

				mSource	=r.Value;
			}

			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "skin")
				{
					return;
				}
				else if(r.Name == "bind_shape_matrix")
				{
					if(r.NodeType == XmlNodeType.Element)
					{
						//skip to the guts
						r.Read();

						Collada.GetMatrixFromString(r.Value, out mBindShapeMatrix);
					}
				}
				else if(r.Name == "joints")
				{
					mJoints	=new Joints();
					mJoints.Load(r);
				}
				else if(r.Name == "source")
				{
					Source	src	=new Source();
					r.MoveToFirstAttribute();
					string	srcID	=r.Value;
					src.Load(r);
					mSources.Add(srcID, src);
				}
				else if(r.Name == "vertex_weights")
				{
					mVertWeights	=new VertexWeights();
					mVertWeights.Load(r);
				}
			}
		}
	}
}