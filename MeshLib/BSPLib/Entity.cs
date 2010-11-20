using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class Entity
	{
		public Dictionary<string, string>	mData		=new Dictionary<string,string>();
		public List<Brush>					mBrushes	=new List<Brush>();


		public Entity()	{}


		public bool GetOrigin(out Vector3 org)
		{
			org	=Vector3.Zero;
			if(!mData.ContainsKey("origin"))
			{
				return	false;
			}
			string	[]szVec	=mData["origin"].Split(' ');
			if(szVec.Length != 3)
			{
				return	false;
			}

			if(!Single.TryParse(szVec[0], out org.X))
			{
				return	false;
			}
			if(!Single.TryParse(szVec[1], out org.Y))
			{
				return	false;
			}
			if(!Single.TryParse(szVec[2], out org.Z))
			{
				return	false;
			}
			//flip x
			org.X	=-org.X;

			//swap y and z
			float	zTemp	=org.Z;
			org.Z	=org.Y;
			org.Y	=zTemp;

			return	true;
		}


		public bool GetLightValue(out float dist)
		{
			dist	=250;

			if(mData.ContainsKey("light"))
			{
				if(!Single.TryParse(mData["light"], out dist))
				{
					return	false;
				}
				return	true;
			}
			return	false;
		}


		public bool GetLightValue(out Vector4 val)
		{
			val	=Vector4.Zero;

			if(mData.ContainsKey("_light"))
			{
				string	[]elements	=mData["_light"].Split(' ');

				Single.TryParse(elements[0], out val.X);
				Single.TryParse(elements[1], out val.Y);
				Single.TryParse(elements[2], out val.Z);
				Single.TryParse(elements[3], out val.W);
				return	true;
			}
			return	false;
		}


		//omni lights use position as a vector
		public bool IsLightOmni()
		{
			return	mData.ContainsKey("omni");
		}


		public bool IsLightEnvironment()
		{
			return	(mData["classname"] == "light_environment");
		}


		public bool GetColor(out Vector3 color)
		{
			color	=Vector3.One;
			string	val	="";
			if(mData.ContainsKey("color"))
			{
				val	=mData["color"];
			}
			else if(mData.ContainsKey("_color"))
			{
				val	=mData["_color"];
			}
			else
			{
				return	false;
			}
			string	[]szVec	=val.Split(' ');

			if(szVec.Length != 3)
			{
				return	false;
			}

			if(!Single.TryParse(szVec[0], out color.X))
			{
				return	false;
			}
			if(!Single.TryParse(szVec[1], out color.Y))
			{
				return	false;
			}
			if(!Single.TryParse(szVec[2], out color.Z))
			{
				return	false;
			}
			return	true;
		}


		internal void ReadFromMap(StreamReader sr)
		{
			string	s	="";
			Brush	b	=null;

			bool	brushComing	=false;
			bool	patchComing	=false;
			bool	patchBrush	=false;

			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					mData.Add(tokens[1], tokens[3]);
				}
				else if(s.StartsWith("{"))
				{
					if(!patchComing)
					{
						//brush coming I think
						b			=new Brush();
						brushComing	=true;
					}
				}
				else if(s.StartsWith("}"))
				{
					if(brushComing)
					{
						brushComing	=false;

						//seal the brush
						b.SealFaces();

						if(b.IsValid())
						{
							mBrushes.Add(b);
						}
					}
					else if(patchComing)
					{
						patchComing	=false;
					}
					else if(patchBrush)
					{
						patchBrush	=false;	//I'll support these someday maybe
					}
					else
					{
						return;	//entity done
					}
				}
				else if(s.StartsWith("("))
				{
					if(brushComing)
					{
						b.MakeFaceFromMapLine(s);
					}
				}
				else if(s.StartsWith("patchDef2"))
				{
					brushComing	=false;
					patchComing	=true;
					patchBrush	=true;
					b			=null;
				}
			}
		}


		//read a single entity block
		internal void ReadVMFEntBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					if(mData.ContainsKey(tokens[1]))
					{
						mData[tokens[1]]	=tokens[3];
					}
					else
					{
						mData.Add(tokens[1], tokens[3]);
					}
				}
				else if(s == "editor")
				{
					Brush.SkipVMFEditorBlock(sr);
				}
				else if(s == "solid")
				{
					Brush	b	=new Brush();

					if(b.ReadVMFSolidBlock(sr))
					{
						b.SealFaces();
						mBrushes.Add(b);
					}
				}
				else if(s == "connections")
				{
					Brush.SkipVMFEditorBlock(sr);
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		void SkipVMFGroupBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("}"))
				{
					return;	//skip done
				}
				else if(s == "editor")
				{
					Brush.SkipVMFEditorBlock(sr);
				}
			}
		}


		//read a single entity block
		internal void ReadVMFWorldBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "solid")
				{
					Brush	b	=new Brush();

					if(b.ReadVMFSolidBlock(sr))
					{
						b.SealFaces();
						mBrushes.Add(b);
					}
				}
				else if(s == "group")
				{
					SkipVMFGroupBlock(sr);
				}
				else if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					mData.Add(tokens[1], tokens[3]);
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		//read's hammer files
		internal void ReadFromVMF(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "entity")
				{
					ReadVMFEntBlock(sr);
					return;
				}
				else if(s == "world")
				{
					ReadVMFWorldBlock(sr);
					return;
				}
			}
		}


		internal void Read(BinaryReader br)
		{
			int	dataCount	=br.ReadInt32();

			mData	=new Dictionary<string, string>();
			for(int i=0;i < dataCount;i++)
			{
				string	key		=br.ReadString();
				string	value	=br.ReadString();

				mData.Add(key, value);
			}
		}


		internal void Write(BinaryWriter bw)
		{
			//write out # of key value pairs
			bw.Write(mData.Count);

			foreach(KeyValuePair<string, string> pair in mData)
			{
				bw.Write(pair.Key);
				bw.Write(pair.Value);
			}
		}
	}
}
