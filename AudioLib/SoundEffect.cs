using System;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Multimedia;
using Vortice.XAudio2;


namespace AudioLib
{
	internal class SoundEffectInstance
	{
		bool				mbPlaying, mb3D;
		IXAudio2SourceVoice	mSourceVoice;


		internal SoundEffectInstance(IXAudio2SourceVoice sv, bool b3D)
		{
			mbPlaying		=false;
			mSourceVoice	=sv;
			mb3D			=b3D;

			mSourceVoice.StreamEnd	+=OnStreamEnd;
		}


		internal bool IsPlaying()
		{
			return	mbPlaying;
		}


		internal void SetVolume(float volume)
		{
			mSourceVoice.SetVolume(volume);
		}


		internal void Play()
		{
			mSourceVoice.Start();
			mbPlaying	=true;
		}


		internal void Update3D(DspSettings dsp)
		{
			Debug.Assert(mb3D);

			//this is possible
			if(!mbPlaying)
			{
				return;
			}

			mSourceVoice.SetOutputMatrix(dsp.SourceChannelCount, dsp.DestinationChannelCount, dsp.MatrixCoefficients);
			mSourceVoice.SetFrequencyRatio(dsp.DopplerFactor, 0);	//TODO: no idea what 2nd par is
		}


		internal void Stop()
		{
			mSourceVoice.Stop();
			mbPlaying	=false;
		}


		internal void Free()
		{
			mSourceVoice.StreamEnd	-=OnStreamEnd;

			mSourceVoice.Stop();
			mbPlaying	=false;

			mSourceVoice.DestroyVoice();
			mSourceVoice.Dispose();
		}


		void OnStreamEnd()
		{
			mbPlaying	=false;
		}
	}


	internal class SoundEffect
	{
		AudioBuffer	mBuffer;
		WaveFormat	mFormat;


		internal SoundEffect(AudioBuffer ab, WaveFormat wf)
		{
			mBuffer		=ab;
			mFormat		=wf;
		}


		internal IXAudio2SourceVoice GetSourceVoice(IXAudio2 xaud, bool bLooping)
		{
			IXAudio2SourceVoice	sv	=xaud.CreateSourceVoice(mFormat, false);

			if(bLooping)
			{
				mBuffer.LoopCount	=255;	//loop forever, couldn't get teh symbol to work
			}
			else
			{
				mBuffer.LoopCount	=0;
			}

			sv.SubmitSourceBuffer(mBuffer, null);

			return	sv;
		}


		//simple fire and forget 2D
		internal void Play(IXAudio2 xaud, float volume)
		{
			IXAudio2SourceVoice	sv	=xaud.CreateSourceVoice(mFormat, false);

			sv.SubmitSourceBuffer(mBuffer, null);

			sv.SetVolume(volume);

			sv.Start();
		}
	}
}
