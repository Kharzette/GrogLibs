using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Source
	{
		private string			mNameID, mFloatID;
		private List<string>	mNames		=new List<string>();
		private List<float>		mFloats		=new List<float>();
		private Accessor		mAccessor;


		public bool IsPosition()
		{
			if(mFloatID == null)
			{
				return	false;
			}

			//not really sure this is a valid way
			return	(mFloatID.Contains("positions-array")
				|| mFloatID.Contains("Position-array")
				|| mFloatID.Contains("pv-array"));
		}


		public bool IsNormal()
		{
			if(mFloatID == null)
			{
				return	false;
			}

			//not really sure this is a valid way
			return	mFloatID.Contains("normals-array");
		}


		public bool IsTexCoord()
		{
			if(mFloatID == null)
			{
				return	false;
			}

			//not really sure this is a valid way
			return	mAccessor.IsTexCoord();
		}


		public List<float>	GetFloatArray()
		{
			return	mFloats;
		}


		public List<string>	GetNameArray()
		{
			return	mNames;
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
					return;
				}
				else if(r.Name == "Name_array")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						mNameID	=r.Value;
						r.MoveToNextAttribute();
						int nCnt;
						int.TryParse(r.Value, out nCnt);

						//skip to the guts
						r.Read();

						string[] tokens	=r.Value.Split(' ', '\n');

						//copynames
						foreach(string tok in tokens)
						{
							if(tok == "")
							{
								continue;	//skip empties
							}
							mNames.Add(tok);
						}
						r.Read();
					}
				}
				else if(r.Name == "float_array")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						mFloatID	=r.Value;
						r.MoveToNextAttribute();
						int	cnt;
						int.TryParse(r.Value, out cnt);

						//skip to the guts
						r.Read();

						string[] tokens	=r.Value.Split(' ', '\n');

						//dump into floats
						foreach(string flt in tokens)
						{
							float	f;
							
							if(Single.TryParse(flt, out f))
							{
								mFloats.Add(f);
							}
						}
						r.Read();
					}
				}
				else if(r.Name == "accessor")
				{
					mAccessor	=new Accessor();
					mAccessor.Load(r);
				}
			}
		}
	}
}