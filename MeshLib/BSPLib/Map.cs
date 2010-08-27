using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using Microsoft.Xna.Framework.Storage;


namespace BSPLib
{
	public class Map
	{
		List<Entity>	mEntities;
		BspTree			mTree;


		#region Constructors
		//reads a .map file
		public Map(string mapFileName)
		{
			mEntities	=new List<Entity>();

			if(File.Exists(mapFileName))
			{
				using(StreamReader sr = File.OpenText(mapFileName))
				{
					string	s	="";

					//see if this is a .map or a .vmf
					if(mapFileName.EndsWith(".map"))
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s.StartsWith("{"))
							{
								Entity	e	=new Entity();

								e.ReadFromMap(sr);

								mEntities.Add(e);
							}
						}
					}
					else
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s == "entity")
							{
								Entity	e	=new Entity();
								e.ReadVMFEntBlock(sr);
								mEntities.Add(e);
							}
							else if(s == "world")
							{
								Entity	e	=new Entity();
								e.ReadVMFWorldBlock(sr);
								mEntities.Add(e);
							}
							else if(s == "cameras")
							{
								Brush.SkipVMFEditorBlock(sr);
							}
							else if(s == "cordon")
							{
								Brush.SkipVMFEditorBlock(sr);
							}
						}
					}
				}
			}
		}
		#endregion


		#region Queries
		public int GetNumPortals(int entityIndex)
		{
			return	mTree.GetNumPortals();
/*
			if(entityIndex >= mEntities.Count)
			{
				return	0;
			}
			if(entityIndex < 0)
			{
				return	0;
			}
			return	mEntities[entityIndex].mBrushes.Count;*/
		}


		public void GetTriangles(List<Vector3> verts, List<ushort> indexes, int entityIndex)
		{
/*			if(entityIndex >= mEntities.Count)
			{
				return;
			}
			if(entityIndex < 0)
			{
				return;
			}

//			if(mEntities[entityIndex].mBrushes != null)
			foreach(Entity ent in mEntities)
			{
				if(ent.mBrushes != null)
				{
//					foreach(Brush b in mEntities[entityIndex].mBrushes)
					foreach(Brush b in ent.mBrushes)
					{
						b.SealFaces();
						b.GetTriangles(verts, indexes);
					}
				}
			}*/
			mTree.GetTriangles(verts, indexes);
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mTree.ClassifyPoint(pnt);
		}


		public Vector3 GetFirstLightPos()
		{
			foreach(Entity e in mEntities)
			{
				if(e == GetWorldSpawnEntity())
				{
					continue;
				}
				float dist;
				if(e.GetLightValue(out dist))
				{
					Vector3	ret;
					e.GetOrigin(out ret);
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public Entity GetWorldSpawnEntity()
		{
			foreach(Entity e in mEntities)
			{
				if(e.mData.ContainsKey("classname"))
				{
					if(e.mData["classname"] == "worldspawn")
					{
						return	e;
					}
				}
			}
			return	null;
		}
		#endregion


		#region IO
		public void Save(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			bw.Write(mEntities.Count);

			//write all entities
			foreach(Entity e in mEntities)
			{
				e.Write(bw);
			}

			//write bsp
			mTree.Write(bw);
		}
		#endregion


		public void BuildTree(float bevelDistance, bool bBevel)
		{
			//look for the worldspawn
			Entity		e	=GetWorldSpawnEntity();
			List<Brush>	copy;

			copy	=new List<Brush>(e.mBrushes);

			//use the copy so we have the old ones around to draw
			mTree	=new BspTree(copy, bevelDistance, bBevel);
		}
		

		public void RemoveOverlap()
		{
			List<Brush>	brushes	=null;

			Entity	wse	=GetWorldSpawnEntity();

			brushes	=wse.mBrushes;

			BspTree.RemoveOverlap(brushes);
		}
	}
}
