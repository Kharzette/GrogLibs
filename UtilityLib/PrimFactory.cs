using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;
using Vortice.Direct3D11;
using Vortice.DXGI;


namespace UtilityLib;

public class PrimObject
{
	ID3D11Buffer		mVB;
	ID3D11Buffer		mIB;
	ID3D11InputLayout	mLayout;

	int		mNumIndexes, mVertStride;
	bool	mb32BitIndexes;

	Matrix4x4	mWorld;


	public PrimObject(
		ID3D11Buffer vb, ID3D11Buffer ib, ID3D11InputLayout inp,
		int	vertStride, int numIndexes, bool b32)
	{
		mVB				=vb;
		mIB				=ib;
		mLayout			=inp;
		mVertStride		=vertStride;
		mNumIndexes		=numIndexes;
		mb32BitIndexes	=b32;

		mWorld	=Matrix4x4.Identity;
	}


	public void Free()
	{
		mIB.Dispose();
		mVB.Dispose();
		mLayout.Dispose();
	}


	public Matrix4x4 World
	{
		get { return mWorld; }
		set { mWorld = value; }
	}


	//custom shader
	public void Draw(ID3D11DeviceContext dc)
	{
		if(dc == null)
		{
			return;
		}

		dc.IASetVertexBuffer(0, mVB, mVertStride);

		if(mb32BitIndexes)
		{
			dc.IASetIndexBuffer(mIB, Format.R32_UInt, 0);
		}
		else
		{
			dc.IASetIndexBuffer(mIB, Format.R16_UInt, 0);
		}

		dc.IASetInputLayout(mLayout);

		dc.DrawIndexed(mNumIndexes, 0, 0);
	}
}


public static class PrimFactory
{
	//layoutkind sequential pack stuff is busted, go manual
	[StructLayout(LayoutKind.Explicit, Pack = 0)]
	internal struct VertexPositionNormalTexture
	{
		[FieldOffset(0)]
		internal Vector3	Position;
		[FieldOffset(12)]
		internal Half4		Normal;
		[FieldOffset(20)]
		internal Half2		TextureCoordinate;

		internal static ID3D11InputLayout MakeLayout(ID3D11Device gd, byte []vShader)
		{
			InputElementDescription	[]ied	=new[]
			{
				new	InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
				new InputElementDescription("NORMAL", 0, Format.R16G16B16A16_Float, 12, 0),
				new InputElementDescription("TEXCOORD", 0, Format.R16G16_Float, 20, 0)
			};
			
			return	gd.CreateInputLayout(ied, vShader);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	internal struct VertexPositionColor
	{
		internal Vector3	Position;
		internal Color		Color;

		internal static ID3D11InputLayout MakeLayout(ID3D11Device gd, byte []vShader)
		{
			InputElementDescription	[]ied	=new[]
			{
				new	InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
				new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UInt, 12, 0)
			};
			
			return	gd.CreateInputLayout(ied, vShader);
		}
	}


	public static PrimObject CreatePlane(ID3D11Device gd,
		byte []fxBytes, float size)
	{
		Vector3	top			=Vector3.UnitY * (size * 0.5f);
		Vector3	bottom		=-Vector3.UnitY * (size * 0.5f);
		Vector3	left		=-Vector3.UnitX * (size * 0.5f);
		Vector3	right		=Vector3.UnitX * (size * 0.5f);

		Vector2	topTex		=Vector2.UnitY;
		Vector2	bottomTex	=Vector2.Zero;
		Vector2	leftTex		=Vector2.Zero;
		Vector2	rightTex	=Vector2.UnitX;

		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[6];

		vpnt[0].Normal		=Vector4.UnitZ;
		vpnt[1].Normal		=Vector4.UnitZ;
		vpnt[2].Normal		=Vector4.UnitZ;

		vpnt[3].Normal		=Vector4.UnitZ;
		vpnt[4].Normal		=Vector4.UnitZ;
		vpnt[5].Normal		=Vector4.UnitZ;

		//need to have a lot of duplicates since each
		//vertex will contain a copy of the face normal
		//as we want this to be flat shaded

		//top upper left face
		vpnt[0].Position	=top + left;
		vpnt[1].Position	=top + right;
		vpnt[2].Position	=bottom + left;

		//top upper right face
		vpnt[3].Position	=top + right;
		vpnt[4].Position	=bottom + right;
		vpnt[5].Position	=bottom + left;

		//texture coordinates
		vpnt[0].TextureCoordinate	=topTex + leftTex;
		vpnt[1].TextureCoordinate	=topTex + rightTex;
		vpnt[2].TextureCoordinate	=bottomTex + leftTex;
		vpnt[3].TextureCoordinate	=topTex + rightTex;
		vpnt[4].TextureCoordinate	=bottomTex + rightTex;
		vpnt[5].TextureCoordinate	=bottomTex + leftTex;

		BufferDescription	bd	=new BufferDescription(				
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);
		
		//indexes
		UInt16	[]indexes	=new UInt16[6];

		//just reference in order
		for(int i=0;i < 6;i++)
		{
			indexes[i]	=(UInt16)i;
		}

		BufferDescription	id	=new BufferDescription(indexes.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, indexes.Length, false);

		return	po;
	}


