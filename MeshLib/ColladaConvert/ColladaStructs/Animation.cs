using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Animation
	{
		private string	mName;

		private List<SubAnimation>	mSubAnims	=new List<SubAnimation>();


		public string GetName()
		{
			return	mName;
		}


		internal MeshLib.SubAnim	GetAnims(MeshLib.Skeleton skel)
		{
			//grab full list of bones
			List<string>	boneNames	=new List<string>();

			skel.GetBoneNames(boneNames);

			//for each bone, find all keyframe times
			foreach(string bone in boneNames)
			{
				List<float>	times	=new List<float>();

				foreach(SubAnimation sa in mSubAnims)
				{
					List<float>	saTimes	=sa.GetTimesForBone(bone);

					foreach(float t in saTimes)
					{
						if(times.Contains(t))
						{
							continue;
						}
						times.Add(t);
					}
				}

				if(times.Count == 0)
				{
					continue;
				}

				times.Sort();

				//build list of keys for times
				List<MeshLib.KeyFrame>	keys	=new List<MeshLib.KeyFrame>();
				foreach(float t in times)
				{
					keys.Add(new MeshLib.KeyFrame());
				}

				//set keys
				foreach(SubAnimation sa in mSubAnims)
				{
					sa.SetKeys(bone, times, keys);
				}

				//fix quaternions
				foreach(MeshLib.KeyFrame kf in keys)
				{
					Matrix	mat	=Matrix.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(kf.mRotation.X));
					mat	*=Matrix.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(kf.mRotation.Y));
					mat	*=Matrix.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(kf.mRotation.Z));

					kf.mRotation	=Quaternion.CreateFromRotationMatrix(mat);
				}


				//find and set bone key reference
				MeshLib.KeyFrame	boneKey	=skel.GetBoneKey(bone);

				return	new MeshLib.SubAnim(bone, times, keys);
			}

			return	null;
		}


		public void Load(XmlReader r)
		{
			r.MoveToNextAttribute();
			mName	=r.Value;
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "animation")
				{
					if(r.NodeType == XmlNodeType.EndElement)
					{
						return;
					}

					SubAnimation	sub	=new SubAnimation();
					sub.Load(r);
					mSubAnims.Add(sub);
				}
			}
		}
	}
}