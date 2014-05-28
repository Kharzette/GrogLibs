using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MapFlags	=SharpDX.Direct3D11.MapFlags;


namespace UtilityLib
{
	public class PrimObject
	{
		Buffer				mVB;
		Buffer				mIB;
		VertexBufferBinding	mVBBinding;
		int					mNumIndexes;

		Matrix				mWorld;
		bool				mbHasNormals;


		public PrimObject(Buffer vb, int vertSize,
			Buffer ib, int numIndexes, bool bHasNormals)
		{
			mVB				=vb;
			mIB				=ib;
			mNumIndexes		=numIndexes;
			mbHasNormals	=bHasNormals;

			mVBBinding	=new VertexBufferBinding(mVB, vertSize, 0);

			mWorld	=Matrix.Identity;
		}


		public Matrix World
		{
			get { return mWorld; }
			set { mWorld = value; }
		}


		//custom shader
		public void Draw(DeviceContext dc)
		{
			if(dc == null)
			{
				return;
			}
			dc.InputAssembler.SetVertexBuffers(0, mVBBinding);
			dc.InputAssembler.SetIndexBuffer(mIB, Format.R16_UInt, 0);

			dc.DrawIndexed(mNumIndexes, 0, 0);
		}
	}


	public class PrimFactory
	{
		internal struct VertexPositionNormalTexture
		{
			internal Vector3	Position;
			internal Vector3	Normal;
			internal Vector2	TextureCoordinate;
		}

		internal struct VertexPositionColor
		{
			internal Vector3	Position;
			internal Color		Color;
		}


		public static PrimObject CreatePlane(Device gd, float size)
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

			vpnt[0].Normal		=Vector3.UnitZ;
			vpnt[1].Normal		=Vector3.UnitZ;
			vpnt[2].Normal		=Vector3.UnitZ;

			vpnt[3].Normal		=Vector3.UnitZ;
			vpnt[4].Normal		=Vector3.UnitZ;
			vpnt[5].Normal		=Vector3.UnitZ;

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
				32 * vpnt.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpnt, bd);
			
			//indexes
			UInt16	[]indexes	=new UInt16[6];

			//just reference in order
			for(int i=0;i < 6;i++)
			{
				indexes[i]	=(UInt16)i;
			}

			BufferDescription	id	=new BufferDescription(indexes.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, indexes, id);

			PrimObject	po	=new PrimObject(vb, 32, ib, indexes.Length, true);