	public static PrimObject CreatePrism(ID3D11Device gd,
		byte []fxBytes, float size)
	{
		Vector3	topPoint	=Vector3.UnitY * size * 2.0f;
		Vector3	bottomPoint	=Vector3.Zero;
		Vector3	top			=Vector3.UnitY * size + Vector3.UnitZ;
		Vector3	bottom		=Vector3.UnitY * size - Vector3.UnitZ;
		Vector3	left		=Vector3.UnitY * size + Vector3.UnitX;
		Vector3	right		=Vector3.UnitY * size - Vector3.UnitX;

		Vector2	topPointTex		=Vector2.One * 0.5f;
		Vector2	bottomPointTex	=Vector2.One * 0.5f;
		Vector2	topTex			=Vector2.UnitX * 0.5f;
		Vector2	bottomTex		=Vector2.UnitX * 0.5f + Vector2.UnitY;
		Vector2	leftTex			=Vector2.UnitY * 0.5f;
		Vector2	rightTex		=Vector2.UnitY * 0.5f + Vector2.UnitX;

		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[24];

		//hacky guessy normals for the 8 directions
		Vector4	topUpperLeft	=Vector4.UnitY + Vector4.UnitX + Vector4.UnitZ;
		Vector4	topUpperRight	=Vector4.UnitY - Vector4.UnitX + Vector4.UnitZ;
		Vector4	topLowerLeft	=Vector4.UnitY + Vector4.UnitX - Vector4.UnitZ;
		Vector4	topLowerRight	=Vector4.UnitY - Vector4.UnitX - Vector4.UnitZ;
		Vector4	botUpperLeft	=-Vector4.UnitY + Vector4.UnitX + Vector4.UnitZ;
		Vector4	botUpperRight	=-Vector4.UnitY - Vector4.UnitX + Vector4.UnitZ;
		Vector4	botLowerLeft	=-Vector4.UnitY + Vector4.UnitX - Vector4.UnitZ;
		Vector4	botLowerRight	=-Vector4.UnitY - Vector4.UnitX - Vector4.UnitZ;

		vpnt[0].Normal		=topUpperLeft;
		vpnt[1].Normal		=topUpperLeft;
		vpnt[2].Normal		=topUpperLeft;

		vpnt[3].Normal		=topUpperRight;
		vpnt[4].Normal		=topUpperRight;
		vpnt[5].Normal		=topUpperRight;

		vpnt[6].Normal		=topLowerLeft;
		vpnt[7].Normal		=topLowerLeft;
		vpnt[8].Normal		=topLowerLeft;

		vpnt[9].Normal		=topLowerRight;
		vpnt[10].Normal		=topLowerRight;
		vpnt[11].Normal		=topLowerRight;

		vpnt[12].Normal		=botUpperLeft;
		vpnt[13].Normal		=botUpperLeft;
		vpnt[14].Normal		=botUpperLeft;

		vpnt[15].Normal		=botUpperRight;
		vpnt[16].Normal		=botUpperRight;
		vpnt[17].Normal		=botUpperRight;

		vpnt[18].Normal		=botLowerLeft;
		vpnt[19].Normal		=botLowerLeft;
		vpnt[20].Normal		=botLowerLeft;

		vpnt[21].Normal		=botLowerRight;
		vpnt[22].Normal		=botLowerRight;
		vpnt[23].Normal		=botLowerRight;

		//need to have a lot of duplicates since each
		//vertex will contain a copy of the face normal
		//as we want this to be flat shaded

		//top upper left face
		vpnt[0].Position	=topPoint;
		vpnt[1].Position	=left;
		vpnt[2].Position	=top;

		//top upper right face
		vpnt[3].Position	=topPoint;
		vpnt[4].Position	=top;
		vpnt[5].Position	=right;

		//top lower left face
		vpnt[6].Position	=topPoint;
		vpnt[7].Position	=bottom;
		vpnt[8].Position	=left;

		//top lower right face
		vpnt[9].Position	=topPoint;
		vpnt[10].Position	=right;
		vpnt[11].Position	=bottom;

		//bottom upper left face
		//note the order switch
		vpnt[12].Position	=bottomPoint;
		vpnt[14].Position	=left;
		vpnt[13].Position	=top;

		//bottom upper right face
		vpnt[15].Position	=bottomPoint;
		vpnt[17].Position	=top;
		vpnt[16].Position	=right;

		//bottom lower left face
		vpnt[18].Position	=bottomPoint;
		vpnt[20].Position	=bottom;
		vpnt[19].Position	=left;

		//bottom lower right face
		vpnt[21].Position	=bottomPoint;
		vpnt[23].Position	=right;
		vpnt[22].Position	=bottom;

		//texture coordinates
		vpnt[0].TextureCoordinate	=topPointTex;
		vpnt[1].TextureCoordinate	=leftTex;
		vpnt[2].TextureCoordinate	=topTex;
		vpnt[3].TextureCoordinate	=topPointTex;
		vpnt[4].TextureCoordinate	=topTex;
		vpnt[5].TextureCoordinate	=rightTex;
		vpnt[6].TextureCoordinate	=topPointTex;
		vpnt[7].TextureCoordinate	=bottomTex;
		vpnt[8].TextureCoordinate	=leftTex;
		vpnt[9].TextureCoordinate	=topPointTex;
		vpnt[10].TextureCoordinate	=rightTex;
		vpnt[11].TextureCoordinate	=bottomTex;
		vpnt[12].TextureCoordinate	=bottomPointTex;
		vpnt[13].TextureCoordinate	=leftTex;
		vpnt[14].TextureCoordinate	=topTex;
		vpnt[15].TextureCoordinate	=bottomPointTex;
		vpnt[16].TextureCoordinate	=topTex;
		vpnt[17].TextureCoordinate	=rightTex;
		vpnt[18].TextureCoordinate	=bottomPointTex;
		vpnt[19].TextureCoordinate	=bottomTex;
		vpnt[20].TextureCoordinate	=leftTex;
		vpnt[21].TextureCoordinate	=bottomPointTex;
		vpnt[22].TextureCoordinate	=rightTex;
		vpnt[23].TextureCoordinate	=bottomTex;
		
		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		//indexes
		UInt16	[]indexes	=new UInt16[24];

		//just reference in order
		for(int i=0;i < 24;i++)
		{
			indexes[i]	=(UInt16)i;
		}

		BufferDescription	id	=new BufferDescription(indexes.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, indexes.Length, false);

		return	po;
	}


