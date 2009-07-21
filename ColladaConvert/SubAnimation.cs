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


		public List<Anim> GetAnims(Dictionary<string, SceneNode> roots)
		{
			List<Anim>	ret	=new List<Anim>();
			foreach(Channel chan in mChannels)
			{
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
				int		sidx	=chan.mTarget.IndexOf('/');
				string	nName	=chan.mTarget.Substring(0, sidx);
				string	addr	=chan.mTarget.Substring(sidx + 1);
				int		pidx	=addr.IndexOf('.');
				string	elName	=addr.Substring(0, pidx);

				addr	=addr.Substring(pidx + 1);

				//find the element in the nodes
				NodeElement	ne	=null;
				foreach(KeyValuePair<string, SceneNode> root in roots)
				{
					if(root.Value.GetElement(nName, elName, out ne))
					{
						//element found

						//make a creation params struct for the factory
						AnimCreationParameters	prm	=new AnimCreationParameters();
						prm.mTimes		=mSources[srcInp].GetFloatArray();
						prm.mValues		=mSources[srcOut].GetFloatArray();
						prm.mControl1	=mSources[srcC1].GetFloatArray();
						prm.mControl2	=mSources[srcC2].GetFloatArray();
						prm.mOperand	=ne;

						Anim	a	=(Anim)Activator.CreateInstance(ne.GetAnimatorType(addr), prm);

						ret.Add(a);
						break;
					}
				}
			}
			return	ret;
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
	}
}