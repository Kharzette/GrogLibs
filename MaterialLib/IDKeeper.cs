using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	public class IDKeeper
	{
		internal struct MatID
		{
			internal Material	mMat;
			internal int		mID;
		};

		List<MaterialLib>	mLibs	=new List<MaterialLib>();

		Dictionary<string, MatID>	mIDs	=new Dictionary<string, MatID>();
		Dictionary<Material, MatID>	mMatIDs	=new Dictionary<Material, MatID>();

		const int	StartIndex	=10;	//0 is for occluders, 10 seems like a nice number!?


		public void Clear()
		{
			mLibs.Clear();
			mIDs.Clear();
			mMatIDs.Clear();
		}


		public void AddLib(MaterialLib lib)
		{
			Debug.Assert(!mLibs.Contains(lib));

			mLibs.Add(lib);
		}


		public void Scan()
		{
			int	index	=StartIndex;
			foreach(MaterialLib mlib in mLibs)
			{
				Dictionary<string, Material>	mats	=mlib.GetMaterials();

				foreach(KeyValuePair<string, Material> mat in mats)
				{
					if(mat.Key == "DMN")
					{
						continue;
					}

					MatID	mid;

					mid.mID		=index++;
					mid.mMat	=mat.Value;

					string	matName	=StripName(mat.Key);
					if(!mIDs.ContainsKey(matName))
					{
						mIDs.Add(matName, mid);
					}
					mMatIDs.Add(mat.Value, mid);
				}
			}
		}


		//this works for BSP materials but not static/char
		public void AssignIDsToMaterials(Effect forEffect)
		{
			foreach(MaterialLib mlib in mLibs)
			{
				Dictionary<string, Material>	mats	=mlib.GetMaterials();

				foreach(KeyValuePair<string, Material> mat in mats)
				{
					if(mat.Key == "DMN")
					{
						continue;
					}

					if(mlib.GetMaterialShader(mat.Key) != forEffect)
					{
						continue;
					}

					//if this already exists, parameterkeeper handles it
					mat.Value.AddParameter("mMaterialID",
						EffectParameterClass.Scalar,
						EffectParameterType.Int32, 1,
						mMatIDs[mat.Value].mID);
				}
			}
		}


		public int GetID(string matName)
		{
			string	stripped	=StripName(matName);

			if(mIDs.ContainsKey(stripped))
			{
				return	mIDs[stripped].mID;
			}
			return	-1;
		}


		public int GetID(Material mat)
		{
			if(mMatIDs.ContainsKey(mat))
			{
				return	mMatIDs[mat].mID;
			}
			return	-1;
		}


		string StripName(string bigName)
		{
			//all world materials have * in front of post stuff
			int	assIndex	=bigName.IndexOf('*');

			if(assIndex == -1)
			{
				return	bigName;
			}

			return	bigName.Substring(0, assIndex);
		}
	}
}
