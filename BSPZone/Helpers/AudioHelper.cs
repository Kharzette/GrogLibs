using System;
using System.Text;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace BSPZone
{
	public class AudioHelper
	{
		Zone	mZone;
//		Audio	mAudio;

		List<ZoneEntity>			mAudioEntities	=new List<ZoneEntity>();
//		List<AudioEmitter>			mEmitters		=new List<AudioEmitter>();
//		List<SoundEffectInstance>	mInstances		=new List<SoundEffectInstance>();

		bool	mbMiscListening;


		public void Initialize(Zone zone, TriggerHelper th)//, Audio aud)
		{
			if(mZone != null)
			{
				//if changing level, release previous instances
//				foreach(SoundEffectInstance sei in mInstances)
//				{
//					sei.Stop();
//					mAudio.ReleaseInstance(sei);
//				}
			}
			mZone	=zone;
//			mAudio	=aud;

			//be sure to only wire this up once!
			//can cause double toggling
			if(!mbMiscListening)
			{
				th.eMisc		+=OnTriggerMisc;
				mbMiscListening	=true;
			}

			mAudioEntities.Clear();
//			mEmitters.Clear();
//			mInstances.Clear();

			//track index
			int	index	=0;

			//grab out audio emitters
			List<ZoneEntity>	sounds	=mZone.GetEntitiesStartsWith("misc_sound");
			foreach(ZoneEntity ze in sounds)
			{
				mAudioEntities.Add(ze);

				Vector3	pos;
				ze.GetOrigin(out pos);

//				AudioEmitter	em	=new AudioEmitter();
//				em.Position	=pos * Audio.InchWorldScale;
//				mEmitters.Add(em);

				string	fxName;
				fxName	=ze.GetValue("effect_name");

				int		looping;
				ze.GetInt("looping", out looping);

				float	volume;
				ze.GetFloat("volume", out volume);
				
//				SoundEffectInstance	sei	=mAudio.GetInstance(fxName, (looping != 0));
//				sei.Volume	=volume;
//				sei.Apply3D(mAudio.mListener, em);

//				mInstances.Add(sei);

				string	sOn	=ze.GetValue("activated");
				if(sOn != "0")
				{
//					sei.Play();
				}

				ze.SetInt("InstanceIndex", index);
				index++;
			}
		}


		public void Update()
		{
//			for(int i=0;i < mInstances.Count;i++)
//			{
//				SoundEffectInstance	sei	=mInstances[i];
//				if(sei.State == SoundState.Playing)
//				{
//					sei.Apply3D(mAudio.mListener, mEmitters[i]);
//				}
//			}
		}


		void OnTriggerMisc(object sender, EventArgs ea)
		{
			ZoneEntity	ze	=sender as ZoneEntity;
			if(ze == null)
			{
				return;
			}

			string	className	=ze.GetValue("classname");
			if(!className.StartsWith("misc_sound"))
			{
				return;
			}

			int	index	=0;
			if(!ze.GetInt("InstanceIndex", out index))
			{
				return;
			}

//			SoundEffectInstance	sei	=mInstances[index];

//			if(sei.State == SoundState.Playing)
//			{
//				sei.Stop();
//			}
//			else
//			{
//				sei.Play();
//			}

//			ze.ToggleEntityActivated();
		}
	}
}
