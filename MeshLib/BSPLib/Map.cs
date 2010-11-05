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
		BspTree			mDrawTree;
		BspTree			mCollisionTree;

		bool		mbBuilding;
		int			mMaxCPUCores;
		bool		mbBevel;

		List<Brush>	mDebugBrushes;		//debuggery remove
		List<Brush>	mDrawBrushes;		//visible geometry
		List<Brush>	mCollisionBrushes;	//collision hull

		public event EventHandler	eCPUCoresInUseChanged;
		public event EventHandler	eNumMapFacesChanged;
		public event EventHandler	eNumDrawFacesChanged;
		public event EventHandler	eNumCollisionFacesChanged;
		public event EventHandler	eProgressChanged;

		static public event EventHandler	ePrint;

		event EventHandler	eDrawCSGDone;
		event EventHandler	eCollisionCSGDone;


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
		public void GetTriangles(List<Vector3> verts, List<ushort> indexes, string drawChoice)
		{
			if(drawChoice == "Map Brushes")
			{
				foreach(Brush b in mDebugBrushes)
				{
					b.GetTriangles(verts, indexes);
				}
			}
			else if(drawChoice == "Draw Brushes")
			{
				foreach(Brush b in mDrawBrushes)
				{
					b.GetTriangles(verts, indexes);
				}
			}
			else if(drawChoice == "Collision Brushes")
			{
				foreach(Brush b in mCollisionBrushes)
				{
					b.GetTriangles(verts, indexes);
				}
			}
			else if(drawChoice == "Draw Tree")
			{
				mDrawTree.GetTriangles(verts, indexes);
			}
			else if(drawChoice == "Collision Tree")
			{
				mCollisionTree.GetTriangles(verts, indexes);
			}
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mCollisionTree.ClassifyPoint(pnt);
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
			mDrawTree.Write(bw);
		}
		#endregion


		//makes sure that only one volume
		//occupies any space
		void RemoveOverlap(List<Brush> brushes, object prog)
		{
			int	iterationCount	=0;
			for(int i=0;i < brushes.Count;i++)
			{
				for(int j=0;j < brushes.Count;j++, iterationCount++)
				{
					if(i == j)
					{
						continue;
					}

					if(iterationCount > 100000)
					{
						ProgressWatcher.UpdateProgress(prog, 0, brushes.Count, i);
						iterationCount	=0;
					}

					if(!brushes[i].Intersects(brushes[j]))
					{
						continue;
					}

					List<Brush>	cutup	=new List<Brush>();
					List<Brush>	cutup2	=new List<Brush>();

					if(brushes[i].SubtractBrush(brushes[j], out cutup))
					{
						//make sure the brush returned is
						//not the one passed in
						if(cutup.Count == 1)
						{
							Debug.Assert(!brushes[i].Equals(cutup[0]));
						}
					}
					else
					{
						cutup.Clear();
					}

					if(brushes[j].SubtractBrush(brushes[i], out cutup2))
					{
						//make sure the brush returned is
						//not the one passed in
						if(cutup2.Count == 1)
						{
							Debug.Assert(!brushes[j].Equals(cutup2[0]));
						}
					}
					else
					{
						cutup2.Clear();
					}

					if(cutup.Count==0 && cutup2.Count==0)
					{
						continue;
					}

					if(cutup.Count > 4 && cutup2.Count > 4)
					{
						continue;
					}

					if(cutup.Count < cutup2.Count)
					{
						cutup2.Clear();

						foreach(Brush b in cutup)
						{
							if(b.IsValid())
							{
								brushes.Add(b);
							}
						}
						cutup.Clear();
						brushes.RemoveAt(i);
						i--;
						break;
					}
					else
					{
						cutup.Clear();

						foreach(Brush b in cutup2)
						{
							if(b.IsValid())
							{
								brushes.Add(b);
							}
						}
						cutup2.Clear();
						brushes.RemoveAt(j);
						j--;
						continue;
					}
				}
			}

			//nuke thins and invalids
			for(int i=0;i < brushes.Count;i++)
			{
				Brush	b	=brushes[i];

				if(!b.IsValid())
				{
					Print("Invalid brush removed");
					brushes.RemoveAt(i);
					i--;
					continue;
				}

				if(b.IsVeryThin())
				{
					b.RemoveVeryThinSides();
					if(!b.IsValid())
					{
						Print("Thin brush removed");
						brushes.RemoveAt(i);
						i--;
					}
				}
			}
		}


		void CopyDetailBrushes(List<Brush> details)
		{
			foreach(Entity e in mEntities)
			{
				if(e.mBrushes != null)
				{
					if(e.mData.ContainsKey("classname"))
					{
						if(e.mData["classname"] == "func_detail")
						{
							foreach(Brush db in e.mBrushes)
							{
								Brush	b	=new Brush(db);

								details.Add(b);
							}
						}
					}
				}
			}
		}


		void PromoteClips(List<Brush> brushes)
		{
			foreach(Brush b in brushes)
			{
				b.PromoteClips();
			}
		}


		void CBRemoveDrawOverlap(object context)
		{
			Print("Draw overlap removal thread starting\n");

			//copy map brushes
			mDrawBrushes	=new List<Brush>();
			foreach(Brush b in mDebugBrushes)
			{
				Brush	db	=new Brush(b);
				mDrawBrushes.Add(db);
			}

			if(eNumDrawFacesChanged != null)
			{
				eNumDrawFacesChanged(mDrawBrushes.Count, null);
			}

			object	prog	=ProgressWatcher.RegisterProgress(0, mDrawBrushes.Count, 0);

			RemoveOverlap(mDrawBrushes, prog);

			ProgressWatcher.DestroyProgress(prog);

			Print("" + mDrawBrushes.Count + " draw brushes after overlap removal\n");

			if(eNumDrawFacesChanged != null)
			{
				eNumDrawFacesChanged(mDrawBrushes.Count, null);
			}
			if(eDrawCSGDone != null)
			{
				eDrawCSGDone(null, null);
			}
		}


		void CBRemoveCollisionOverlap(object context)
		{
			Print("Collision overlap removal thread starting\n");

			//copy map brushes
			mCollisionBrushes	=new List<Brush>();
			foreach(Brush b in mDebugBrushes)
			{
				Brush	cb	=new Brush(b);
				mCollisionBrushes.Add(cb);
			}

			//promote clips to solid and make the collision hull
			PromoteClips(mCollisionBrushes);
			if(eNumCollisionFacesChanged != null)
			{
				eNumCollisionFacesChanged(mDrawBrushes.Count, null);
			}

			object	prog	=ProgressWatcher.RegisterProgress(0, mDrawBrushes.Count, 0);

			RemoveOverlap(mCollisionBrushes, prog);

			ProgressWatcher.DestroyProgress(prog);

			Print("" + mCollisionBrushes.Count + " collision brushes after overlap removal\n");

			if(eNumCollisionFacesChanged != null)
			{
				eNumCollisionFacesChanged(mDrawBrushes.Count, null);
			}

			if(eCollisionCSGDone != null)
			{
				eCollisionCSGDone(context, null);
			}
		}


		void OnCollisionCSGDone(object sender, EventArgs ea)
		{
			eCollisionCSGDone	-=OnCollisionCSGDone;

			Print("Starting collision tree build\n");

			//dupe collision list
			List<Brush>	colls	=new List<Brush>();
			foreach(Brush b in mCollisionBrushes)
			{
				colls.Add(new Brush(b));
			}

			mCollisionTree	=new BspTree(colls, sender != null);
		}


		void OnDrawCSGDone(object sender, EventArgs ea)
		{
			eDrawCSGDone	-=OnDrawCSGDone;

			Print("Starting draw tree build\n");

			//dupe draw list
			List<Brush>	draws	=new List<Brush>();
			foreach(Brush b in mDrawBrushes)
			{
				draws.Add(new Brush(b));
			}

			mDrawTree	=new BspTree(draws, false);
		}


		static internal void Print(string str)
		{
			EventHandler	evt	=ePrint;
			if(evt != null)
			{
				if(str.EndsWith("\n"))
				{
					evt(str, null);
				}
				else
				{
					evt(str + "\n", null);
				}
			}
		}


		public void BuildTree(bool bBevel, int maxCPUCores)
		{
			mbBuilding		=true;
			mbBevel			=bBevel;
			mMaxCPUCores	=maxCPUCores;

			//look for the worldspawn
			Entity	wse	=GetWorldSpawnEntity();

			//copy all starting brushes to debug
			mDebugBrushes	=new List<Brush>();
			foreach(Brush b in wse.mBrushes)
			{
				Brush	cb	=new Brush(b);
				mDebugBrushes.Add(cb);
			}
			CopyDetailBrushes(mDebugBrushes);
			if(eNumMapFacesChanged != null)
			{
				eNumMapFacesChanged(mDebugBrushes.Count, null);
			}

			Print("" + mDebugBrushes.Count + " map brushes\n");

			Print("Starting two threads for overlap removal\n");

			eCollisionCSGDone	+=OnCollisionCSGDone;
			eDrawCSGDone		+=OnDrawCSGDone;

			//start two threads for removing overlap
			//for the two datasets
			ThreadPool.QueueUserWorkItem(CBRemoveDrawOverlap);
			ThreadPool.QueueUserWorkItem(CBRemoveCollisionOverlap, bBevel ? new object() : null);
		}
	}
}
