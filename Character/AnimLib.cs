using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;

namespace Character
{
	public class AnimLib
	{
		Dictionary<string, Anim>	mAnims	=new Dictionary<string,Anim>();

		Skeleton	mSkeleton;


		public void AddAnim(Anim an)
		{
			mAnims.Add(an.Name, an);
		}


		public Skeleton GetSkeleton()
		{
			return	mSkeleton;
		}


		public void SetSkeleton(Skeleton sk)
		{
			mSkeleton	=sk;
		}


		//used mainly by gui
		public List<Anim>	GetAnims()
		{
			List<Anim>	ret	=new List<Anim>();

			foreach(KeyValuePair<string, Anim> anms in mAnims)
			{
				ret.Add(anms.Value);
			}
			return	ret;
		}


		//this only saves referenced stuff
		//the tool side will still need to enumerate
		//all the textures / shaders
		public void SaveToFile(string fileName)
		{
			FileStream	file	=MaterialLib.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying anim libs
			UInt32	magic	=0xA91BA7E;

			bw.Write(magic);

			//save skeleton
			mSkeleton.Write(bw);

			//write number of anims
			bw.Write(mAnims.Count);

			foreach(KeyValuePair<string, Anim> mat in mAnims)
			{
				mat.Value.Write(bw);
			}

			bw.Close();
			file.Close();
		}


		public bool ReadFromFile(string fileName)
		{
			FileStream	file	=MaterialLib.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			BinaryReader	br	=new BinaryReader(file);

			//clear existing data
			mAnims.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xA91BA7E)
			{
				return	false;
			}

			mSkeleton	=new Skeleton();
			mSkeleton.Read(br);

			int	numAnims	=br.ReadInt32();

			for(int i=0;i < numAnims;i++)
			{
				Anim	an	=new Anim();

				an.Read(br);

				mAnims.Add(an.Name, an);
			}

			//point channels at the skeleton
			foreach(KeyValuePair<string, Anim> an in mAnims)
			{
				an.Value.FixChannels(mSkeleton);
			}

			br.Close();
			file.Close();

			return	true;
		}


		//only used by the tools, this makes sure
		//the names used as keys in the dictionaries
		//match the name properties in the objects
		public void UpdateDictionaries()
		{
			restart:
			foreach(KeyValuePair<string, Anim> an in mAnims)
			{
				if(an.Key != an.Value.Name)
				{
					mAnims.Remove(an.Key);
					mAnims.Add(an.Value.Name, an.Value);
					goto restart;
				}
			}
		}


		public void Animate(string anim, float time)
		{
			if(mAnims.ContainsKey(anim))
			{
				mAnims[anim].Animate(time);
			}
//			BuildBones();
		}
	}
}