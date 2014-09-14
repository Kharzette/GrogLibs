using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using SharpDX.Multimedia;


namespace AudioLib
{
	internal class SoundEffectInstance
	{
		bool		mbPlaying, mb3D;
		SourceVoice	mSourceVoice;


		internal SoundEffectInstance(SourceVoice sv, bool b3D)
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
			Debug.Assert(mbPlaying);

			if(!mbPlaying)
			{
				return;
			}

			mSourceVoice.SetOutputMatrix(dsp.SourceChannelCount, dsp.DestinationChannelCount, dsp.MatrixCoefficients);
			mSourceVoice.SetFrequencyRatio(dsp.DopplerFactor);
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


		internal SourceVoice GetSourceVoice(XAudio2 xaud, bool bLooping)
		{
			SourceVoice	sv	=new SourceVoice(xaud, mFormat);

			if(bLooping)
			{
				mBuffer.LoopCount	=AudioBuffer.LoopInfinite;
			}
			else
			{
				mBuffer.LoopCount	=0;
			}

			sv.SubmitSourceBuffer(mBuffer, null);

			return	sv;
		}


		//simple fire and forget 2D
		internal void Play(XAudio2 xaud, float volume)
		{
			SourceVoice	sv	=new SourceVoice(xaud, mFormat);

			sv.SubmitSourceBuffer(mBuffer, null);

			sv.SetVolume(volume);

			sv.Start();
		}
	}
}
