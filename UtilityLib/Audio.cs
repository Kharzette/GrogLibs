using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;


namespace UtilityLib
{
	public class Audio
	{
		Dictionary<string, SoundEffect>	mFX	=new Dictionary<string, SoundEffect>();

		//list of stuff out in the wild, might be playing, might not
		List<SoundEffectInstance>	mActive	=new List<SoundEffectInstance>();

		//list of stuff this class is managing
		Dictionary<string, List<SoundEffectInstance>>	mPlayingHere
			=new Dictionary<string, List<SoundEffectInstance>>();

		const int	MaxInstances	=300;	//xbox limitation

		public const float	MeterWorldScale	=0.5f;			//use this for game unit == meter
		public const float	InchWorldScale	=1.0f / 72f;	//for inches


		public void AddSound(string name, SoundEffect fx)
		{
			if(!mFX.ContainsKey(name))
			{
				mFX.Add(name, fx);
			}
		}


		public SoundEffectInstance GetInstance(string name, bool bLooping)
		{
			if(!mFX.ContainsKey(name) || mActive.Count >= MaxInstances)
			{
				return	null;
			}

			SoundEffect	se	=mFX[name];

			SoundEffectInstance	sei	=se.CreateInstance();

			sei.IsLooped	=bLooping;

			mActive.Add(sei);

			return	sei;
		}


		public void ReleaseAll()
		{
			List<SoundEffectInstance>	toNuke	=new List<SoundEffectInstance>(mActive);

			foreach(SoundEffectInstance sei in toNuke)
			{
				ReleaseInstance(sei);
			}

			Debug.Assert(mActive.Count == 0);

			mPlayingHere.Clear();
		}


		public void ReleaseInstance(SoundEffectInstance sei)
		{
			if(sei == null)
			{
				return;
			}

			mActive.Remove(sei);

			sei.Stop(true);
			sei.Dispose();
		}


		public int GetNumInstances()
		{
			return	mActive.Count();
		}


		public void Update()
		{
			foreach(KeyValuePair<string, List<SoundEffectInstance>> seis in mPlayingHere)
			{
				for(int i=0;i < seis.Value.Count;i++)
				{
					if(seis.Value[i].State == SoundState.Stopped)
					{
						ReleaseInstance(seis.Value[i]);
						seis.Value.RemoveAt(i);
						i--;
					}
				}
			}
		}


		public void StopSound(string name)
		{
			if(!mPlayingHere.ContainsKey(name))
			{
				return;
			}

			foreach(SoundEffectInstance sei in mPlayingHere[name])
			{
				sei.Stop();
			}
		}


		//tracked in this class
		public void Play(string name, bool bLooping, float volume, float pan)
		{
			if(!mFX.ContainsKey(name) || mActive.Count >= MaxInstances)
			{
				return;
			}

			SoundEffect	se	=mFX[name];

			SoundEffectInstance	sei	=se.CreateInstance();

			//todo:  This looks like maybe a race condition could happen
			lock(mActive)
			{
				mActive.Add(sei);
			}
			lock(mPlayingHere)
			{
				if(mPlayingHere.ContainsKey(name))
				{
					mPlayingHere[name].Add(sei);
				}
				else
				{
					mPlayingHere.Add(name, new List<SoundEffectInstance>());
					mPlayingHere[name].Add(sei);
				}
			}

			sei.Pan			=pan;
			sei.Volume		=volume;
			sei.IsLooped	=bLooping;
			sei.Play();
		}


		//tracked in this class
		public void Play(string name, bool bLooping, float volume)
		{
			Play(name, bLooping, volume, 0.0f);
		}
	}
}
