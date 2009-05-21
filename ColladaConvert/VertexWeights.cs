using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class VertexWeights
	{
		private List<Input>		mInputs				=new List<Input>();
		private List<int>		mInfluenceCounts	=new List<int>();
		private List<List<int>>	mBoneIndexes		=new List<List<int>>();
		private List<List<int>>	mWeightIndexes		=new List<List<int>>();


		//returns the number of influences for a vert at vertIndex
		public int GetCount(int vertIndex)
		{
			return	mInfluenceCounts[vertIndex];
		}


		//returns the infIndexd index of the vert at vertIndex
		//as if that made any sense at all
		public int GetBoneIndex(int vertIndex, int infIndex)
		{
			return	mBoneIndexes[vertIndex][infIndex];
		}


		//returns an index into the array of weights for the
		//vertex at vertIndex.
		public int GetWeightIndex(int vertIndex, int infIndex)
		{
			return	mWeightIndexes[vertIndex][infIndex];
		}


		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "input")
				{
					Input	inp	=new Input();
					inp.Load(r);
					mInputs.Add(inp);
				}
				else if(r.Name == "vcount")
				{
					if(r.NodeType != XmlNodeType.EndElement)
					{
						r.Read();

						string[] tokens	=r.Value.Split(' ','\n');

						//copy vertex weight counts
						foreach(string tok in tokens)
						{
							int numInfluences;

							if(int.TryParse(tok, out numInfluences))
							{
								mInfluenceCounts.Add(numInfluences);
							}
						}
					}
				}
				else if(r.Name == "v")
				{
					if(r.NodeType != XmlNodeType.EndElement)
					{
						r.Read();

						string	[]tokens	=r.Value.Split(' ','\n');

						int			curVert		=0;
						bool		bEven		=true;
						int			numInf		=0;
						List<int>	pvBone		=new List<int>();
						List<int>	pvWeight	=new List<int>();

						//copy vertex weight bones
						foreach(string tok in tokens)
						{
							int	val;

							if(int.TryParse(tok, out val))
							{
								if(bEven)
								{
									pvBone.Add(val);
								}
								else
								{
									pvWeight.Add(val);
									numInf++;
								}
								bEven	=!bEven;
								if(numInf >= GetCount(curVert))
								{
									mBoneIndexes.Add(pvBone);
									mWeightIndexes.Add(pvWeight);
									numInf		=0;
									pvBone		=new List<int>();
									pvWeight	=new List<int>();
								}
							}
						}
					}
				}
				else if(r.Name == "vertex_weights")
				{
					return;
				}
			}
		}
	}
}