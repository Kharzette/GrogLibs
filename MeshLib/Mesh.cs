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

public class Mesh
{
	public class MeshAndArch
	{
		public object	mMesh;
		public IArch	mArch;
	};

	protected string			mName;
	protected ID3D11Buffer		mVerts;
	protected ID3D11Buffer		mIndexs;
	protected int				mNumVerts, mNumTriangles, mVertSize;
	protected int				mTypeIndex;
	protected BoundingBox		mBoxBound;
	protected BoundingSphere	mSphereBound;

	//this is a sort of submesh transform
	//like an offset to a piece from the origin
	protected Matrix4x4			mPart;

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


	public Matrix4x4 GetTransform()
	{
		return	mPart;
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


	public void SetTransform(Matrix4x4 mat)
	{
		mPart		=mat;
	}


	public virtual void Write(BinaryWriter bw)
	{
		bw.Write(mName);
		bw.Write(mNumVerts);
		bw.Write(mNumTriangles);
		bw.Write(mVertSize);
		bw.Write(mTypeIndex);

		//transform
		FileUtil.WriteMatrix(bw, mPart);

		//box bound
		FileUtil.WriteVector3(bw, mBoxBound.Min);
		FileUtil.WriteVector3(bw, mBoxBound.Max);

		//sphere bound
		FileUtil.WriteVector3(bw, mSphereBound.Center);
		bw.Write(mSphereBound.Radius);
	}


	public virtual void Read(BinaryReader br, ID3D11Device gd, bool bEditor)
	{
		mName			=br.ReadString();
		mNumVerts		=br.ReadInt32();
		mNumTriangles	=br.ReadInt32();
		mVertSize		=br.ReadInt32();
		mTypeIndex		=br.ReadInt32();

		mPart	=FileUtil.ReadMatrix(br);

		SetTransform(mPart);

		mBoxBound.Min	=FileUtil.ReadVector3(br);
		mBoxBound.Max	=FileUtil.ReadVector3(br);

		mSphereBound.Center	=FileUtil.ReadVector3(br);
		mSphereBound.Radius	=br.ReadSingle();

		if(!bEditor)
		{
			Array	vertArray;

			VertexTypes.ReadVerts(br, gd, out vertArray);

			int	indLen	=br.ReadInt32();

			UInt16	[]indArray	=new UInt16[indLen];

			for(int i=0;i < indLen;i++)
			{
				indArray[i]	=br.ReadUInt16();
			}

			mVerts	=VertexTypes.BuildABuffer(gd, vertArray, mTypeIndex);
			mIndexs	=VertexTypes.BuildAnIndexBuffer(gd, indArray);

			mVerts.DebugName	=mName;
		}
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

		cbk.SetWorldMat(transform * mPart);		
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

		cbk.SetWorldMat(transform * mPart);		
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

		cbk.SetWorldMat(transform * mPart);
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

		cbk.SetWorldMat(transform * mPart);
		mlib.ApplyMaterial(mm.mMaterialName, dc);

		dc.DrawIndexed(mNumTriangles * 3, 0, 0);
	}


	public virtual void Bound()
	{
		throw new NotImplementedException();
	}


	public BoundingBox GetBoxBounds()
	{
		return	mBoxBound;
	}


	public BoundingSphere GetSphereBounds()
	{
		return	mSphereBound;
	}


	public static Dictionary<string, IArch> LoadAllStaticMeshes(
		string dir,	ID3D11Device gd)
	{
		Dictionary<string, IArch>	ret	=new Dictionary<string, IArch>();

		if(Directory.Exists(dir))
		{
			DirectoryInfo	di	=new DirectoryInfo(dir + "/");

			FileInfo[]		fi	=di.GetFiles("*.Static", SearchOption.TopDirectoryOnly);
			foreach(FileInfo f in fi)
			{
				//strip back
				string	path	=f.DirectoryName;

				IArch	smo	=new StaticArch();
				bool	bWorked	=smo.ReadFromFile(path + "\\" + f.Name, gd, true);

				if(bWorked)
				{
					ret.Add(f.Name, smo);
				}
			}
		}

		return	ret;
	}
}