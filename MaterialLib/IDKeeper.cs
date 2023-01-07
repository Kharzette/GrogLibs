using System.Collections.Generic;
using System.Diagnostics;
using Vortice.Mathematics;


namespace MaterialLib;

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

	public delegate Color CalcMaterialOutlineColor(string matName);

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


	public int GetHighestMatID()
	{
		int	highID	=-1;
		foreach(KeyValuePair<string, MatID> id in mIDs)
		{
			if(id.Value.mID > highID)
			{
				highID	=id.Value.mID;
			}
		}

		return	highID;
	}


	public void CalcMaterialOutlineColors(CalcMaterialOutlineColor calc, Color []colors)
	{
		foreach(KeyValuePair<string, MatID> id in mIDs)
		{
			colors[id.Value.mID]	=calc(id.Key);
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