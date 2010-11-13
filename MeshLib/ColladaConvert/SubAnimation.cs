using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	//animations on individual controllers
	public class SubAnimation
	{
		private Dictionary<string, source>	mSources	=new Dictionary<string, source>();
		private Dictionary<string, sampler>	mSamplers	=new Dictionary<string, sampler>();
		private List<channel>				mChannels	=new List<channel>();


		public SubAnimation(animation anim)
		{
			foreach(object anObj in anim.Items)
			{
				if(anObj is source)
				{
					source	src	=anObj as source;
					mSources.Add(src.id, src);
				}
				else if(anObj is sampler)
				{
					sampler	samp	=anObj as sampler;
					mSamplers.Add(samp.id, samp);
				}
				else if(anObj is channel)
				{
					channel	chan	=anObj as channel;
					mChannels.Add(chan);
				}
			}
		}


		string GetSourceForSemantic(sampler samp, string sem)
		{
			string	srcInp	="";
			foreach(InputLocal inp in samp.input)
			{
				if(inp.semantic == sem)
				{
					srcInp	=inp.source.Substring(1);
				}
			}
			return	srcInp;
		}


		internal List<float> GetTimesForBone(string bone)
		{
			List<float>	ret	=new List<float>();

			foreach(channel chan in mChannels)
			{
				//extract the node name and address
				int		sidx	=chan.target.IndexOf('/');
				string	nName	=chan.target.Substring(0, sidx);

				if(nName != bone)
				{
					continue;
				}

				//grab sampler key
				string	sampKey	=chan.source;

				//strip #
				sampKey	=sampKey.Substring(1);

				sampler	samp	=mSamplers[sampKey];
				string	srcInp	=GetSourceForSemantic(samp, "INPUT");

				float_array	srcTimes	=mSources[srcInp].Item as float_array;

				foreach(float time in srcTimes.Values)
				{
					float	t	=time;
					if(ret.Contains(t))
					{
						continue;
					}
					ret.Add(t);
				}
			}

			return	ret;
		}


		float LerpValue(float time, float_array chanTimes, float_array chanValues)
		{
			//calc totaltime
			float	totalTime	=chanTimes.Values[chanTimes.Values.Length - 1]
				- chanTimes.Values[0];

			//make sure the time is not before our start
			Debug.Assert(time >= chanTimes.Values[0]);

			//bring the passed in time value into
			//the space of our animation
			float	animTime	=time % totalTime;

			//Bring to start
			animTime	+=chanTimes.Values[0];

			//locate the key index to start with
			int	startIndex;
			for(startIndex = 0;startIndex < chanTimes.Values.Length;startIndex++)
			{
				if(animTime < chanTimes.Values[startIndex])
				{
					//back up one
					startIndex--;
					break;	//found
				}
			}

			//figure out the percentage between pos1 and pos2
			//get the deltatime
			float	percentage	=chanTimes.Values[startIndex + 1]
				- chanTimes.Values[startIndex];

			//convert to percentage
			percentage	=1.0f / percentage;

			//multiply by amount beyond p1
			percentage	*=(animTime - chanTimes.Values[startIndex]);

			Debug.Assert(percentage >= 0.0f && percentage <= 1.0f);

			float value	=MathHelper.Lerp(chanValues.Values[startIndex],
				chanValues.Values[startIndex + 1], percentage);

			return	value;
		}


		internal void SetKeys(string bone, List<float> times, List<MeshLib.KeyFrame> keys)
		{
			foreach(channel chan in mChannels)
			{
				//extract the node name and address
				int		sidx	=chan.target.IndexOf('/');
				string	nName	=chan.target.Substring(0, sidx);

				if(nName != bone)
				{
					continue;
				}

				//grab sampler key
				string	sampKey	=chan.source;

				//strip #
				sampKey	=sampKey.Substring(1);

				sampler	samp	=mSamplers[sampKey];

				string	srcInp	=GetSourceForSemantic(samp, "INPUT");
				string	srcOut	=GetSourceForSemantic(samp, "OUTPUT");
				string	srcC1	=GetSourceForSemantic(samp, "IN_TANGENT");
				string	srcC2	=GetSourceForSemantic(samp, "OUT_TANGENT");

				//extract the node name and address
				string	addr	=chan.target.Substring(sidx + 1);
				int		pidx	=addr.IndexOf('.');
				string	elName	=addr.Substring(0, pidx);

				addr	=addr.Substring(pidx + 1);

				float_array	chanTimes	=mSources[srcInp].Item as float_array;
				float_array	chanValues	=mSources[srcOut].Item as float_array;
				List<float>	outValues	=new List<float>();

				//grab values for this channel
				//along the overall list of times
				for(int tidx=0;tidx < times.Count;tidx++)
				{
					outValues.Add(LerpValue(times[tidx], chanTimes, chanValues));
				}

				//insert values into the proper spot
				for(int v=0;v < outValues.Count;v++)
				{
					float	val	=outValues[v];

					if(elName == "rotateX")
					{
						keys[v].mRotation.X	=val;
					}
					else if(elName == "rotateY")
					{
						keys[v].mRotation.Y	=val;
					}
					else if(elName == "rotateZ")
					{
						keys[v].mRotation.Z	=val;
					}
					else if(elName == "translate")
					{
						if(addr == "X")
						{
							keys[v].mPosition.X	=val;
						}
						else if(addr == "Y")
						{
							keys[v].mPosition.Y	=val;
						}
						else if(addr == "Z")
						{
							keys[v].mPosition.Z	=val;
						}
					}
					else if(elName == "scale")
					{
						if(addr == "X")
						{
							keys[v].mScale.X	=val;
						}
						else if(addr == "Y")
						{
							keys[v].mScale.Y	=val;
						}
						else if(addr == "Z")
						{
							keys[v].mScale.Z	=val;
						}
					}
				}

				//this will leave euler angles in
				//the quaternion, will fix in Animation.cs
			}
		}
	}
}