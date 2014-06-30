using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SharpDX;


namespace MaterialLib
{
	//tries to assign unique material ids to stuff
	public class IDKeeper
	{
		internal struct MatID
		{
			internal string	mMat;
			internal int	mID;
		};

		List<MaterialLib>	mLibs	=new List<MaterialLib>();

		Dictionary<string, List<string>>	mGroups	=new Dictionary<string, List<string>>();

		Dictionary<string, MatID>	mIDs	=new Dictionary<string, MatID>();

		const int	StartIndex	=10;	//0 is for occluders, 10 seems like a nice number!?


		public void Clear()
		{
			mLibs.Clear();
			mIDs.Clear();
		}


		//allows grouping of materials so they all get the same id
		//must be done before scan!
		public void AddMaterialGroup(string name, List<string> members)
		{
			Debug.Assert(mIDs.Count == 0);

			mGroups.Add(name, members);
		}


		public void AddLib(MaterialLib lib)
		{
			Debug.Assert(!mLibs.Contains(lib));

			mLibs.Add(lib);
		}


		public void Scan()
		{
			int	index	=StartIndex;

			//start with material groups
			foreach(KeyValuePair<string, List<string>> group in mGroups)
			{
				if(!mIDs.ContainsKey(group.Key))
				{
					MatID	mid;

					mid.mID		=index++;
					mid.mMat	=group.Key;

					mIDs.Add(group.Key, mid);
				}
			}

			foreach(MaterialLib mlib in mLibs)
			{
				List<string>	mats	=mlib.GetMaterialNames();

				foreach(string mat in mats)
				{
					if(mat == "DMN")
					{
						continue;
					}

					//ensure not a part of a group
					bool	bInGroup	=false;
					foreach(KeyValuePair<string, List<string>> group in mGroups)
					{
						if(group.Value.Contains(mat))
						{
							bInGroup	=true;
							break;
						}
					}

					if(bInGroup)
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
