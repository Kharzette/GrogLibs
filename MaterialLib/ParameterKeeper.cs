using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using UtilityLib;


namespace MaterialLib
{
	internal class ParameterKeeper
	{
		internal struct TriLight
		{
			internal Vector4	mColor0, mColor1, mColor2;
		}

		//references to texture dictionaries
		Dictionary<string, Texture2D>	mMaps	=new Dictionary<string, Texture2D>();
		Dictionary<string, TextureCube>	mCubes	=new Dictionary<string, TextureCube>();

		//parameters for the chosen shader
		List<ShaderParameters>	mParameters	=new List<ShaderParameters>();

		//the same stuff presented for a gui
		BindingList<ShaderParameters>	mGUIParameters	=new BindingList<ShaderParameters>();

		//stuff the gui doesn't want to see
		List<ShaderParameters>	mHidden	=new List<ShaderParameters>();

		//original trilight value
		TriLight	mOGTriLight;


		internal ParameterKeeper(Dictionary<string, Texture2D> maps,
			Dictionary<string, TextureCube> cubes)
		{
			mMaps	=maps;
			mCubes	=cubes;
		}


		//should only be used to tie in to a grid or something
		internal BindingList<ShaderParameters>	GetParametersForGUI()
		{
			return	mGUIParameters;
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mParameters.Count);
			foreach(ShaderParameters sp in mParameters)
			{
				sp.Write(bw);
			}

			bw.Write(mHidden.Count);
			foreach(ShaderParameters sp in mHidden)
			{
				bw.Write(mParameters.IndexOf(sp));
			}
		}


		internal void Read(BinaryReader br)
		{
			mParameters.Clear();

			int	count	=br.ReadInt32();
			for(int i=0;i < count;i++)
			{
				ShaderParameters	sp	=new ShaderParameters();

				sp.Read(br);

				//fix up texture pointers if need be
				FixTextures(sp);

				mParameters.Add(sp);
			}

			//read the hidden list
			List<int>	hidden	=new List<int>();

			int	hiddenCount	=br.ReadInt32();

			for(int i=0;i < hiddenCount;i++)
			{
				hidden.Add(br.ReadInt32());
			}

			for(int i=0;i < count;i++)
			{
				if(hidden.Contains(i))
				{
					mHidden.Add(mParameters[i]);
				}
			}

			UpdateGUI();

			//init OG trilight
			GetTriLight(out mOGTriLight);
		}


		internal void Hide(List<ShaderParameters> toHide)
		{
			foreach(ShaderParameters sp in toHide)
			{
				if(mHidden.Contains(sp))
				{
					continue;
				}
				mHidden.Add(sp);
			}

			UpdateGUI();
		}


		internal bool GetTriLight(out TriLight tri)
		{
			bool	bZero	=false;
			bool	bOne	=false;
			bool	bTwo	=false;

			tri.mColor0	=Vector4.Zero;
			tri.mColor1	=Vector4.Zero;
			tri.mColor2	=Vector4.Zero;

			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Class != EffectParameterClass.Vector)
				{
					continue;
				}

