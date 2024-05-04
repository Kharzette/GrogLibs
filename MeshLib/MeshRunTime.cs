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


	public void DeleteVertElement(ID3D11Device gd, List<int> inds)
	{
		if(mEditorMesh != null)
		{
			mEditorMesh.NukeVertexElement(inds, gd);
		}
	}

	
	public static void LoadAllMeshes(string dir, ID3D11Device gd, Dictionary<string, Mesh> meshes)
	{
		if(meshes == null)
		{
			return;
		}

		if(Directory.Exists(dir))
		{
			DirectoryInfo	di	=new DirectoryInfo(dir + "/");

			//load all mesh
			FileInfo[]		fi	=di.GetFiles("*.Mesh", SearchOption.TopDirectoryOnly);
			foreach(FileInfo f in fi)
			{
				Mesh	m	=new Mesh();
				m.Read(f.FullName, gd, true);

				if(!meshes.ContainsKey(m.Name))
				{
					meshes.Add(m.Name, m);
				}
			}
		}
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
			LoadAllMeshes(dir, gd, meshes);

			//load statics
			FileInfo	[]fi	=di.GetFiles("*.Static", SearchOption.TopDirectoryOnly);
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