	public static PrimObject CreateHalfPrism(ID3D11Device gd,
		byte []fxBytes, float size)
	{
		Vector3	topPoint	=Vector3.Zero;
		Vector3	top			=(Vector3.UnitY * size * 5.0f) + size * Vector3.UnitZ;
		Vector3	bottom		=(Vector3.UnitY * size * 5.0f) + size * -Vector3.UnitZ;
		Vector3	left		=(Vector3.UnitY * size * 5.0f) + size * Vector3.UnitX;
		Vector3	right		=(Vector3.UnitY * size * 5.0f) + size * -Vector3.UnitX;

		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[12];

		//hacky guessy normals for the 8 directions
		Vector4	topUpperLeft	=Vector4.UnitY + Vector4.UnitX + Vector4.UnitZ;
		Vector4	topUpperRight	=Vector4.UnitY - Vector4.UnitX + Vector4.UnitZ;
		Vector4	topLowerLeft	=Vector4.UnitY + Vector4.UnitX - Vector4.UnitZ;
		Vector4	topLowerRight	=Vector4.UnitY - Vector4.UnitX - Vector4.UnitZ;

		vpnt[0].Normal		=topUpperLeft;
		vpnt[1].Normal		=topUpperLeft;
		vpnt[2].Normal		=topUpperLeft;

		vpnt[3].Normal		=topUpperRight;
		vpnt[4].Normal		=topUpperRight;
		vpnt[5].Normal		=topUpperRight;

		vpnt[6].Normal		=topLowerLeft;
		vpnt[7].Normal		=topLowerLeft;
		vpnt[8].Normal		=topLowerLeft;

		vpnt[9].Normal		=topLowerRight;
		vpnt[10].Normal		=topLowerRight;
		vpnt[11].Normal		=topLowerRight;

		//need to have a lot of duplicates since each
		//vertex will contain a copy of the face normal
		//as we want this to be flat shaded

		//top upper left face
		vpnt[2].Position	=topPoint;
		vpnt[1].Position	=left;
		vpnt[0].Position	=top;

		//top upper right face
		vpnt[5].Position	=topPoint;
		vpnt[4].Position	=top;
		vpnt[3].Position	=right;

		//top lower left face
		vpnt[8].Position	=topPoint;
		vpnt[7].Position	=bottom;
		vpnt[6].Position	=left;

		//top lower right face
		vpnt[11].Position	=topPoint;
		vpnt[10].Position	=right;
		vpnt[9].Position	=bottom;

		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		//indexes
		UInt16	[]indexes	=new UInt16[12];

		//just reference in order
		for(int i=0;i < 12;i++)
		{
			indexes[i]	=(UInt16)i;
		}

		BufferDescription	id	=new BufferDescription(indexes.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, indexes.Length, false);

		return	po;
	}


	public static PrimObject CreateCube(ID3D11Device gd,
		byte []fxBytes, float size)
	{
		List<Vector3>	corners	=new List<Vector3>();

		//cube corners
		corners.Add(-Vector3.UnitY * size + Vector3.UnitX * size + Vector3.UnitZ * size);
		corners.Add(-Vector3.UnitY * size - Vector3.UnitX * size + Vector3.UnitZ * size);
		corners.Add(-Vector3.UnitY * size + Vector3.UnitX * size - Vector3.UnitZ * size);
		corners.Add(-Vector3.UnitY * size - Vector3.UnitX * size - Vector3.UnitZ * size);
		corners.Add(Vector3.UnitY * size + Vector3.UnitX * size + Vector3.UnitZ * size);
		corners.Add(Vector3.UnitY * size - Vector3.UnitX * size + Vector3.UnitZ * size);
		corners.Add(Vector3.UnitY * size + Vector3.UnitX * size - Vector3.UnitZ * size);
		corners.Add(Vector3.UnitY * size - Vector3.UnitX * size - Vector3.UnitZ * size);

		return	CreateCube(gd, fxBytes, corners.ToArray());
	}


