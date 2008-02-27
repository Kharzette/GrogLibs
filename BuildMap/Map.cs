using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;


namespace BuildMap
{
    class Map
    {
        List<Brush> mBrushes;


        //reads a .map file
        public Map(string mapFileName)
        {
            mBrushes = new List<Brush>();
            if(File.Exists(mapFileName))
            {
                using(StreamReader sr = File.OpenText(mapFileName))
                {
                    string s = "";
                    Brush b =new Brush();

                    while((s = sr.ReadLine()) != null)
                    {
                        s = s.Trim();

                        //skip past the crap
                        if(s.StartsWith("}"))
                        {
                            //seal the brush
                            b.SealFaces();

                            if(b.IsValid())
                            {
                                mBrushes.Add(b);
                            }
                            b = new Brush();
                        }
                        else if(s.StartsWith("("))
                        {
                            b.MakeFaceFromMapLine(s);
                        }
                    }
                }
            }
        }


        public void Draw(GraphicsDevice g)
        {
            foreach(Brush b in mBrushes)
            {
                b.Draw(g);
            }
        }


        public void RemoveOverlap()
        {
            int i, j;

            i = 1;
        startoveragain:
            if (i > 0)
            {
                i--;
            }

            for (; i < mBrushes.Count; i++)
            {
                for(j = 0;j < mBrushes.Count;j++)
                {
                    if(i == j)
                    {
                        continue;
                    }

                    if(!mBrushes[i].Intersects(mBrushes[j]))
                    {
                        continue;
                    }

                    List<Brush> cutup = new List<Brush>();
                    List<Brush> cutup2 = new List<Brush>();

                    if(mBrushes[i].SubtractBrush(mBrushes[j], out cutup))
                    {
                        //make sure the brush returned is
                        //not the one passed in
                        if(cutup.Count == 1)
                        {
                            if(mBrushes[i].Equals(cutup[0]))
                            {
                                continue;
                            }
                        }

                        if(cutup.Count == 0)
                        {
                            Debug.WriteLine("Subtract returned true, but an empty list");
                        }
                    }
                    else
                    {
                        cutup.Clear();
                    }

                    if(mBrushes[j].SubtractBrush(mBrushes[i], out cutup2))
                    {
                        //make sure the brush returned is
                        //not the one passed in
                        if(cutup2.Count == 1)
                        {
                            if(mBrushes[j].Equals(cutup2[0]))
                            {
                                continue;
                            }
                        }

                        if(cutup2.Count == 0)
                        {
                            Debug.WriteLine("Subtract returned true, but an empty list");
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
                            mBrushes.Add(b);
                        }
                        cutup.Clear();
                        mBrushes.RemoveAt(i);
                        goto startoveragain;
                    }
                    else
                    {
                        cutup.Clear();

                        foreach(Brush b in cutup2)
                        {
                            mBrushes.Add(b);
                        }
                        cutup2.Clear();
                        mBrushes.RemoveAt(j);
                        goto startoveragain;
                    }
                }
            }

            bool    bDone   =false;

            //see if any got completely eaten
            while(!bDone)
            {
                bDone = true;
                foreach(Brush b in mBrushes)
                {
                    if(!b.IsValid())
                    {
                        Debug.WriteLine("Brush totally clipped away");

                        mBrushes.Remove(b);
                        bDone = false;
                        break;
                    }
                }
            }
        }
    }
}
