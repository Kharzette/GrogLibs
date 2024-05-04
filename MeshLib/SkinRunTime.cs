using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;


namespace MeshLib;

//this is now a master skin, one per character
//all mesh parts will index in the same way
//The tool will need to make sure the inverse bind poses
//are all the same for each bone
public partial class Skin
{
	internal void BuildDebugBoundDrawData(int index, CommonPrims cprims)
	{
		if(!mBoneColShapes.ContainsKey(index))
		{
			return;
		}

		int	choice	=mBoneColShapes[index];
		if(choice == Box)
		{
			cprims.AddBox(index, mBoneBoxes[index]);
		}
		else if(choice == Sphere)
		{
			cprims.AddSphere(index, mBoneSpheres[index]);
		}
		else if(choice == Capsule)
		{
			cprims.AddCapsule(index, mBoneCapsules[index]);
		}
	}


	internal void BuildDebugBoundDrawData(CommonPrims cprims)
	{
		foreach(KeyValuePair<int, int> choice in mBoneColShapes)
		{
			BuildDebugBoundDrawData(choice.Key, cprims);
		}
	}
}