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
		private Dictionary<string, Source>	mSources	=new Dictionary<string,Source>();
		private Dictionary<string, Sampler>	mSamplers	=new Dictionary<string,Sampler>();
		private List<Channel>				mChannels	=new List<Channel>();



		internal List<float> GetTimesForBone(string bone)
		{
			List<float>	ret	=new List<float>();

			foreach(Channel chan in mChannels)
			{
				//extract the node name and address
				int		sidx	=chan.mTarget.IndexOf('/');
				string	nName	=chan.mTarget.Substring(0, sidx);

				if(nName != bone)
				{
					continue;
				}

				//grab sampler key
				string	sampKey	=chan.mSource;

				//strip #
				sampKey	=sampKey.Substring(1);

				Sampler	samp	=mSamplers[sampKey];
				string	srcInp	=samp.GetSourceForSemantic("INPUT");

				srcInp	=srcInp.Substring(1);

				List<float>	srcTimes	=mSources[srcInp].GetFloatArray();

				foreach(float time in srcTimes)
				{
					if(ret.Contains(time))
					{
						continue;
					}
					ret.Add(time);
				}
			}

			return	ret;
		}


		float LerpValue(float time, List<float> chanTimes, List<float> chanValues)
		{
			//calc totaltime
			float	totalTime	=chanTimes[chanTimes.Count - 1]
				- chanTimes[0];

			//make sure the time is not before our start
			Debug.Assert(time >= chanTimes[0]);

			//bring the passed in time value into
			//the space of our animation
			float	animTime	=time % totalTime;

			//Bring to start
			animTime	+=chanTimes[0];

			//locate the key index to start with
			int	startIndex;
			for(startIndex = 0;startIndex < chanTimes.Count;startIndex++)
			{
				if(animTime < chanTimes[startIndex])
				{
					//back up one
					startIndex--;
					break;	//found
				}
			}

			//figure out the percentage between pos1 and pos2
			//get the deltatime
			float	percentage	=chanTimes[startIndex + 1] - chanTimes[startIndex];

			//convert to percentage
			percentage	=1.0f / percentage;

			//multiply by amount beyond p1
			percentage	*=(animTime - chanTimes[startIndex]);

			Debug.Assert(percentage >= 0.0f && percentage <= 1.0f);

			float value	=MathHelper.Lerp(chanValues[startIndex],
				chanValues[startIndex + 1], percentage);

			return	value;
		}


		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "source")
				{
					Source	src	=new Source();
					r.MoveToFirstAttribute();
					string	srcID	=r.Value;
					src.Load(r);
					mSources.Add(srcID, src);
				}
				else if(r.Name == "sampler")
				{
					Sampler	samp	=new Sampler();
					r.MoveToFirstAttribute();
					string	sampID	=r.Value;
					samp.Load(r);
					mSamplers.Add(sampID, samp);
				}
				else if(r.Name == "channel")
				{
					Channel	chan	=new Channel();

					int	attCnt	=r.AttributeCount;
					r.MoveToFirstAttribute();
					while(attCnt > 0)
					{
						if(r.Name == "source")
						{
							chan.mSource	=r.Value;
						}
						else if(r.Name == "target")
						{
							chan.mTarget	=r.Value;
						}
						r.MoveToNextAttribute();
						attCnt--;
					}
					mChannels.Add(chan);
				}
				else if(r.Name == "animation")
				{
					return;
				}
			}
		}


		internal void SetKeys(string bone, List<float> times, List<MeshLib.KeyFrame> keys)
		{
			foreach(Channel chan in mChannels)
			{
				//extract the node name and address
				int		sidx	=chan.mTarget.IndexOf('/');
				string	nName	=chan.mTarget.Substring(0, sidx);

				if(nName != bone)
				{
					continue;
				}

				//grab sampler key
				string	sampKey	=chan.mSource;

				//strip #
				sampKey	=sampKey.Substring(1);

				Sampler	samp	=mSamplers[sampKey];

				string	srcInp	=samp.GetSourceForSemantic("INPUT");
				string	srcOut	=samp.GetSourceForSemantic("OUTPUT");
				string	srcC1	=samp.GetSourceForSemantic("IN_TANGENT");
				string	srcC2	=samp.GetSourceForSemantic("OUT_TANGENT");

				//strip #
				srcInp	=srcInp.Substring(1);
				srcOut	=srcOut.Substring(1);
				srcC1	=srcC1.Substring(1);
				srcC2	=srcC2.Substring(1);

				//extract the node name and address
				string	addr	=chan.mTarget.Substring(sidx + 1);
				int		pidx	=addr.IndexOf('.');
				string	elName	=addr.Substring(0, pidx);

				addr	=addr.Substring(pidx + 1);

				List<float>	chanTimes	=mSources[srcInp].GetFloatArray();
				List<float>	chanValues	=mSources[srcOut].GetFloatArray();
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