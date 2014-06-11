using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SharpDX;


namespace MaterialLib
{
	public class IDKeeper
	{
		internal struct MatID
		{
			internal string	mMat;
			internal int	mID;
		};

		List<MaterialLib>	mLibs	=new List<MaterialLib>();

		Dictionary<string, MatID>	mIDs	=new Dictionary<string, MatID>();

		const int	StartIndex	=10;	//0 is for occluders, 10 seems like a nice number!?


		public void Clear()
		{
			mLibs.Clear();
			mIDs.Clear();
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
				List<string>	mats	=mlib.GetMaterialNames();

				foreach(string mat in mats)
				{
					if(mat == "DMN")
					{
						continue;
					}

					MatID	mid;

					mid.mID		=index++;
					mid.mMat	=mat;

					string	matName	=StripName(mat);
					if(!mIDs.ContainsKey(matName))
					{
						mIDs.Add(matName, mid);
					}
				}
			}
		}


		//this works for BSP materials but not static/char
		public void AssignIDsToEffectMaterials(string fxName)
		{
			foreach(MaterialLib mlib in mLibs)
			{
				List<string>	mats	=mlib.GetMaterialNames();

				foreach(string mat in mats)
				{
					if(mat == "DMN")
					{
						continue;
					}

					if(mlib.GetMaterialEffect(mat) != fxName)
					{
						continue;
					}

					string	stripped	=StripName(mat);
					if(!mIDs.ContainsKey(stripped))
					{
						continue;
					}

					mlib.SetMaterialParameter(mat, "mMaterialID", mIDs[stripped].mID);
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
