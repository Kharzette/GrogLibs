using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Animator
	{
		//I think this class will need to store all the
		//animation channels per anim, and somehow we need
		//to figure out a way to distinguish different animations.
		//Also need to store the skeleton I think
		//
		//The basic idea behind this right now is to give me the
		//skeleton at time t
		private	Dictionary<string, List<Anim>>	mAnims	=new Dictionary<string, List<Anim>>();


		public Animator(Dictionary<string, Animation> anims, Dictionary<string, SceneNode> roots)
		{
			foreach(KeyValuePair<string, Animation> an in anims)
			{
				List<Anim>	alist	=an.Value.GetAnims(roots);

				mAnims.Add(an.Key, alist);
			}
		}


		public void AnimateAll(float time)
		{
			foreach(KeyValuePair<string, List<Anim>> anlist in mAnims)
			{
				foreach(Anim an in anlist.Value)
				{
					an.Animate(time);
				}
			}
		}


		public void Animate(string name, float time)
		{
			if(mAnims.ContainsKey(name))
			{
				foreach(Anim an in mAnims[name])
				{
					an.Animate(time);
				}
			}
		}


		public List<GameSubAnim> BuildGameAnims(GameSkeleton gs)
		{
			List<GameSubAnim>	ret	=new List<GameSubAnim>();
			foreach(KeyValuePair<string, List<Anim>> anlist in mAnims)
			{
				foreach(Anim an in anlist.Value)
				{
					GameChannel	gc;

					GameChannelTarget	gct;
					
					if(!gs.GetChannelTarget(an.GetNodeName(), an.GetOperandSID(), out gct))
					{
						Debug.WriteLine("GetChannelTarget failed in BuildGameAnims!");
					}

					gc	=new GameChannel(gct, an.GetChannelTarget());

					GameSubAnim	gsa	=new GameSubAnim(an.GetNumKeys(),
						an.GetTotalTime(), gc,
						an.GetTimes(), an.GetValues(),
						an.GetControl1(), an.GetControl2());

					ret.Add(gsa);
				}
			}
			return	ret;
		}

/*
		public Dictionary<string, GameSubAnim> BuildGameAnims(int blah, List<string> boneNames, Dictionary<string, SceneNode> nodes)
		{
			//for each sub anim, the keyframes must be
			//played, then the matrices extracted

			//in collada, there are multiple subanims animating
			//basic elemental values like position rotation etc...
			//Sometimes there are more keyframes of a certain
			//type than the other types influencing the bone, so
			//we have to generate extra keys for those.  The game
			//anims will be a combination of the subanims
			//influencing a single bone

			//per bone animation statistics
			Dictionary<string, List<float>>	keyTimesPerBone		=new Dictionary<string, List<float>>();
			Dictionary<string, int>			numKeysPerBone		=new Dictionary<string,int>();
			Dictionary<string, float>		totalTimePerBone	=new Dictionary<string,float>();
			Dictionary<string, GameSubAnim>	perBoneAnims		=new Dictionary<string,GameSubAnim>();

			//record keyframe info
			for(int i=0;i < boneNames.Count;i++)
			{
				string	bn	=boneNames[i];
				foreach(KeyValuePair<string, List<Anim>> anlist in mAnims)
				{
					foreach(Anim an in anlist.Value)
					{
						if(an.GetNodeName() == bn)
						{
							if(numKeysPerBone.ContainsKey(bn))
							{
								//take the greater of the two
								if(an.GetNumKeys() > numKeysPerBone[bn])
								{
									numKeysPerBone[bn]	=an.GetNumKeys();

									//copy extra key times
									keyTimesPerBone[bn].Clear();
									for(int key=0;key < an.GetNumKeys();key++)
									{
										keyTimesPerBone[bn].Add(an.GetTimeForKey(key));
									}
								}
								Debug.Assert(totalTimePerBone[bn] == an.GetTotalTime());
							}
							else
							{
								numKeysPerBone.Add(bn, an.GetNumKeys());
								totalTimePerBone.Add(bn, an.GetTotalTime());
								keyTimesPerBone[bn]	=new List<float>();
								for(int key=0;key < an.GetNumKeys();key++)
								{
									keyTimesPerBone[bn].Add(an.GetTimeForKey(key));
								}
							}
						}
					}
				}
			}

			//animate each subanimation to each key
			for(int i=0;i < boneNames.Count;i++)
			{
				string	bn	=boneNames[i];

				//create a game sub anim for this bone
				GameSubAnim	ga	=new GameSubAnim(numKeysPerBone[bn], totalTimePerBone[bn], bn);
				perBoneAnims.Add(bn, ga);

				//combine all animations that touch this bone

				//step by key to add keys to gamesubanim
				for(int key=0;key < numKeysPerBone[bn];key++)
				{
					foreach(KeyValuePair<string, List<Anim>> anlist in mAnims)
					{
						foreach(Anim an in anlist.Value)
						{
							if(an.GetNodeName() == bn)
							{
								//animate to the time
								//a key may not exist here but
								//the animator will interpolate
								an.Animate(keyTimesPerBone[bn][key]);
							}
						}
					}

					//grab the animated bone and decompose it
					foreach(KeyValuePair<string, SceneNode> sn in nodes)
					{
						Matrix	mat;
						if(sn.Value.GetMatrixForBoneNonRecursive(bn, out mat))
						{
							Quaternion	r;
							Vector3		s, t;

							if(!mat.Decompose(out s, out r, out t))
							{
								Debug.WriteLine("Matrix decomposition failed in BuildGameAnims()!");
							}
//							perBoneAnims[bn].AddKey(key, keyTimesPerBone[bn][key], r, s, t);
							perBoneAnims[bn].AddKey(key, keyTimesPerBone[bn][key], mat);
							break;
						}
					}
				}
			}
			return	perBoneAnims;
		}*/
	}
}