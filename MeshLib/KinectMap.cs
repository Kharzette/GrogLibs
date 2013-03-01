using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Kinect;


namespace MeshLib
{
	public class KinectMap
	{
		public JointType	Joint		{ get; set; }
		public float		RotX		{ get; set; }
		public float		RotY		{ get; set; }
		public float		RotZ		{ get; set; }
		public string		CharBone	{ get; set; }


		public KinectMap(JointType jt)
		{
			Joint	=jt;
		}

		public KinectMap(BinaryReader br)
		{
			Joint		=(JointType)br.ReadUInt32();
			RotX		=br.ReadSingle();
			RotY		=br.ReadSingle();
			RotZ		=br.ReadSingle();
			CharBone	=br.ReadString();
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write((UInt32)Joint);
			bw.Write(RotX);
			bw.Write(RotY);
			bw.Write(RotZ);

			if(CharBone == null)
			{
				bw.Write("");
			}
			else
			{
				bw.Write(CharBone);
			}
		}
	}
}
