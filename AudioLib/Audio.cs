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
		XAudio2			mXAud	=new XAudio2(XAudio2Flags.None, ProcessorSpecifier.DefaultProcessor, XAudio2Version.Version27);
		MasteringVoice	mMV;
		X3DAudio		m3DAud;
		DeviceDetails	mDetails;

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
			mDetails	=mXAud.GetDeviceDetails(0);

			mMV	=new MasteringVoice(mXAud, mDetails.OutputFormat.Channels);

			m3DAud	=new X3DAudio(mDetails.OutputFormat.ChannelMask);

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

			SoundEffect	se	=new SoundEffect(ab, ss.Format);

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

					CalculateFlags	calcFlags	=CalculateFlags.Matrix | CalculateFlags.Doppler;

					if((mDetails.OutputFormat.ChannelMask & Speakers.LowFrequency) != 0)
					{
						calcFlags	|=CalculateFlags.RedirectToLfe;
					}

					//this is for mono sounds TODO: assert
					DspSettings	dsp	=m3DAud.Calculate(mListener, threeD.Key,
						calcFlags, 1, mDetails.OutputFormat.Channels);

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


		public void StopEmitter(Emitter em)
		{
			if(m3DHere.ContainsKey(em))
			{
				foreach(SoundEffectInstance sei in m3DHere[em])
				{
					sei.Stop();
				}
			}
		}


		public void StopSound(object seiObj)
		{
			SoundEffectInstance	sei	=seiObj as SoundEffectInstance;
			if(sei == null)
			{
				return;
			}
			sei.Stop();
		}


		//fire and forget play from Emitter
		public object PlayAtLocation(string name, float volume, bool bLooping, Emitter em)
		{
			SoundEffectInstance	sei	=GetInstance(name, bLooping, true);
			if(sei == null)
			{
				return	null;
			}

			sei.SetVolume(volume);

			CalculateFlags	calcFlags	=CalculateFlags.Matrix | CalculateFlags.Doppler;

			if((mDetails.OutputFormat.ChannelMask & Speakers.LowFrequency) != 0)
			{
				calcFlags	|=CalculateFlags.RedirectToLfe;
			}

			//this is for mono sounds TODO: assert
			DspSettings	dsp	=m3DAud.Calculate(mListener, em,
				calcFlags, 1, mDetails.OutputFormat.Channels);

			sei.Play();

			sei.Update3D(dsp);

			if(m3DHere.ContainsKey(em))
			{
				m3DHere[em].Add(sei);
			}
			else
			{
				m3DHere.Add(em, new List<SoundEffectInstance>());
				m3DHere[em].Add(sei);
			}
			return	sei;
		}


		//tracked in this class
		public object Play(string name, bool bLooping, float volume)
		{
			if(!mFX.ContainsKey(name) || mActive.Count >= MaxInstances)
			{
				return	null;
			}

			SoundEffect	se	=mFX[name];

			SoundEffectInstance	sei	=new SoundEffectInstance(se.GetSourceVoice(mXAud, bLooping), false);

			mActive.Add(sei);
			if(mPlayingHere.ContainsKey(name))
			{
				mPlayingHere[name].Add(sei);
			}
			else
			{
				mPlayingHere.Add(name, new List<SoundEffectInstance>());
				mPlayingHere[name].Add(sei);
			}

			sei.SetVolume(volume);
			sei.Play();

			return	sei;
		}


		public List<string> GetSoundList()
		{
			return	mFX.Keys.ToList();
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


		public static Emitter MakeEmitter(Vector3 loc)
		{
			Emitter	ret	=new Emitter();

			ret.Position			=loc;
			ret.OrientFront			=Vector3.ForwardRH;
			ret.OrientTop			=Vector3.Up;
			ret.Velocity			=Vector3.Zero;
			ret.CurveDistanceScaler	=50f;
			ret.ChannelCount		=1;
			ret.DopplerScaler		=1f;

			return	ret;
		}
	}
}
