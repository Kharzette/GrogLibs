using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using UtilityLib;
using MaterialLib;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Vortice.Mathematics;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

//contains the gpu specific stuff for mesh rendering
//might be part of a static or a character
public partial class Mesh
{
	string			mName;
	ID3D11Buffer	mVerts;
	ID3D11Buffer	mIndexs;
	int				mNumVerts, mNumTriangles, mVertSize;
	int				mTypeIndex;

	//optional editor data, will be null usually in a game
	EditorMesh	mEditorMesh;

	//scale factors, collada always in meterish scale
	public const float	MetersToQuakeUnits	=37.6471f;
	public const float	MetersToValveUnits	=39.37001f;
	public const float	MetersToGrogUnits	=MetersToValveUnits;
	public const float	MetersToCentiMeters	=100f;


	public string Name
	{
		get { return mName; }
		set { mName = ((value == null)? "" : value); }
	}
	public Type VertexType
	{
		get { return VertexTypes.GetTypeForIndex(mTypeIndex); }
		private set { mTypeIndex = VertexTypes.GetIndex(value); }
	}
	

	public Mesh() { }
	public Mesh(string name)
	{
		Name	=name;
	}


	public void FreeAll()
	{
		mVerts.Dispose();
		mIndexs.Dispose();
	}


	public void SetVertSize(int size)
	{
		mVertSize	=size;
	}


	public void SetNumVerts(int nv)
	{
		mNumVerts	=nv;
	}


	public void SetNumTriangles(int numTri)
	{
		mNumTriangles	=numTri;
	}


	internal int GetNumTriangles()
	{
		return	mNumTriangles;
	}


	public void SetVertexBuffer(ID3D11Buffer vb)
	{
		mVerts		=vb;
	}


	public void SetIndexBuffer(ID3D11Buffer indbuf)
	{
		mIndexs	=indbuf;
	}


	public void SetTypeIndex(int idx)
	{
		mTypeIndex	=idx;
	}


	public void SetEditorData(Array verts, ushort []inds)
	{
		mEditorMesh	=new EditorMesh();

		mEditorMesh.SetData(mTypeIndex, verts, inds);
	}


	public void Write(string fileName)
	{
		if(mEditorMesh == null)
		{
			return;	//can only save from editors
		}
		
		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying character instances
		UInt32	magic	=0xB0135313;

		bw.Write(magic);

		bw.Write(mName);
		bw.Write(mNumVerts);
		bw.Write(mNumTriangles);
		bw.Write(mVertSize);
		bw.Write(mTypeIndex);

		mEditorMesh.Write(bw);

		bw.Write(mNumTriangles * 3);

		bw.Close();
		file.Close();
	}


	public void Read(string fileName, ID3D11Device gd, bool bEditor)
	{
		if(!File.Exists(fileName))
		{
			return;
		}

		Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
		if(file == null)
		{
			return;
		}
		BinaryReader	br	=new BinaryReader(file);

		UInt32	magic	=br.ReadUInt32();
		if(magic != 0xb0135313)
		{
			br.Close();
			file.Close();
			return;
		}

		mName			=br.ReadString();
		mNumVerts		=br.ReadInt32();
		mNumTriangles	=br.ReadInt32();
		mVertSize		=br.ReadInt32();
		mTypeIndex		=br.ReadInt32();

		Array	vertArray;

		VertexTypes.ReadVerts(br, gd, out vertArray);
		
		UInt16	[]indArray	=FileUtil.Read16Array(br);

		mVerts	=VertexTypes.BuildABuffer(gd, vertArray, mTypeIndex);
		mIndexs	=VertexTypes.BuildAnIndexBuffer(gd, indArray);

		mVerts.DebugName	=mName;

		if(bEditor)
		{
			mEditorMesh	=new EditorMesh();
			mEditorMesh.SetData(mTypeIndex, vertArray, indArray);
		}

		br.Close();
		file.Close();
	}


	internal EditorMesh	GetEditorMesh()
	{
		return	mEditorMesh;
	}
}