	public static PrimObject CreateCube(ID3D11Device gd,
		byte []fxBytes, BoundingBox box)
	{
		List<Vector3>	corners	=new List<Vector3>();

		//cube corners
		corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Max.Z);
		corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Max.Z);
		corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Min.Z);
		corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Min.Z);
		corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Max.Z);
		corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Max.Z);
		corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Min.Z);
		corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Min.Z);

		return	CreateCube(gd, fxBytes, corners.ToArray());
	}


	public static PrimObject CreateCubes(ID3D11Device gd,
		byte []fxBytes, List<BoundingBox> boxes)
	{
		List<Vector3>	corners	=new List<Vector3>();

		//cube corners
		foreach(BoundingBox box in boxes)
		{
			corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Max.Z);
			corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Max.Z);
			corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Min.Z);
			corners.Add(Vector3.UnitY * box.Min.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Min.Z);
			corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Max.Z);
			corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Max.Z);
			corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Max.X + Vector3.UnitZ * box.Min.Z);
			corners.Add(Vector3.UnitY * box.Max.Y + Vector3.UnitX * box.Min.X + Vector3.UnitZ * box.Min.Z);
		}
		return	CreateCubes(gd, fxBytes, corners.ToArray());
	}


	public unsafe static PrimObject CreateCube(ID3D11Device gd,
		byte []fxBytes, Vector3 []corners)
	{
		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[24];

		//cube corners
		Vector3	lowerTopRight	=corners[0];
		Vector3	lowerTopLeft	=corners[1];
		Vector3	lowerBotRight	=corners[2];
		Vector3	lowerBotLeft	=corners[3];
		Vector3	upperTopRight	=corners[4];
		Vector3	upperTopLeft	=corners[5];
		Vector3	upperBotRight	=corners[6];
		Vector3	upperBotLeft	=corners[7];

		//cube sides
		//top
		vpnt[0].Position	=upperTopLeft;
		vpnt[1].Position	=upperTopRight;
		vpnt[2].Position	=upperBotRight;
		vpnt[3].Position	=upperBotLeft;

		//bottom (note reversal)
		vpnt[7].Position	=lowerTopLeft;
		vpnt[6].Position	=lowerTopRight;
		vpnt[5].Position	=lowerBotRight;
		vpnt[4].Position	=lowerBotLeft;

		//top z side
		vpnt[11].Position	=upperTopLeft;
		vpnt[10].Position	=upperTopRight;
		vpnt[9].Position	=lowerTopRight;
		vpnt[8].Position	=lowerTopLeft;

		//bottom z side
		vpnt[12].Position	=upperBotLeft;
		vpnt[13].Position	=upperBotRight;
		vpnt[14].Position	=lowerBotRight;
		vpnt[15].Position	=lowerBotLeft;

		//-x side
		vpnt[16].Position	=upperTopLeft;
		vpnt[17].Position	=upperBotLeft;
		vpnt[18].Position	=lowerBotLeft;
		vpnt[19].Position	=lowerTopLeft;

		//x side
		vpnt[23].Position	=upperTopRight;
		vpnt[22].Position	=upperBotRight;
		vpnt[21].Position	=lowerBotRight;
		vpnt[20].Position	=lowerTopRight;

		//normals
		vpnt[0].Normal	=new Half4(Vector4.UnitY);
		vpnt[1].Normal	=Vector4.UnitY;
		vpnt[2].Normal	=Vector4.UnitY;
		vpnt[3].Normal	=Vector4.UnitY;

		vpnt[4].Normal	=-Vector4.UnitY;
		vpnt[5].Normal	=-Vector4.UnitY;
		vpnt[6].Normal	=-Vector4.UnitY;
		vpnt[7].Normal	=-Vector4.UnitY;

		vpnt[8].Normal	=Vector4.UnitZ;
		vpnt[9].Normal	=Vector4.UnitZ;
		vpnt[10].Normal	=Vector4.UnitZ;
		vpnt[11].Normal	=Vector4.UnitZ;

		vpnt[12].Normal	=-Vector4.UnitZ;
		vpnt[13].Normal	=-Vector4.UnitZ;
		vpnt[14].Normal	=-Vector4.UnitZ;
		vpnt[15].Normal	=-Vector4.UnitZ;

		vpnt[16].Normal	=-Vector4.UnitX;
		vpnt[17].Normal	=-Vector4.UnitX;
		vpnt[18].Normal	=-Vector4.UnitX;
		vpnt[19].Normal	=-Vector4.UnitX;

		vpnt[20].Normal	=Vector4.UnitX;
		vpnt[21].Normal	=Vector4.UnitX;
		vpnt[22].Normal	=Vector4.UnitX;
		vpnt[23].Normal	=Vector4.UnitX;

		//texcoords
		for(int i=0;i < 24;i+=4)
		{
			vpnt[i].TextureCoordinate		=Vector2.Zero;
			vpnt[i + 1].TextureCoordinate	=Vector2.UnitX;
			vpnt[i + 2].TextureCoordinate	=Vector2.UnitX + Vector2.UnitY;
			vpnt[i + 3].TextureCoordinate	=Vector2.UnitY;
		}

		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 24);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		UInt16	[]indexes	=new UInt16[36];

		int	idx	=0;
		for(int i=0;i < 36;i+=6)
		{
			indexes[i]		=(UInt16)(idx + 2);
			indexes[i + 1]	=(UInt16)(idx + 1);
			indexes[i + 2]	=(UInt16)(idx + 0);
			indexes[i + 3]	=(UInt16)(idx + 3);
			indexes[i + 4]	=(UInt16)(idx + 2);
			indexes[i + 5]	=(UInt16)(idx + 0);

			idx	+=4;
		}

		BufferDescription	id	=new BufferDescription(indexes.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, indexes.Length, false);

		return	po;
	}


	public static PrimObject CreateCubes(ID3D11Device gd,
		byte []fxBytes, List<Vector3> boxCenters, float size)
	{
		List<Vector3>	corners	=new List<Vector3>();

		foreach(Vector3 center in boxCenters)
		{
			//cube corners
			corners.Add(center - Vector3.UnitY * size + Vector3.UnitX * size + Vector3.UnitZ * size);
			corners.Add(center - Vector3.UnitY * size - Vector3.UnitX * size + Vector3.UnitZ * size);
			corners.Add(center - Vector3.UnitY * size + Vector3.UnitX * size - Vector3.UnitZ * size);
			corners.Add(center - Vector3.UnitY * size - Vector3.UnitX * size - Vector3.UnitZ * size);
			corners.Add(center + Vector3.UnitY * size + Vector3.UnitX * size + Vector3.UnitZ * size);
			corners.Add(center + Vector3.UnitY * size - Vector3.UnitX * size + Vector3.UnitZ * size);
			corners.Add(center + Vector3.UnitY * size + Vector3.UnitX * size - Vector3.UnitZ * size);
			corners.Add(center + Vector3.UnitY * size - Vector3.UnitX * size - Vector3.UnitZ * size);
		}

		return	CreateCubes(gd, fxBytes, corners.ToArray());
	}


	public static PrimObject CreateCubes(ID3D11Device gd,
		byte []fxBytes, Vector3 []corners)
	{
		VertexPositionNormalTexture	[]vpnt	=
			new VertexPositionNormalTexture[corners.Length * 3];

		for(int i=0;i < (corners.Length / 8);i++)
		{
			int	idx		=i * 8;
			int	vidx	=i * 24;

			//cube corners
			Vector3	lowerTopRight	=corners[0 + idx];
			Vector3	lowerTopLeft	=corners[1 + idx];
			Vector3	lowerBotRight	=corners[2 + idx];
			Vector3	lowerBotLeft	=corners[3 + idx];
			Vector3	upperTopRight	=corners[4 + idx];
			Vector3	upperTopLeft	=corners[5 + idx];
			Vector3	upperBotRight	=corners[6 + idx];
			Vector3	upperBotLeft	=corners[7 + idx];

			//cube sides
			//top
			vpnt[0 + vidx].Position	=upperTopLeft;
			vpnt[1 + vidx].Position	=upperTopRight;
			vpnt[2 + vidx].Position	=upperBotRight;
			vpnt[3 + vidx].Position	=upperBotLeft;

			//bottom (note reversal)
			vpnt[7 + vidx].Position	=lowerTopLeft;
			vpnt[6 + vidx].Position	=lowerTopRight;
			vpnt[5 + vidx].Position	=lowerBotRight;
			vpnt[4 + vidx].Position	=lowerBotLeft;

			//top z side
			vpnt[11 + vidx].Position	=upperTopLeft;
			vpnt[10 + vidx].Position	=upperTopRight;
			vpnt[9 + vidx].Position		=lowerTopRight;
			vpnt[8 + vidx].Position		=lowerTopLeft;

			//bottom z side
			vpnt[12 + vidx].Position	=upperBotLeft;
			vpnt[13 + vidx].Position	=upperBotRight;
			vpnt[14 + vidx].Position	=lowerBotRight;
			vpnt[15 + vidx].Position	=lowerBotLeft;

			//-x side
			vpnt[16 + vidx].Position	=upperTopLeft;
			vpnt[17 + vidx].Position	=upperBotLeft;
			vpnt[18 + vidx].Position	=lowerBotLeft;
			vpnt[19 + vidx].Position	=lowerTopLeft;

			//x side
			vpnt[23 + vidx].Position	=upperTopRight;
			vpnt[22 + vidx].Position	=upperBotRight;
			vpnt[21 + vidx].Position	=lowerBotRight;
			vpnt[20 + vidx].Position	=lowerTopRight;

			//normals
			vpnt[0 + vidx].Normal	=Vector4.UnitY;
			vpnt[1 + vidx].Normal	=Vector4.UnitY;
			vpnt[2 + vidx].Normal	=Vector4.UnitY;
			vpnt[3 + vidx].Normal	=Vector4.UnitY;

			vpnt[4 + vidx].Normal	=-Vector4.UnitY;
			vpnt[5 + vidx].Normal	=-Vector4.UnitY;
			vpnt[6 + vidx].Normal	=-Vector4.UnitY;
			vpnt[7 + vidx].Normal	=-Vector4.UnitY;

			vpnt[8 + vidx].Normal	=Vector4.UnitZ;
			vpnt[9 + vidx].Normal	=Vector4.UnitZ;
			vpnt[10 + vidx].Normal	=Vector4.UnitZ;
			vpnt[11 + vidx].Normal	=Vector4.UnitZ;

			vpnt[12 + vidx].Normal	=-Vector4.UnitZ;
			vpnt[13 + vidx].Normal	=-Vector4.UnitZ;
			vpnt[14 + vidx].Normal	=-Vector4.UnitZ;
			vpnt[15 + vidx].Normal	=-Vector4.UnitZ;

			vpnt[16 + vidx].Normal	=-Vector4.UnitX;
			vpnt[17 + vidx].Normal	=-Vector4.UnitX;
			vpnt[18 + vidx].Normal	=-Vector4.UnitX;
			vpnt[19 + vidx].Normal	=-Vector4.UnitX;

			vpnt[20 + vidx].Normal	=Vector4.UnitX;
			vpnt[21 + vidx].Normal	=Vector4.UnitX;
			vpnt[22 + vidx].Normal	=Vector4.UnitX;
			vpnt[23 + vidx].Normal	=Vector4.UnitX;

			//texcoords
			for(int j=0;j < 24;j+=4)
			{
				vpnt[vidx + j].TextureCoordinate		=Vector2.Zero;
				vpnt[vidx + j + 1].TextureCoordinate	=Vector2.UnitX;
				vpnt[vidx + j + 2].TextureCoordinate	=Vector2.UnitX + Vector2.UnitY;
				vpnt[vidx + j + 3].TextureCoordinate	=Vector2.UnitY;
			}
		}
		
		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		UInt32	[]indexes	=new UInt32[36 * (corners.Length / 8)];

		int	idxx	=0;
		for(int i=0;i < (36 * (corners.Length / 8));i+=6)
		{
			indexes[i]		=(UInt32)(idxx + 2);
			indexes[i + 1]	=(UInt32)(idxx + 1);
			indexes[i + 2]	=(UInt32)(idxx + 0);
			indexes[i + 3]	=(UInt32)(idxx + 3);
			indexes[i + 4]	=(UInt32)(idxx + 2);
			indexes[i + 5]	=(UInt32)(idxx + 0);

			idxx	+=4;
		}

		BufferDescription	id	=new BufferDescription(indexes.Length * 4,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt32>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, indexes.Length, false);

		return	po;
	}


	public static PrimObject CreateSphere(ID3D11Device gd,
		byte []fxBytes, Vector3 center, float radius)
	{
		int	theta, phi;

		//density
		int	dtheta	=10;
		int	dphi	=10;
		
		List<Vector3>	points	=new List<Vector3>();
		List<UInt16>	inds	=new List<UInt16>();

		//build and index a hemisphere
		UInt16	curIdx	=0;
		for(theta=-90;theta <= 0-dtheta;theta += dtheta)
		{
			for(phi=0;phi <= 360-dphi;phi += dphi)
			{
				Vector3	pos	=Vector3.Zero;

				float	rtheta	=MathHelper.ToRadians(theta);
				float	rdtheta	=MathHelper.ToRadians(dtheta);
				float	rphi	=MathHelper.ToRadians(phi);
				float	rdphi	=MathHelper.ToRadians(dphi);

				pos.X	=(float)(Math.Cos(rtheta) * Math.Cos(rphi));
				pos.Y	=(float)(Math.Cos(rtheta) * Math.Sin(rphi));
				pos.Z	=(float)Math.Sin(rtheta);

				points.Add(pos);
				
				pos.X	=(float)(Math.Cos((rtheta + rdtheta)) * Math.Cos(rphi));
				pos.Y	=(float)(Math.Cos((rtheta + rdtheta)) * Math.Sin(rphi));
				pos.Z	=(float)Math.Sin((rtheta + rdtheta));

				points.Add(pos);

				pos.X	=(float)(Math.Cos((rtheta + rdtheta)) * Math.Cos((rphi + rdphi)));
				pos.Y	=(float)(Math.Cos((rtheta + rdtheta)) * Math.Sin((rphi + rdphi)));
				pos.Z	=(float)Math.Sin((rtheta + rdtheta));

				points.Add(pos);

				if(theta > -90 && theta < 0)
				{
					pos.X	=(float)(Math.Cos(rtheta) * Math.Cos((rphi + rdphi)));
					pos.Y	=(float)(Math.Cos(rtheta) * Math.Sin((rphi + rdphi)));
					pos.Z	=(float)Math.Sin(rtheta);

					points.Add(pos);

					inds.Add(curIdx);
					inds.Add((UInt16)(curIdx + 1));
					inds.Add((UInt16)(curIdx + 2));
					inds.Add((UInt16)(curIdx + 0));
					inds.Add((UInt16)(curIdx + 2));
					inds.Add((UInt16)(curIdx + 3));

					curIdx	+=4;
				}
				else
				{
					inds.Add(curIdx);
					inds.Add((UInt16)(curIdx + 1));
					inds.Add((UInt16)(curIdx + 2));
					curIdx	+=3;
				}
			}
		}

		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[points.Count * 2];

		//copy in hemisphere
		for(int i=0;i < points.Count;i++)
		{
			Vector3	norm	=Vector3.Normalize(points[i]);

			vpnt[i].Normal				=new Half4(norm.X, norm.Y, norm.Z, 1f);
			vpnt[i].Position			=norm * radius + center;
			vpnt[i].TextureCoordinate	=Vector2.Zero;	//not tackling this yet
		}

		//dupe for other half
		int	ofs	=points.Count;
		for(int i=ofs;i < points.Count + ofs;i++)
		{
			Vector3	norm	=Vector3.Normalize(points[i - ofs]);

			//flip normal
			vpnt[i].Normal				=new Half4(-norm.X, -norm.Y, -norm.Z, 1f);
			vpnt[i].Position			=-norm * radius + center;
			vpnt[i].TextureCoordinate	=Vector2.Zero;	//not tackling this yet
		}

		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		//index the other half
		List<UInt16>	otherHalf	=new List<UInt16>();

		int	halfCount	=inds.Count;
		for(int i=0;i < halfCount;i++)
		{
			otherHalf.Add((UInt16)(points.Count + inds[i]));
		}

		//reverse order
		otherHalf.Reverse();

		inds.AddRange(otherHalf);

		//don't need this because of handedness change
		//inds.Reverse();

		BufferDescription	id	=new BufferDescription(inds.Count * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(inds.ToArray(), id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, inds.Count, false);

		return	po;
	}


	//should orient along the Y axis
	public static PrimObject CreateCapsule(ID3D11Device gd,
		byte []fxBytes, float radius, float len)
	{
		int	theta, phi;

		//density
		int	dtheta	=20;
		int	dphi	=20;
		
		List<Vector3>	points	=new List<Vector3>();
		List<UInt16>	inds	=new List<UInt16>();

		//build and index a hemisphere
		UInt16	curIdx	=0;
		for(theta=-90;theta <= 0-dtheta;theta += dtheta)
		{
			for(phi=0;phi <= 360-dphi;phi += dphi)
			{
				Vector3	pos	=Vector3.Zero;

				float	rtheta	=MathHelper.ToRadians(theta);
				float	rdtheta	=MathHelper.ToRadians(dtheta);
				float	rphi	=MathHelper.ToRadians(phi);
				float	rdphi	=MathHelper.ToRadians(dphi);

				if(curIdx == 0)
				{
					pos.X	=(float)(Math.Cos(rtheta) * Math.Cos(rphi));
					pos.Z	=(float)(Math.Cos(rtheta) * Math.Sin(rphi));
					pos.Y	=(float)Math.Sin(rtheta);

					points.Add(pos);
					curIdx++;
				}
				
				pos.X	=(float)(Math.Cos((rtheta + rdtheta)) * Math.Cos(rphi));
				pos.Z	=(float)(Math.Cos((rtheta + rdtheta)) * Math.Sin(rphi));
				pos.Y	=(float)Math.Sin((rtheta + rdtheta));

				points.Add(pos);
			}
		}

//		curIdx++;
		for(UInt16 i=1;i < 18;i++)
		{
			//base ring
			inds.Add(0);
			inds.Add(i);
			inds.Add((ushort)(i + 1));
		}

		//final tri
		inds.Add(0);
		inds.Add(18);
		inds.Add(1);	//wrap

		//next ring
		for(UInt16 i=19;i < 36;i++)
		{
			inds.Add((ushort)(i - 18));
			inds.Add((ushort)(i));
			inds.Add((ushort)(i + 1));

			inds.Add((ushort)(i - 18));
			inds.Add((ushort)(i + 1));
			inds.Add((ushort)(i - 17));
		}

		//finish quad for this ring
		inds.Add(18);
		inds.Add(36);
		inds.Add(19);

		inds.Add(18);
		inds.Add(19);
		inds.Add(1);	//wrap

		//next ring
		for(UInt16 i=37;i < 54;i++)
		{
			inds.Add((ushort)(i - 18));
			inds.Add((ushort)(i));
			inds.Add((ushort)(i + 1));

			inds.Add((ushort)(i - 18));
			inds.Add((ushort)(i + 1));
			inds.Add((ushort)(i - 17));
		}

		//finish quad
		inds.Add(36);
		inds.Add(54);
		inds.Add(37);

		inds.Add(36);
		inds.Add(37);
		inds.Add(19);	//wrap

		//next ring
		for(UInt16 i=55;i < 72;i++)
		{
			inds.Add((ushort)(i - 18));
			inds.Add((ushort)(i));
			inds.Add((ushort)(i + 1));

			inds.Add((ushort)(i - 18));
			inds.Add((ushort)(i + 1));
			inds.Add((ushort)(i - 17));
		}

		//finish quad
		inds.Add(54);
		inds.Add(72);
		inds.Add(55);

		inds.Add(54);
		inds.Add(55);
		inds.Add(37);	//wrap

		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[points.Count * 2];

		//copy in hemisphere
		for(int i=0;i < points.Count;i++)
		{
			Vector3	norm	=Vector3.Normalize(points[i]);

			vpnt[i].Normal				=new Half4(norm.X, norm.Y, norm.Z, 1f);
			vpnt[i].Position			=norm * radius;
			vpnt[i].TextureCoordinate	=Vector2.Zero;	//not tackling this yet
		}

		//dupe for other half
		int	ofs	=points.Count;
		for(int i=ofs;i < points.Count + ofs;i++)
		{
			Vector3	norm	=Vector3.Normalize(points[i - ofs]);

			//flip normal
			vpnt[i].Normal				=new Half4(-norm.X, -norm.Y, -norm.Z, 1f);
			vpnt[i].Position			=-norm * radius + (Vector3.UnitY * len);
			vpnt[i].TextureCoordinate	=Vector2.Zero;	//not tackling this yet
		}

		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		//index the other half
		List<UInt16>	otherHalf	=new List<UInt16>();

		int	halfCount	=inds.Count;
		for(int i=0;i < halfCount;i++)
		{
			otherHalf.Add((UInt16)(ofs + inds[i]));
		}

		//reverse order
		otherHalf.Reverse();

		inds.AddRange(otherHalf);

		//rings from renderdoc
		//55 to 72 for the lower index ring
		//128 to 145 for the higher index ring
		//9 is added because the verts are on opposite sides
		int	ringOfs	=128 - 55 + 9;

		//connect the 2 hemispheres
		//half ring
		for(int i=55;i < 63;i++)
		{
			inds.Add((UInt16)i);
			inds.Add((UInt16)(i + ringOfs));
			inds.Add((UInt16)(i + ringOfs + 1));

			inds.Add((UInt16)i);
			inds.Add((UInt16)(i + ringOfs + 1));
			inds.Add((UInt16)(i + 1));
		}

		inds.Add(63);
		inds.Add(145);
		inds.Add(128);	//wrap

		//correct back to opposite side
		ringOfs	=128 - 63;

		//other half
		for(int i=63;i < 72;i++)
		{
			inds.Add((UInt16)i);
			inds.Add((UInt16)(i + ringOfs));
			inds.Add((UInt16)(i + ringOfs + 1));

			inds.Add((UInt16)i);
			inds.Add((UInt16)(i + ringOfs + 1));
			inds.Add((UInt16)(i + 1));
		}

		inds.Add(72);
		inds.Add(137);
		inds.Add(55);	//wrap

		//reverse all
		inds.Reverse();

		BufferDescription	id	=new BufferDescription(inds.Count * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(inds.ToArray(), id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, inds.Count, false);

		return	po;
	}


	public static PrimObject CreateShadowCircle(ID3D11Device gd,
		byte []fxBytes, float radius)
	{
		VertexPositionColor	[]vpc	=new VertexPositionColor[16];

		//can't remember how to generate a circle
		//I know it's something with sin or something? Pi?
		//just going to use a matrix
		Matrix4x4	rotMat	=Matrix4x4.CreateRotationY(MathHelper.TwoPi / 8.0f);
		Vector3		rotPos	=Vector3.UnitX * radius * 0.25f;

		Color	halfDarken	=new Color(new Vector4(0.0f, 0.0f, 0.0f, 0.35f));
		Color	fadeOut		=new Color(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

		//inner ring
		for(int i=0;i < 8;i++)
		{
			vpc[i].Position	=rotPos;
			rotPos			=Mathery.TransformCoordinate(rotPos, ref rotMat);
			vpc[i].Color	=halfDarken;
		}

		//outer ring
		rotPos	=Vector3.UnitX * radius;
		for(int i=8;i < 16;i++)
		{
			vpc[i].Position	=rotPos;
			rotPos			=Mathery.TransformCoordinate(rotPos, ref rotMat);
			vpc[i].Color	=fadeOut;
		}

		BufferDescription	bd	=new BufferDescription(
			16 * vpc.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionColor>(vpc, bd);

		UInt16	[]indexes	=new UInt16[66];

		//inner ring
		indexes[0]	=0;
		indexes[1]	=2;
		indexes[2]	=1;
		indexes[3]	=0;
		indexes[4]	=3;
		indexes[5]	=2;
		indexes[6]	=0;
		indexes[7]	=4;
		indexes[8]	=3;
		indexes[9]	=0;
		indexes[10]	=5;
		indexes[11]	=4;
		indexes[12]	=0;
		indexes[13]	=6;
		indexes[14]	=5;
		indexes[15]	=0;
		indexes[16]	=7;
		indexes[17]	=6;

		indexes[18]	=0;
		indexes[19]	=9;
		indexes[20]	=8;
		indexes[21]	=0;
		indexes[22]	=1;
		indexes[23]	=9;

		indexes[24]	=2;
		indexes[25]	=9;
		indexes[26]	=1;
		indexes[27]	=2;
		indexes[28]	=10;
		indexes[29]	=9;

		indexes[30]	=3;
		indexes[31]	=10;
		indexes[32]	=2;
		indexes[33]	=3;
		indexes[34]	=11;
		indexes[35]	=10;

		indexes[36]	=4;
		indexes[37]	=11;
		indexes[38]	=3;
		indexes[39]	=4;
		indexes[40]	=12;
		indexes[41]	=11;

		indexes[42]	=5;
		indexes[43]	=12;
		indexes[44]	=4;
		indexes[45]	=5;
		indexes[46]	=13;
		indexes[47]	=12;

		indexes[48]	=6;
		indexes[49]	=13;
		indexes[50]	=5;
		indexes[51]	=6;
		indexes[52]	=14;
		indexes[53]	=13;

		indexes[54]	=7;
		indexes[55]	=14;
		indexes[56]	=6;
		indexes[57]	=7;
		indexes[58]	=15;
		indexes[59]	=14;

		indexes[60]	=0;
		indexes[61]	=8;
		indexes[62]	=15;
		indexes[63]	=0;
		indexes[64]	=15;
		indexes[65]	=7;

		BufferDescription	id	=new BufferDescription(indexes.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 16, indexes.Length, false);

		return	po;
	}


	public static PrimObject CreateCylinder(ID3D11Device gd,
		byte []fxBytes, float radius, float len)
	{
		VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[32];

		//can't remember how to generate a circle
		//I know it's something with sin or something? Pi?
		//just going to use a matrix
		Matrix4x4	rotMat	=Matrix4x4.CreateRotationY(MathHelper.TwoPi / 8.0f);
		Vector3		rotPos	=Vector3.UnitX * radius;

		//make top and bottom surfaces
		for(int i=0;i < 8;i++)
		{
			vpnt[i].Position		=rotPos;
			vpnt[i + 8].Position	=rotPos + Vector3.UnitY * len;

			vpnt[i].Normal		=-Vector4.UnitY;
			vpnt[i + 8].Normal	=Vector4.UnitY;

			rotPos	=Mathery.TransformCoordinate(rotPos, ref rotMat);

			Vector3	rotDir	=Vector3.Normalize(rotPos);

			rotDir	*=0.5f;

			vpnt[i].TextureCoordinate		=new Half2(rotDir.X, rotDir.Z);
			vpnt[i + 8].TextureCoordinate	=new Half2(rotDir.X, rotDir.Z);
		}

		//duplicate top and bottom and generate a side facing normal
		for(int i=0;i < 16;i++)
		{
			//use position about 0, 0 to generate side normal
			Vector3	norm	=vpnt[i].Position;

			//clear y
			norm.Y	=0f;

			norm	=Vector3.Normalize(norm);

			vpnt[i + 16]	=vpnt[i];
			vpnt[i + 16].Normal	=new Half4(norm.X, norm.Y, norm.Z, 1f);
		}

		BufferDescription	bd	=new BufferDescription(
			24 * vpnt.Length,
			BindFlags.VertexBuffer,	ResourceUsage.Immutable, 
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	vb	=gd.CreateBuffer<VertexPositionNormalTexture>(vpnt, bd);

		UInt16	[]indexes	=new UInt16[36 + 48];

		//top surface
		indexes[0]	=0;
		indexes[1]	=1;
		indexes[2]	=7;
		indexes[3]	=1;
		indexes[4]	=2;
		indexes[5]	=3;
		indexes[6]	=3;
		indexes[7]	=4;
		indexes[8]	=5;
		indexes[9]	=5;
		indexes[10]	=6;
		indexes[11]	=7;
		indexes[12]	=7;
		indexes[13]	=1;
		indexes[14]	=5;
		indexes[15]	=1;
		indexes[16]	=3;
		indexes[17]	=5;

		//bottom surface
		indexes[35]	=0 + 8;
		indexes[34]	=1 + 8;
		indexes[33]	=7 + 8;
		indexes[32]	=1 + 8;
		indexes[31]	=2 + 8;
		indexes[30]	=3 + 8;
		indexes[29]	=3 + 8;
		indexes[28]	=4 + 8;
		indexes[27]	=5 + 8;
		indexes[26]	=5 + 8;
		indexes[25]	=6 + 8;
		indexes[24]	=7 + 8;
		indexes[23]	=7 + 8;
		indexes[22]	=1 + 8;
		indexes[21]	=5 + 8;
		indexes[20]	=1 + 8;
		indexes[19]	=3 + 8;
		indexes[18]	=5 + 8;

		//connexions
		for(int i=0;i < 7;i++)
		{
			indexes[36 + (i * 6)]	=(UInt16)(i + 1 + 16);
			indexes[37 + (i * 6)]	=(UInt16)(i + 0 + 16);
			indexes[38 + (i * 6)]	=(UInt16)(i + 8 + 16);
			indexes[39 + (i * 6)]	=(UInt16)(i + 8 + 16);
			indexes[40 + (i * 6)]	=(UInt16)(i + 9 + 16);
			indexes[41 + (i * 6)]	=(UInt16)(i + 1 + 16);
		}

		//last 2 faces are goofy
		indexes[36 + 42]	=(UInt16)(0 + 16);
		indexes[37 + 42]	=(UInt16)(7 + 16);
		indexes[38 + 42]	=(UInt16)(7 + 8 + 16);
		indexes[39 + 42]	=(UInt16)(7 + 8 + 16);
		indexes[40 + 42]	=(UInt16)(8 + 16);
		indexes[41 + 42]	=(UInt16)(0 + 16);

		BufferDescription	id	=new BufferDescription(indexes.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		ID3D11Buffer	ib	=gd.CreateBuffer<UInt16>(indexes, id);

		ID3D11InputLayout	lay	=VertexPositionNormalTexture.MakeLayout(gd, fxBytes);

		PrimObject	po	=new PrimObject(vb, ib, lay, 24, indexes.Length, false);

		return	po;
	}
}