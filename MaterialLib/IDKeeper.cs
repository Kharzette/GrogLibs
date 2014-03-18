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
			int	index	=0;
			foreach(MaterialLib mlib in mLibs)
			{
				Dictionary<string, Material>	mats	=mlib.GetMaterials();

				foreach(KeyValuePair<string, Material> mat in mats)
				{
					string	matName	=StripName(mat.Key);

					if(mIDs.ContainsKey(matName))
					{
						mat.Value.AddParameter("mMaterialID",
							EffectParameterClass.Scalar,
							EffectParameterType.Int32, 1, mIDs[matName].mID);
						continue;
					}

					MatID	mid;
					mid.mID		=index++;
					mid.mMat	=mat.Value;
					mIDs.Add(matName, mid);
				}
			}

			//assign
			foreach(KeyValuePair<string, MatID> ids in mIDs)
			{
				//if this already exists, parameterkeeper handles it
				ids.Value.mMat.AddParameter("mMaterialID",
					EffectParameterClass.Scalar,
					EffectParameterType.Int32, 1, ids.Value.mID);
			}
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
