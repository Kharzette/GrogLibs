using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPLib
{
	public interface IReadWriteable
	{
		void Write(BinaryWriter bw);
		void Read(BinaryReader br);
	}
}
