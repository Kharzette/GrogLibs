using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using SharpDX.Multimedia;
using UtilityLib;


namespace AudioLib
{
	public class Audio
	{
		XAudio2			mXAud	=new XAudio2();
		MasteringVoice	mMV;
		X3DAudio		m3DAud;

		//the listener everyclass will use
		Listener	mListener;

		//stuff loaded from disk
		Dictionary<string, SoundEffect>	mFX	=new Dictionary<string, SoundEffect>();

		//list of stuff out in the wild, might be playing, might not
		List<SoundEffectInstance>	mActive	=new List<SoundEffectInstance>();

		//list of stuff this class is managing
		Dictionary<string, List<SoundEffectInstance>>	mPlayingHere
			=new Dictionary<string, List<SoundEffectInstance>>();

		//list of 3d sounds currently being updated
		Dictionary<Emitter, List<SoundEffectInstance>>	m3DHere
			=new Dictionary<Emitter, List<SoundEffectInstance>>();

		const int	MaxInstances	=300;	//xbox limitation


		public Audio()
		{
			mMV	=new MasteringVoice(mXAud);

			DeviceDetails	det	=mXAud.GetDeviceDetails(0);

			m3DAud	=new X3DAudio(det.OutputFormat.ChannelMask);

			mListener	=new Listener();

			mListener.OrientFront	=Vector3.UnitZ;
			mListener.OrientTop		=Vector3.UnitY;
			mListener.Position		=Vector3.Zero;
			mListener.Velocity		=Vector3.Zero;

			mXAud.StartEngine();
		}


		public void FreeAll()
		{
			mXAud.StopEngine();

			mMV.Dispose();
			mXAud.Dispose();
		}
		
		
		public void LoadSound(string name, string fileName)
		{
			if(mFX.ContainsKey(name))
			{
				return;
			}

			FileStream	fs	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(fs == null)
			{
				return;
			}

			SoundStream	ss	=new SoundStream(fs);

			AudioBuffer	ab	=new AudioBuffer();

			ab.Stream		=ss;
			ab.AudioBytes	=(int)ss.Length;
			ab.Flags		=BufferFlags.EndOfStream;

			SoundEffect	se	=new SoundEffect(ab, ss.DecodedPacketsInfo, ss.Format);

			mFX.Add(name, se);

			ss.Close();
			fs.Close();
		}


		internal SoundEffectInstance GetInstance(string name, bool bLooping, bool b3D)
		{
			if(!mFX.ContainsKey(name) || mActive.Count >= MaxInstances)
			{
				return	null;
			}

			SoundEffect	se	=mFX[name];

			SoundEffectInstance	sei	=new SoundEffectInstance(se.GetSourceVoice(mXAud, bLooping), b3D);

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
			
			foreach(KeyValuePair<Emitter, List<SoundEffectInstance>> threeD in m3DHere)
			{
				threeD.Value.Clear();
			}
			m3DHere.Clear();
		}


		internal void ReleaseInstance(SoundEffectInstance sei)
		{
			if(sei == null)
			{
				return;
			}

			mActive.Remove(sei);

			sei.Stop();
			sei.Free();
		}


		public int GetNumInstances()
		{
			return	mActive.Count();
		}


		public void Update(GameCamera cam)
		{
			//update listener
			mListener.Position		=cam.Position;// * Audio.InchWorldScale;
			mListener.OrientFront	=-cam.Forward;

			List<string>	toNuke	=new List<string>();
			foreach(KeyValuePair<string, List<SoundEffectInstance>> seis in mPlayingHere)
			{
				if(seis.Value.Count == 0)
				{
					toNuke.Add(seis.Key);
				}

				for(int i=0;i < seis.Value.Count;i++)
				{
					SoundEffectInstance	sei	=seis.Value[i];
					if(!sei.IsPlaying())
					{
						ReleaseInstance(seis.Value[i]);
						seis.Value.RemoveAt(i);
						i--;
					}
				}
			}

			foreach(string key in toNuke)
			{
				mPlayingHere.Remove(key);
			}

			List<Emitter>	emToNuke	=new List<Emitter>();
			foreach(KeyValuePair<Emitter, List<SoundEffectInstance>> threeD in m3DHere)
			{
				if(threeD.Value.Count == 0)
				{
					emToNuke.Add(threeD.Key);
				}

				for(int i=0;i < threeD.Value.Count;i++)
				{
					SoundEffectInstance	sei	=threeD.Value[i];

					if(!sei.IsPlaying())
					{
						ReleaseInstance(sei);
						threeD.Value.RemoveAt(i);
						i--;
						continue;
					}

					//this is for mono sounds TODO: assert
					//The zerocenter flag below I think shouldn't be needed
					//but without it there is an audio "hole" when facing toward
					//a sound
					DspSettings	dsp	=m3DAud.Calculate(mListener, threeD.Key,
						CalculateFlags.Matrix | CalculateFlags.Doppler
						| CalculateFlags.ZeroCenter,	//TODO: Investigate / bugreport
						1, 1);

					sei.Update3D(dsp);
				}
			}

			foreach(Emitter em in emToNuke)
			{
				m3DHere.Remove(em);
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


		//fire and forget play from Emitter, no looping tracked in this class
		public void PlayAtLocation(string name, float volume, Emitter em)
		{
			SoundEffectInstance	sei	=GetInstance(name, false, true);
			if(sei == null)
			{
				return;
			}

			sei.SetVolume(volume);

			//this is for mono sounds TODO: assert
			//The zerocenter flag below I think shouldn't be needed
			//but without it there is an audio "hole" when facing toward
			//a sound
			DspSettings	dsp	=m3DAud.Calculate(mListener, em,
				CalculateFlags.Matrix | CalculateFlags.Doppler
				| CalculateFlags.ZeroCenter,	//TODO: Investigate / bugreport
				1, 1);

			sei.Play();

			sei.Update3D(dsp);

			lock(mActive)
			{
				mActive.Add(sei);
			}

			lock(m3DHere)
			{
				if(m3DHere.ContainsKey(em))
				{
					m3DHere[em].Add(sei);
				}
				else
				{
					m3DHere.Add(em, new List<SoundEffectInstance>());
					m3DHere[em].Add(sei);
				}
			}
		}


		//tracked in this class
		public void Play(string name, bool bLooping, float volume)
		{
			if(!mFX.ContainsKey(name) || mActive.Count >= MaxInstances)
			{
				return;
			}

			SoundEffect	se	=mFX[name];

			SoundEffectInstance	sei	=new SoundEffectInstance(se.GetSourceVoice(mXAud, bLooping), false);

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

			sei.SetVolume(volume);
			sei.Play();
		}


		public void LoadAllSounds(string dir)
		{
			if(!Directory.Exists(dir))
			{
				return;
			}
			DirectoryInfo	di	=new DirectoryInfo(dir + "/");

			FileInfo[]		fi	=di.GetFiles("*.wav", SearchOption.TopDirectoryOnly);
			foreach(FileInfo f in fi)
			{
				//strip back
				string	path	=f.DirectoryName;
				string	extLess	=FileUtil.StripExtension(f.Name);

				LoadSound(extLess, path + "\\" + f.Name);
			}
		}
	}
}
