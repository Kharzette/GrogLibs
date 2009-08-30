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


		//returns the array of bone names
		public List<string> GetJointNameArray()
		{
			string	key	=mJoints.GetJointKey();

			//strip #
			key	=key.Substring(1);

			Source	src	=mSources[key];

			return	src.GetNameArray();
		}


		public void ConvertBindShapeMatrixCoordinateSystemMAX()
		{
			//mBindShapeMatrix	=Collada.ConvertMatrixCoordinateSystemMAX(mBindShapeMatrix);
		}


		public List<Matrix>	GetInverseBindPoses()
		{
			string	key	=mJoints.GetInverseBindPosesKey();

			//strip #
			key	=key.Substring(1);

			Source	src	=mSources[key];

			List<float>	fa	=src.GetFloatArray();

			List<Matrix>	ret	=new List<Matrix>();

			for(int i=0;i < fa.Count / 16;i++)
			{
				int	ofs	=i * 16;
				Matrix	mat	=new Matrix(
					fa[ofs + 0], fa[ofs + 4], fa[ofs + 8], fa[ofs + 12],
					fa[ofs + 1], fa[ofs + 5], fa[ofs + 9], fa[ofs + 13],
					fa[ofs + 2], fa[ofs + 6], fa[ofs + 10], fa[ofs + 14],
					fa[ofs + 3], fa[ofs + 7], fa[ofs + 11], fa[ofs + 15]);

				ret.Add(mat);
			}
			return	ret;
		}


		public Matrix GetBindShapeMatrix()
		{
			return	mBindShapeMatrix;
		}


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

			//get the source key to the actual weight array
			string	key	=mVertWeights.GetWeightArrayKey();

			//strip off #
			key	=key.Substring(1);

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