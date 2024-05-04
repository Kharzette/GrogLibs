using System.IO;
using System.Numerics;
using UtilityLib;


namespace MaterialLib;

//Material stuff specific to BSP meshes
public partial class BSPMat
{
	bool	mbTextureEnabled;
	Vector2	mTexSize;

	string	mTexture;


	internal void Load(BinaryReader br)
	{
		mbTextureEnabled	=br.ReadBoolean();
		mTexSize			=FileUtil.ReadVector2(br);
		mTexture			=br.ReadString();
	}


	internal void Save(BinaryWriter bw)
	{
		bw.Write(mbTextureEnabled);
		FileUtil.WriteVector2(bw, mTexSize);

		//don't bother with the animated light array

		bw.Write(mTexture);
	}


	internal BSPMat()
	{
		mbTextureEnabled	=false;
		mTexSize			=Vector2.One * 64f;
		mTexture			="";		
	}

	internal BSPMat Clone()
	{
		BSPMat	ret	=new BSPMat();

		ret.mbTextureEnabled	=mbTextureEnabled;
		ret.mTexSize			=mTexSize;

		return	ret;
	}

	public bool	TextureEnabled
	{
		get	{	return	mbTextureEnabled;	}
		set	{	mbTextureEnabled	=value;	}
	}

	public Vector2	TextureSize
	{
		get	{	return	mTexSize;	}
		set	{	mTexSize	=value;	}
	}

	public string	Texture
	{
		get	{	return	mTexture;	}
		set	{	mTexture	=value;	}
	}
}