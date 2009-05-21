using System;
using System.Collections.Generic;
using System.Xml;
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
	}
}