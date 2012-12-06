using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using UtilityLib;


namespace BSPZone
{
	public class AudioHelper
	{
		Zone			mZone;
		Audio			mAudio;
		AudioListener	mPlayerListener;

		List<ZoneEntity>			mAudioEntities	=new List<ZoneEntity>();
		List<AudioEmitter>			mEmitters		=new List<AudioEmitter>();
		List<SoundEffectInstance>	mInstances		=new List<SoundEffectInstance>();


		public void Initialize(Zone zone, Audio aud, AudioListener playerListener)
		{
			mZone			=zone;
			mAudio			=aud;
			mPlayerListener	=playerListener;

			mAudioEntities.Clear();
			mEmitters.Clear();
			mInstances.Clear();

			//grab out audio emitters
			List<ZoneEntity>	sounds	=mZone.GetEntitiesStartsWith("misc_sound");
			foreach(ZoneEntity ze in sounds)
			{
				mAudioEntities.Add(ze);

				Vector3	pos;
				ze.GetOrigin(out pos);

				AudioEmitter	em	=new AudioEmitter();
				em.Position	=pos;
				mEmitters.Add(em);

				string	fxName;
				fxName	=ze.GetValue("effect_name");

				int		looping;
				ze.GetInt("looping", out looping);

				float	volume;
				ze.GetFloat("volume", out volume);
				
				SoundEffectInstance	sei	=mAudio.GetInstance(fxName, (looping != 0));
				sei.Volume	=volume;
				sei.Apply3D(playerListener, em);

				mInstances.Add(sei);

				string	sOn	=ze.GetValue("turned_on");
				if(sOn != "0")
				{
					sei.Play();
				}
			}
		}


		public void Update()
		{
			for(int i=0;i < mInstances.Count;i++)
			{
				SoundEffectInstance	sei	=mInstances[i];
				if(sei.State == SoundState.Playing)
				{
					sei.Apply3D(mPlayerListener, mEmitters[i]);
				}
			}
		}
	}
}