			return	po;
		}


		public static PrimObject CreatePrism(Device gd, float size)
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
			Vector3	topUpperLeft	=-Vector3.UnitY + Vector3.UnitX + Vector3.UnitZ;
			Vector3	topUpperRight	=-Vector3.UnitY - Vector3.UnitX + Vector3.UnitZ;
			Vector3	topLowerLeft	=-Vector3.UnitY + Vector3.UnitX - Vector3.UnitZ;
			Vector3	topLowerRight	=-Vector3.UnitY - Vector3.UnitX - Vector3.UnitZ;
			Vector3	botUpperLeft	=Vector3.UnitY + Vector3.UnitX + Vector3.UnitZ;
			Vector3	botUpperRight	=Vector3.UnitY - Vector3.UnitX + Vector3.UnitZ;
			Vector3	botLowerLeft	=Vector3.UnitY + Vector3.UnitX - Vector3.UnitZ;
			Vector3	botLowerRight	=Vector3.UnitY - Vector3.UnitX - Vector3.UnitZ;

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
				32 * vpnt.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpnt, bd);

			//indexes
			UInt16	[]indexes	=new UInt16[24];

			//just reference in order
			for(int i=0;i < 24;i++)
			{
				indexes[i]	=(UInt16)i;
			}

			BufferDescription	id	=new BufferDescription(indexes.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, indexes, id);

			PrimObject	po	=new PrimObject(vb, 32, ib, indexes.Length, true);

			return	po;
		}


		public static PrimObject CreateHalfPrism(Device gd, float size)
		{
			Vector3	topPoint	=Vector3.Zero;
			Vector3	top			=(Vector3.UnitY * size * 5.0f) + size * Vector3.UnitZ;
			Vector3	bottom		=(Vector3.UnitY * size * 5.0f) + size * -Vector3.UnitZ;
			Vector3	left		=(Vector3.UnitY * size * 5.0f) + size * Vector3.UnitX;
			Vector3	right		=(Vector3.UnitY * size * 5.0f) + size * -Vector3.UnitX;

			VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[12];

			//hacky guessy normals for the 8 directions
			Vector3	topUpperLeft	=Vector3.UnitY + Vector3.UnitX + Vector3.UnitZ;
			Vector3	topUpperRight	=Vector3.UnitY - Vector3.UnitX + Vector3.UnitZ;
			Vector3	topLowerLeft	=Vector3.UnitY + Vector3.UnitX - Vector3.UnitZ;
			Vector3	topLowerRight	=Vector3.UnitY - Vector3.UnitX - Vector3.UnitZ;

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
				32 * vpnt.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpnt, bd);

			//indexes
			UInt16	[]indexes	=new UInt16[12];

			//just reference in order
			for(int i=0;i < 12;i++)
			{
				indexes[i]	=(UInt16)i;
			}

			BufferDescription	id	=new BufferDescription(indexes.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, indexes, id);

			PrimObject	po	=new PrimObject(vb, 32, ib, indexes.Length, true);

			return	po;
		}


		public static PrimObject CreateCube(Device gd, float size)
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

			return	CreateCube(gd, corners.ToArray());
		}


		public static PrimObject CreateCube(Device gd, BoundingBox box)
		{
			List<Vector3>	corners	=new List<Vector3>();

			//cube corners
			corners.Add(Vector3.UnitY * box.Minimum.Y + Vector3.UnitX * box.Maximum.X + Vector3.UnitZ * box.Maximum.Z);
			corners.Add(Vector3.UnitY * box.Minimum.Y + Vector3.UnitX * box.Minimum.X + Vector3.UnitZ * box.Maximum.Z);
			corners.Add(Vector3.UnitY * box.Minimum.Y + Vector3.UnitX * box.Maximum.X + Vector3.UnitZ * box.Minimum.Z);
			corners.Add(Vector3.UnitY * box.Minimum.Y + Vector3.UnitX * box.Minimum.X + Vector3.UnitZ * box.Minimum.Z);
			corners.Add(Vector3.UnitY * box.Maximum.Y + Vector3.UnitX * box.Maximum.X + Vector3.UnitZ * box.Maximum.Z);
			corners.Add(Vector3.UnitY * box.Maximum.Y + Vector3.UnitX * box.Minimum.X + Vector3.UnitZ * box.Maximum.Z);
			corners.Add(Vector3.UnitY * box.Maximum.Y + Vector3.UnitX * box.Maximum.X + Vector3.UnitZ * box.Minimum.Z);
			corners.Add(Vector3.UnitY * box.Maximum.Y + Vector3.UnitX * box.Minimum.X + Vector3.UnitZ * box.Minimum.Z);

			return	CreateCube(gd, corners.ToArray());
		}


		public static PrimObject CreateCube(Device gd, Vector3 []corners)
		{
			VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[24];

			//cube corners
			Vector3	upperTopLeft	=corners[0];
			Vector3	upperTopRight	=corners[1];
			Vector3	upperBotLeft	=corners[2];
			Vector3	upperBotRight	=corners[3];
			Vector3	lowerTopLeft	=corners[4];
			Vector3	lowerTopRight	=corners[5];
			Vector3	lowerBotLeft	=corners[6];
			Vector3	lowerBotRight	=corners[7];

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

			//x side
			vpnt[16].Position	=upperTopLeft;
			vpnt[17].Position	=upperBotLeft;
			vpnt[18].Position	=lowerBotLeft;
			vpnt[19].Position	=lowerTopLeft;

			//-x side
			vpnt[23].Position	=upperTopRight;
			vpnt[22].Position	=upperBotRight;
			vpnt[21].Position	=lowerBotRight;
			vpnt[20].Position	=lowerTopRight;

			//normals
			vpnt[0].Normal	=Vector3.UnitY;
			vpnt[1].Normal	=Vector3.UnitY;
			vpnt[2].Normal	=Vector3.UnitY;
			vpnt[3].Normal	=Vector3.UnitY;

			vpnt[4].Normal	=-Vector3.UnitY;
			vpnt[5].Normal	=-Vector3.UnitY;
			vpnt[6].Normal	=-Vector3.UnitY;
			vpnt[7].Normal	=-Vector3.UnitY;

			vpnt[8].Normal	=Vector3.UnitZ;
			vpnt[9].Normal	=Vector3.UnitZ;
			vpnt[10].Normal	=Vector3.UnitZ;
			vpnt[11].Normal	=Vector3.UnitZ;

			vpnt[12].Normal	=-Vector3.UnitZ;
			vpnt[13].Normal	=-Vector3.UnitZ;
			vpnt[14].Normal	=-Vector3.UnitZ;
			vpnt[15].Normal	=-Vector3.UnitZ;

			vpnt[16].Normal	=Vector3.UnitX;
			vpnt[17].Normal	=Vector3.UnitX;
			vpnt[18].Normal	=Vector3.UnitX;
			vpnt[19].Normal	=Vector3.UnitX;

			vpnt[20].Normal	=-Vector3.UnitX;
			vpnt[21].Normal	=-Vector3.UnitX;
			vpnt[22].Normal	=-Vector3.UnitX;
			vpnt[23].Normal	=-Vector3.UnitX;

			//texcoords
			for(int i=0;i < 24;i+=4)
			{
				vpnt[i].TextureCoordinate		=Vector2.Zero;
				vpnt[i + 1].TextureCoordinate	=Vector2.UnitX;
				vpnt[i + 2].TextureCoordinate	=Vector2.UnitX + Vector2.UnitY;
				vpnt[i + 3].TextureCoordinate	=Vector2.UnitY;
			}
			
			BufferDescription	bd	=new BufferDescription(
				32 * vpnt.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpnt, bd);

			UInt16	[]indexes	=new UInt16[36];

			int	idx	=0;
			for(int i=0;i < 36;i+=6)
			{
				indexes[i]		=(UInt16)(idx + 1);
				indexes[i + 1]	=(UInt16)(idx + 2);
				indexes[i + 2]	=(UInt16)(idx + 3);
				indexes[i + 3]	=(UInt16)(idx + 0);
				indexes[i + 4]	=(UInt16)(idx + 1);
				indexes[i + 5]	=(UInt16)(idx + 3);

				idx	+=4;
			}

			BufferDescription	id	=new BufferDescription(indexes.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, indexes, id);

			PrimObject	po	=new PrimObject(vb, 32, ib, indexes.Length, true);

			return	po;
		}


		public static PrimObject CreateSphere(Device gd, Vector3 center, float radius)
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

					float	rtheta	=MathUtil.DegreesToRadians(theta);
					float	rdtheta	=MathUtil.DegreesToRadians(dtheta);
					float	rphi	=MathUtil.DegreesToRadians(phi);
					float	rdphi	=MathUtil.DegreesToRadians(dphi);

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
				vpnt[i].Normal	=points[i];
				vpnt[i].Normal.Normalize();

				vpnt[i].Position			=vpnt[i].Normal * radius + center;
				vpnt[i].TextureCoordinate	=Vector2.Zero;	//not tackling this yet
			}

			//dupe for other half
			int	ofs	=points.Count;
			for(int i=ofs;i < points.Count + ofs;i++)
			{
				vpnt[i].Normal	=points[i - ofs];
				vpnt[i].Normal.Normalize();

				//flip normal
				vpnt[i].Normal	=-vpnt[i].Normal;

				vpnt[i].Position			=vpnt[i].Normal * radius + center;
				vpnt[i].TextureCoordinate	=Vector2.Zero;	//not tackling this yet
			}

			BufferDescription	bd	=new BufferDescription(
				32 * vpnt.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpnt, bd);

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

			inds.Reverse();

			BufferDescription	id	=new BufferDescription(inds.Count * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, inds.ToArray(), id);

			PrimObject	po	=new PrimObject(vb, 32, ib, inds.Count, true);

			return	po;
		}


		public static PrimObject CreateShadowCircle(Device gd, float radius)
		{
			VertexPositionColor	[]vpc	=new VertexPositionColor[16];

			//can't remember how to generate a circle
			//I know it's something with sin or something? Pi?
			//just going to use a matrix
			Matrix	rotMat	=Matrix.RotationY(MathUtil.TwoPi / 8.0f);
			Vector3	rotPos	=Vector3.UnitX * radius * 0.25f;

			Color	halfDarken	=new Color(new Vector4(0.0f, 0.0f, 0.0f, 0.35f));
			Color	fadeOut		=new Color(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

			//inner ring
			for(int i=0;i < 8;i++)
			{
				vpc[i].Position	=rotPos;
				rotPos			=Vector3.TransformCoordinate(rotPos, rotMat);
				vpc[i].Color	=halfDarken;
			}

			//outer ring
			rotPos	=Vector3.UnitX * radius;
			for(int i=8;i < 16;i++)
			{
				vpc[i].Position	=rotPos;
				rotPos			=Vector3.TransformCoordinate(rotPos, rotMat);
				vpc[i].Color	=fadeOut;
			}

			BufferDescription	bd	=new BufferDescription(
				16 * vpc.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpc, bd);

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
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, indexes, id);

			PrimObject	po	=new PrimObject(vb, 16, ib, indexes.Length, true);

			return	po;
		}


		public static PrimObject CreateCylinder(Device gd, float radius, float len)
		{
			VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[16];

			//can't remember how to generate a circle
			//I know it's something with sin or something? Pi?
			//just going to use a matrix
			Matrix	rotMat	=Matrix.RotationY(MathUtil.TwoPi / 8.0f);
			Vector3	rotPos	=Vector3.UnitX * radius;

			//make top and bottom surfaces
			for(int i=0;i < 8;i++)
			{
				vpnt[i].Position		=rotPos;
				vpnt[i + 8].Position	=rotPos - Vector3.UnitY * len;

				vpnt[i].Normal		=-Vector3.UnitY;
				vpnt[i + 8].Normal	=Vector3.UnitY;

				rotPos	=Vector3.TransformCoordinate(rotPos, rotMat);

				Vector3	rotDir	=rotPos;
				rotDir.Normalize();

				rotDir	*=0.5f;

				vpnt[i].TextureCoordinate.X		=rotDir.X;
				vpnt[i].TextureCoordinate.Y		=rotDir.Z;
				vpnt[i + 8].TextureCoordinate.X	=rotDir.X;
				vpnt[i + 8].TextureCoordinate.Y	=rotDir.Z;
			}

			//shift to center
			for(int i=0;i < 16;i++)
			{
				vpnt[i].Position	+=Vector3.UnitY * (len);
			}

			BufferDescription	bd	=new BufferDescription(
				32 * vpnt.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	vb	=Buffer.Create(gd, vpnt, bd);

			UInt16	[]indexes	=new UInt16[36 + 48];

			//top surface
			indexes[17]	=0;
			indexes[16]	=1;
			indexes[15]	=7;
			indexes[14]	=1;
			indexes[13]	=2;
			indexes[12]	=3;
			indexes[11]	=3;
			indexes[10]	=4;
			indexes[9]	=5;
			indexes[8]	=5;
			indexes[7]	=6;
			indexes[6]	=7;
			indexes[5]	=7;
			indexes[4]	=1;
			indexes[3]	=5;
			indexes[2]	=1;
			indexes[1]	=3;
			indexes[0]	=5;

			//bottom surface
			indexes[18]	=0 + 8;
			indexes[19]	=1 + 8;
			indexes[20]	=7 + 8;
			indexes[21]	=1 + 8;
			indexes[22]	=2 + 8;
			indexes[23]	=3 + 8;
			indexes[24]	=3 + 8;
			indexes[25]	=4 + 8;
			indexes[26]	=5 + 8;
			indexes[27]	=5 + 8;
			indexes[28]	=6 + 8;
			indexes[29]	=7 + 8;
			indexes[30]	=7 + 8;
			indexes[31]	=1 + 8;
			indexes[32]	=5 + 8;
			indexes[33]	=1 + 8;
			indexes[34]	=3 + 8;
			indexes[35]	=5 + 8;

			//connexions
			for(int i=0;i < 7;i++)
			{
				indexes[38 + (i * 6)]	=(UInt16)(i + 1);
				indexes[37 + (i * 6)]	=(UInt16)(i + 0);
				indexes[36 + (i * 6)]	=(UInt16)(i + 8);
				indexes[41 + (i * 6)]	=(UInt16)(i + 8);
				indexes[40 + (i * 6)]	=(UInt16)(i + 9);
				indexes[39 + (i * 6)]	=(UInt16)(i + 1);
			}

			//last 2 faces are goofy
			indexes[38 + 42]	=(UInt16)(0);
			indexes[37 + 42]	=(UInt16)(7);
			indexes[36 + 42]	=(UInt16)(7 + 8);
			indexes[41 + 42]	=(UInt16)(7 + 8);
			indexes[40 + 42]	=(UInt16)(8);
			indexes[39 + 42]	=(UInt16)(0);

			BufferDescription	id	=new BufferDescription(indexes.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			Buffer	ib	=Buffer.Create<UInt16>(gd, indexes, id);

			PrimObject	po	=new PrimObject(vb, 32, ib, indexes.Length, true);

			return	po;
		}
	}
}
