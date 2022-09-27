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
public class Mesh
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
		
		UInt16	[]indArray	=FileUtil.ReadArray<UInt16>(br);

		mVerts	=VertexTypes.BuildABuffer(gd, vertArray, mTypeIndex);
		mIndexs	=VertexTypes.BuildAnIndexBuffer(gd, indArray);

		mVerts.DebugName	=mName;

		if(bEditor)
		{
			mEditorMesh.SetData(mTypeIndex, vertArray, indArray);
		}

		br.Close();
		file.Close();
	}


	internal EditorMesh	GetEditorMesh()
	{
		return	mEditorMesh;
	}


	internal void DrawDMN(MatLib mlib, Matrix4x4 transform, MeshMaterial mm)
	{
		if(!mm.mbVisible)
		{
			return;
		}

		if(mlib == null)
		{
			return;
		}

		if(!mlib.MaterialExists("DMN"))
		{
			return;
		}

		string	vs	=mlib.GetMaterialVShader("DMN");
		if(vs == null)
		{
			return;
		}

		ID3D11DeviceContext	dc	=mlib.GetDC();

		dc.IASetVertexBuffer(0, mVerts, mVertSize);
		dc.IASetIndexBuffer(mIndexs, Format.R16_UInt, 0);

		mlib.SetMaterialShadersAndLayout("DMN");

		mlib.SetMaterialID("DMN", mm.mMaterialID);

		CBKeeper	cbk	=mlib.GetCBKeeper();

		//TODO: this used to have a sort of offset transform
//		cbk.SetWorldMat(transform * mPart);

		//now there is only this passed in transform
		cbk.SetWorldMat(transform);

		mlib.ApplyMaterial("DMN", dc);

		dc.DrawIndexed(mNumTriangles * 3, 0, 0);
	}


	internal void Draw(MatLib mlib, Matrix4x4 transform,
						MeshMaterial mm, string altMaterial)
	{
		if(!mm.mbVisible)
		{
			return;
		}

		if(mlib == null)
		{
			return;
		}

		if(!mlib.MaterialExists(altMaterial))
		{
			return;
		}

		string	vs	=mlib.GetMaterialVShader(altMaterial);
		if(vs == null)
		{
			return;
		}

		ID3D11DeviceContext	dc	=mlib.GetDC();

		dc.IASetVertexBuffer(0, mVerts, mVertSize);
		dc.IASetIndexBuffer(mIndexs, Format.R16_UInt, 0);

		mlib.SetMaterialShadersAndLayout(altMaterial);

		mlib.SetMaterialID(altMaterial, mm.mMaterialID);

		CBKeeper	cbk	=mlib.GetCBKeeper();

		//TODO: this used to have a sort of offset transform
//		cbk.SetWorldMat(transform * mPart);

		//now there is only this passed in transform
		cbk.SetWorldMat(transform);

		mlib.ApplyMaterial(altMaterial, dc);

		dc.DrawIndexed(mNumTriangles * 3, 0, 0);
	}


	//render X times
	internal void DrawX(MatLib mlib, Matrix4x4 transform,
		MeshMaterial mm, int numInst, string altMaterial)
	{
		if(!mm.mbVisible)
		{
			return;
		}

		if(mlib == null)
		{
			return;
		}

		if(!mlib.MaterialExists(altMaterial))
		{
			return;
		}

		string	vs	=mlib.GetMaterialVShader(altMaterial);
		if(vs == null)
		{
			return;
		}

		ID3D11DeviceContext	dc	=mlib.GetDC();

		dc.IASetVertexBuffer(0, mVerts, mVertSize);
		dc.IASetIndexBuffer(mIndexs, Format.R16_UInt, 0);

		mlib.SetMaterialShadersAndLayout(altMaterial);

		mlib.SetMaterialID(altMaterial, mm.mMaterialID);

		CBKeeper	cbk	=mlib.GetCBKeeper();

		//TODO: this used to have a sort of offset transform
//		cbk.SetWorldMat(transform * mPart);

		//now there is only this passed in transform
		cbk.SetWorldMat(transform);
		mlib.ApplyMaterial(altMaterial, dc);

		dc.DrawIndexedInstanced(mNumTriangles * 3, numInst, 0, 0, 0);
	}


	internal void Draw(MatLib mlib, Matrix4x4 transform, MeshMaterial mm)
	{
		if(!mm.mbVisible)
		{
			return;
		}

		if(mlib == null)
		{
			return;
		}

		if(!mlib.MaterialExists(mm.mMaterialName))
		{
			return;
		}

		string	vs	=mlib.GetMaterialVShader(mm.mMaterialName);
		if(vs == null)
		{
			return;
		}

		ID3D11DeviceContext	dc	=mlib.GetDC();

		dc.IASetVertexBuffer(0, mVerts, mVertSize);
		dc.IASetIndexBuffer(mIndexs, Format.R16_UInt, 0);

		mlib.SetMaterialShadersAndLayout(mm.mMaterialName);

		mlib.SetMaterialID(mm.mMaterialName, mm.mMaterialID);

		CBKeeper	cbk	=mlib.GetCBKeeper();

		//TODO: this used to have a sort of offset transform
//		cbk.SetWorldMat(transform * mPart);

		//now there is only this passed in transform
		cbk.SetWorldMat(transform);
		mlib.ApplyMaterial(mm.mMaterialName, dc);

		dc.DrawIndexed(mNumTriangles * 3, 0, 0);
	}


	public static Dictionary<string, StaticMesh> LoadAllStaticMeshes(
		string dir,	ID3D11Device gd)
	{
		Dictionary<string, StaticMesh>	ret	=new Dictionary<string, StaticMesh>();

		if(Directory.Exists(dir))
		{
			DirectoryInfo	di	=new DirectoryInfo(dir + "/");

			//load all mesh
			Dictionary<string, Mesh>	meshes	=new Dictionary<string, Mesh>();

			FileInfo[]		fi	=di.GetFiles("*.Mesh", SearchOption.TopDirectoryOnly);
			foreach(FileInfo f in fi)
			{
				//strip back
				string	path	=f.DirectoryName;

				Mesh	m	=new Mesh();
				m.Read(path + "\\" + f.Name, gd, true);

				meshes.Add(m.Name, m);
			}

			//load statics
			fi	=di.GetFiles("*.Static", SearchOption.TopDirectoryOnly);
			foreach(FileInfo f in fi)
			{
				//strip back
				string	path	=f.DirectoryName;

				StaticMesh	sm	=new StaticMesh(path + "\\" + f.Name, meshes);

				ret.Add(f.Name, sm);
			}
		}

		return	ret;
	}
}