				if(sp.Name == "mLightColor0")
				{
					tri.mColor0	=(Vector4)sp.GetRealValue();
					bZero		=true;
				}
				else if(sp.Name == "mLightColor1")
				{
					tri.mColor1	=(Vector4)sp.GetRealValue();
					bOne		=true;
				}
				else if(sp.Name == "mLightColor2")
				{
					tri.mColor2	=(Vector4)sp.GetRealValue();
					bTwo		=true;
				}
			}
			return	(bZero && bOne && bTwo);
		}


		internal void SetTriLightValues(Vector4 lightColor, Vector3 lightDir)
		{
			TriLight	tri	=mOGTriLight;

			tri.mColor0	*=lightColor;
			tri.mColor1	*=lightColor;
			tri.mColor2	*=lightColor;

			tri.mColor0.W	=1f;
			tri.mColor1.W	=1f;
			tri.mColor2.W	=1f;

			SetTriLightValues(tri, lightDir);
		}


		void SetTriLightValues(TriLight tri, Vector3 lightDir)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Class != EffectParameterClass.Vector)
				{
					continue;
				}

				if(sp.Name == "mLightColor0")
				{
					sp.SetRealValue(tri.mColor0);
				}
				else if(sp.Name == "mLightColor1")
				{
					sp.SetRealValue(tri.mColor1);
				}
				else if(sp.Name == "mLightColor2")
				{
					sp.SetRealValue(tri.mColor2);
				}
				else if(sp.Name == "mLightDirection")
				{
					sp.SetRealValue(lightDir);
				}
			}
		}


		internal void SetParameter(string paramName, object value)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == paramName)
				{
					sp.SetRealValue(value);
					return;
				}
			}
		}


		internal void SetTextureParameterToCube(string name)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == name)
				{
					if(sp.Type == EffectParameterType.Texture)
					{
						sp.Type	=EffectParameterType.TextureCube;
					}
					return;
				}
			}
		}


		internal void GetTexturesInUse(List<string> tex)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Class != EffectParameterClass.Object)
				{
					continue;
				}

				object	val	=sp.GetRealValue();
				if(val is Texture2D)
				{
					string	sz	=sp.Value;
					if(sz == "")
					{
						continue;
					}

					if(tex.Contains(sz))
					{
						continue;
					}

					tex.Add(sp.Value);
				}
			}
		}


		internal void GetTextureCubesInUse(List<string> tex)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Class != EffectParameterClass.Object)
				{
					continue;
				}

				object	val	=sp.GetRealValue();
				if(val is TextureCube)
				{
					tex.Add(sp.Value);
				}
			}
		}


		internal object GetParameterValue(string name)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == name)
				{
					return	sp.Value;
				}
			}
			return	null;
		}


		//sets up an effect with the recorded material values
		internal void ApplyShaderParameters(Effect fx)
		{
			bool	bFixUpNeeded	=false;
			foreach(ShaderParameters sp in mParameters)
			{
				object			val	=sp.GetRealValue();
				EffectParameter	ep	=fx.Parameters[sp.Name];

				if(val == null)
				{
					continue;
				}
				if(ep == null)
				{
					continue;
				}
				switch(sp.Class)
				{
					case	EffectParameterClass.Matrix:
						if(val is Matrix)
						{
							ep.SetValue((Matrix)val);
						}
						else
						{
							ep.SetValue((Matrix [])val);
						}
						break;

					case	EffectParameterClass.Object:
						if(val is string)
						{
							//texture pointer hasn't been fixed up yet
							ep.SetValue((Texture)null);
							bFixUpNeeded	=true;
						}
						else
						{
							ep.SetValue((Texture)val);
						}
						break;

					case	EffectParameterClass.Scalar:
						if(sp.Type == EffectParameterType.Single)
						{
							if(ep.Elements.Count > 1)
							{
								ep.SetValue((float [])val);
							}
							else
							{
								ep.SetValue((float)val);
							}
						}
						else if(sp.Type == EffectParameterType.Bool)
						{
							ep.SetValue((bool)val);
						}
						break;

					case	EffectParameterClass.Vector:
						//get the number of columns
						if(ep.ColumnCount == 2)
						{
							ep.SetValue((Vector2)val);
						}
						else if(ep.ColumnCount == 3)
						{
							ep.SetValue((Vector3)val);
						}
						else if(ep.ColumnCount == 4)
						{
							ep.SetValue((Vector4)val);
						}
						else
						{
							Debug.Assert(false);
						}
						break;
				}
			}

			if(bFixUpNeeded)
			{
				foreach(ShaderParameters sp in mParameters)
				{
					FixTextures(sp);
				}
			}
		}


		//fills in our data from a shader
		internal void UpdateShaderParameters(Effect fx)
		{
			List<ShaderParameters>	parms	=new List<ShaderParameters>();

			foreach(EffectParameter ep in fx.Parameters)
			{
				ShaderParameters	sp	=new ShaderParameters();

				sp.Name		=ep.Name;
				sp.Class	=ep.ParameterClass;
				sp.Type		=ep.ParameterType;

				switch(sp.Class)
				{
					case	EffectParameterClass.Matrix:
						if(ep.Elements.Count > 1)
						{
							sp.SetRealValue(ep.GetValueMatrixArray(ep.Elements.Count));
							sp.SetCount(ep.Elements.Count);
						}
						else
						{
							sp.SetRealValue(ep.GetValueMatrix());
						}
						break;

					case	EffectParameterClass.Vector:
						if(ep.ColumnCount == 2)
						{
							sp.SetRealValue(ep.GetValueVector2());
							sp.SetCount(2);
						}
						else if(ep.ColumnCount == 3)
						{
							sp.SetRealValue(ep.GetValueVector3());
							sp.SetCount(3);
						}
						else
						{
							sp.SetRealValue(ep.GetValueVector4());
							sp.SetCount(4);
						}
						break;

					case	EffectParameterClass.Scalar:
						sp.SetCount(ep.ColumnCount);
						break;

					case	EffectParameterClass.Object:
						break;
				}
				parms.Add(sp);
			}

			//merge results
			//add any new parameters
			foreach(ShaderParameters newSp in parms)
			{
				bool	bFound	=false;
				foreach(ShaderParameters sp in mParameters)
				{
					if(sp.Name == newSp.Name)
					{
						bFound	=true;
					}
				}

				if(!bFound)
				{
					mParameters.Add(newSp);
				}
			}

			//gank any parameters that no longer exist
			//within the shader
			List<ShaderParameters>	gank	=new List<ShaderParameters>();
			foreach(ShaderParameters sp in mParameters)
			{
				bool	bFound	=false;
				{
					foreach(ShaderParameters newSp in parms)
					if(sp.Name == newSp.Name)
					{
						bFound	=true;
						break;
					}
				}

				if(!bFound)
				{
					gank.Add(sp);
				}
			}

			//gankery
			foreach(ShaderParameters sp in gank)
			{
				mParameters.Remove(sp);
			}

			UpdateGUI();
		}


		internal void AddParameter(string name, EffectParameterClass epc,
								EffectParameterType ept, int count, object value)
		{
			ShaderParameters	parm	=null;

			//see if the parameter already exists
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == name)
				{
					parm	=sp;
					break;
				}
			}

			bool	bNew	=false;

			if(parm == null)
			{
				bNew	=true;
				parm	=new ShaderParameters();
			}

			parm.Name	=name;
			parm.Class	=epc;
			parm.Type	=ept;

			parm.SetCount(count);
			parm.SetRealValue(value);

			if(bNew)
			{
				mParameters.Add(parm);
			}
		}


		void UpdateGUI()
		{
			mGUIParameters.Clear();

			foreach(ShaderParameters sp in mParameters)
			{
				if(mHidden.Contains(sp))
				{
					continue;
				}
				mGUIParameters.Add(sp);
			}
		}


		void FixTextures(ShaderParameters sp)
		{
			if(sp.Class != EffectParameterClass.Object)
			{
				return;
			}

			object	val	=sp.GetRealValue();
			if(val == null)
			{
				return;
			}

			if(val is Texture)
			{
				return;	//already fixed up
			}

			if(!(val is string))
			{
				return;
			}

			string	name	=val as string;

			if(mMaps.ContainsKey(name))
			{
				sp.SetRealValue(mMaps[name]);
				return;
			}

			if(mCubes.ContainsKey(name))
			{
				sp.SetRealValue(mCubes[name]);
				return;
			}
		}


		internal void UpdateTexPointers(Dictionary<string, Texture2D> maps,
			Dictionary<string, TextureCube> cubes)
		{
			mMaps	=maps;
			mCubes	=cubes;
		}
	}
}
