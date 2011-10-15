#if SILVERLIGHT
// Silverlight needs to know the conventional colour,
// Even when fading between colours. So keep that data around!
#define EXEN_WIDE_COLOR
#endif
using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework
{

#if EXEN_WIDE_COLOR
	[DebuggerDisplay("R:{R} G:{G} B:{B} A:{A} | wR:{wR} wG:{wG} wB:{wB} wQ:{wQ}")]
#endif
	public partial struct Color : IEquatable<Color>
	{

		#region Packed Value Helpers

		private static uint Pack(float v)
		{
			v *= 255f;
			if(v < 0f)
				v = 0;
			else if(v > 255f)
				v = 255f;

			Debug.Assert((uint)Math.Round(v) < 256);
			return (uint)Math.Round(v);
		}

		private static uint Pack(float r, float g, float b, float a)
		{
			return (Pack(r)) | (Pack(g)<<8) | (Pack(b)<<16) | (Pack(a)<<24);
		}

		private static uint Pack(int r, int g, int b, int a)
		{
			return (((uint)r&0xff)) | (((uint)g&0xff)<<8) | (((uint)b&0xff)<<16) | (((uint)a&0xff)<<24);
		}

		public const uint OpaqueMask = 0xffffff;

		#endregion


		#region Constructors

		private Color(uint packedValue)
		{
			this.packedValue = packedValue;

#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		public Color(Vector4 color)
		{
			packedValue = Pack(color.X, color.Y, color.Z, color.W);
#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		public Color(Vector3 color)
		{
			packedValue = Pack(color.X, color.Y, color.Z, 1f);
#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		public Color(float r, float g, float b)
		{
			packedValue = Pack(r, g, b, 1f);
#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		public Color(float r, float g, float b, float alpha)
		{
			packedValue = Pack(r, g, b, alpha);
#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		public Color(int r, int g, int b)
		{
			packedValue = Pack(r, g, b, 255);
#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		public Color(int r, int g, int b, int alpha)
		{
			packedValue = Pack(r, g, b, alpha);
#if EXEN_WIDE_COLOR
			wideValue = packedValue;
#endif
		}

		#endregion


		#region Data

		private uint packedValue;

		/// <summary>The colour as a 32-bit integer packed as ABGR (alpha is most significant)</summary>
		public uint PackedValue
		{
			get { return packedValue; }
			set
			{
				packedValue = value;
#if EXEN_WIDE_COLOR
				wideValue = packedValue;
#endif
			}
		}


		public byte R
		{
			get { return (byte)(packedValue); }
			set
			{
				packedValue = (packedValue & 0xffffff00) | ((uint)(value));
#if EXEN_WIDE_COLOR
				wideValue = packedValue;
#endif
			}
		}

		public byte G
		{
			get { return (byte)(packedValue >> 8); }
			set
			{
				packedValue = (packedValue & 0xffff00ff) | ((uint)(value << 8));
#if EXEN_WIDE_COLOR
				wideValue = packedValue;
#endif
			}
		}

		public byte B
		{
			get { return (byte)(packedValue >> 16); }
			set
			{
				packedValue = (packedValue & 0xff00ffff) | ((uint)(value << 16));
#if EXEN_WIDE_COLOR
				wideValue = packedValue;
#endif
			}
		}

		public byte A
		{
			get { return (byte)(packedValue >> 24); }
			set
			{
				packedValue = (packedValue & 0x00ffffff) | ((uint)(value << 24));
#if EXEN_WIDE_COLOR
				wideValue = packedValue;
#endif
			}
		}


		public Vector3 ToVector3()
		{
			Vector3 vector = new Vector3();
			vector.X = R / 255f;
			vector.Y = G / 255f;
			vector.Z = B / 255f;
			return vector;
		}

		public Vector4 ToVector4()
		{
			Vector4 vector = new Vector4();
			vector.X = R / 255f;
			vector.Y = G / 255f;
			vector.Z = B / 255f;
			vector.W = A / 255f;
			return vector;
		}

		#endregion


		#region Wide Colour
#if EXEN_WIDE_COLOR

		private uint wideValue;

		/// <summary>
		/// The conventional colour value, packed as QBGR, 
		/// where Q represents the denominator of the fraction used to normalize the colour.
		/// </summary>
		public uint WideValue { get { return wideValue; } }

		// Unnormalized conventional colour
		public byte wR { get { return (byte)(wideValue); } }
		public byte wG { get { return (byte)(wideValue >> 8); } }
		public byte wB { get { return (byte)(wideValue >> 16); } }
		public byte wQ { get { return (byte)(wideValue >> 24); } }

		// Normalized conventional colour
		public byte cR { get { return (byte)Math.Min(255f, (wR/(wQ/255f))); } }
		public byte cG { get { return (byte)Math.Min(255f, (wG/(wQ/255f))); } }
		public byte cB { get { return (byte)Math.Min(255f, (wB/(wQ/255f))); } }


		private Color(uint packedValue, uint wideValue)
		{
			this.packedValue = packedValue;
			// If the actual value becomes transparent, drop the conventional colour
			this.wideValue = (packedValue != 0) ? wideValue : 0;
		}

#if SILVERLIGHT
		public System.Windows.Media.Color ToOpaqueSilverlightColor()
		{
			if(wQ == 255)
				return System.Windows.Media.Color.FromArgb(255, wR, wG, wB);
			else // requires normalization
			{
				float q = wQ/255f;
				return System.Windows.Media.Color.FromArgb(255, (byte)(wR/q), (byte)(wG/q), (byte)(wB/q));
			}
		}
#endif

#endif
		#endregion


		#region Functions

		public static Color Lerp(Color v1, Color v2, float amount)
		{
			// TODO: write a fixed-point lerp function

			uint p = Pack(
					(int)MathHelper.Lerp(v1.R, v2.R, amount),
					(int)MathHelper.Lerp(v1.G, v2.G, amount),
					(int)MathHelper.Lerp(v1.B, v2.B, amount),
					(int)MathHelper.Lerp(v1.A, v2.A, amount));

	
#if EXEN_WIDE_COLOR
			uint w;

			// TODO: This could use some more maths.

			// We could do the maths to figure out how to lerp the conventional
			// colours when they aren't normalized... or we could just drop in some
			// conditionals and hope for the best!
			if(v1.wideValue == v2.wideValue)
				w = v1.wideValue;
			else if(v1.wQ == 255 && v2.wQ == 255)
			{
				w = Pack(
						(int)MathHelper.Lerp(v1.cR, v2.cR, amount),
						(int)MathHelper.Lerp(v1.cG, v2.cG, amount),
						(int)MathHelper.Lerp(v1.cB, v2.cB, amount), 255);
			}
			else if(v2.wQ == 0)
				w = v1.wideValue;
			else if(v1.wQ == 0)
				w = v2.wideValue;
			else
				w = p; // Hope for the best :)

			return new Color(p, w);
#else
			return new Color(p);
#endif
		}

		public static Color FromNonPremultiplied(int r, int g, int b, int a)
		{
			return new Color(Pack((r/255f)*(a/255f), (g/255f)*(a/255f), (b/255f)*(a/255f), (a/255f))
#if EXEN_WIDE_COLOR
					, Pack(r, g, b, 255)
#endif
					);
		}

		public static Color FromNonPremultiplied(Vector4 c)
		{
			return new Color(Pack(c.X * c.W, c.Y * c.W, c.Z * c.W, c.W)
#if EXEN_WIDE_COLOR
					, Pack(c.X, c.Y, c.Z, 1f)
#endif
					);
		}

		public static Color Multiply(Color value, float scale)
		{
			return value * scale;
		}

		public static Color operator*(Color value, float scale)
		{
			uint p = Pack((value.R/255f)*scale, (value.G/255f)*scale, (value.B/255f)*scale, (value.A/255f)*scale);

#if EXEN_WIDE_COLOR
			return new Color(p, value.wideValue);
#else
			return new Color(p);
#endif
		}

		#endregion


		#region Object Members

		public static bool operator==(Color lhs, Color rhs)
		{
			return (lhs.packedValue == rhs.packedValue);
		}

		public static bool operator!=(Color lhs, Color rhs)
		{
			return (lhs.packedValue != rhs.packedValue);
		}

		public override int GetHashCode()
		{
			return this.packedValue.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return ((obj is Color) && this.Equals((Color)obj));
		}

		public override string ToString()
		{
			return string.Format("{{R:{0} G:{1} B:{2} A:{3}}}", R, G, B, A);
		}

		#endregion


		#region IEquatable<Color> Members

		public bool Equals(Color other)
		{
			return (other.packedValue == this.packedValue);
		}

		#endregion

	}
}
