using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework
{
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct Matrix : IEquatable<Matrix>
	{

		#region Matrix Elements

		public float M11;
		public float M12;
		public float M13;
		public float M14;
		public float M21;
		public float M22;
		public float M23;
		public float M24;
		public float M31;
		public float M32;
		public float M33;
		public float M34;
		public float M41;
		public float M42;
		public float M43;
		public float M44;

		#endregion


		#region Identity and Constructor

		public static readonly Matrix Identity = new Matrix(1, 0, 0, 0,  0, 1, 0, 0,  0, 0, 1, 0,  0, 0, 0, 1);

		public Matrix(float m11, float m12, float m13, float m14,
		              float m21, float m22, float m23, float m24,
		              float m31, float m32, float m33, float m34, 
		              float m41, float m42, float m43, float m44)
		{
			this.M11 = m11;
			this.M12 = m12;
			this.M13 = m13;
			this.M14 = m14;
			this.M21 = m21;
			this.M22 = m22;
			this.M23 = m23;
			this.M24 = m24;
			this.M31 = m31;
			this.M32 = m32;
			this.M33 = m33;
			this.M34 = m34;
			this.M41 = m41;
			this.M42 = m42;
			this.M43 = m43;
			this.M44 = m44;
		}

		// object
		public override string ToString()
		{
			return string.Format("{{ {{M11:{0} M12:{1} M13:{2} M14:{3}}} {{M21:{4} M22:{5} M23:{6} M24:{7}}} {{M31:{8} M32:{9} M33:{10} M34:{11}}} {{M41:{12} M42:{13} M43:{14} M44:{15}}} }}",
					M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
		}

		#endregion


		#region Silverlight
#if SILVERLIGHT

		public System.Windows.Media.Matrix AsSilverlightMatrix
		{
			get
			{
				return new System.Windows.Media.Matrix(M11, M12, M21, M22, M41, M42);
			}
		}

		public bool IsAffineMatrix
		{
			get
			{
				return M13 == 0f && M14 == 0f && M23 == 0f && M23 == 0f &&
						M31 == 0f && M32 == 0f && M33 == 1f && M34 == 0f &&
						M43 == 0f && M44 == 1f;
			}
		}

#endif
		#endregion


		#region Equality Comparisons

		static bool Equals(ref Matrix matrix1, ref Matrix matrix2)
		{
			return matrix1.M11 == matrix2.M11
			    && matrix1.M12 == matrix2.M12
			    && matrix1.M13 == matrix2.M13
			    && matrix1.M14 == matrix2.M14
			    && matrix1.M21 == matrix2.M21
			    && matrix1.M22 == matrix2.M22
			    && matrix1.M23 == matrix2.M23
			    && matrix1.M24 == matrix2.M24
			    && matrix1.M31 == matrix2.M31
			    && matrix1.M32 == matrix2.M32
			    && matrix1.M33 == matrix2.M33
			    && matrix1.M34 == matrix2.M34
			    && matrix1.M41 == matrix2.M41
			    && matrix1.M42 == matrix2.M42
			    && matrix1.M43 == matrix2.M43
			    && matrix1.M44 == matrix2.M44;
		}

		public static bool operator ==(Matrix matrix1, Matrix matrix2)
		{
			return matrix1.M11 == matrix2.M11
			    && matrix1.M12 == matrix2.M12
			    && matrix1.M13 == matrix2.M13
			    && matrix1.M14 == matrix2.M14
			    && matrix1.M21 == matrix2.M21
			    && matrix1.M22 == matrix2.M22
			    && matrix1.M23 == matrix2.M23
			    && matrix1.M24 == matrix2.M24
			    && matrix1.M31 == matrix2.M31
			    && matrix1.M32 == matrix2.M32
			    && matrix1.M33 == matrix2.M33
			    && matrix1.M34 == matrix2.M34
			    && matrix1.M41 == matrix2.M41
			    && matrix1.M42 == matrix2.M42
			    && matrix1.M43 == matrix2.M43
			    && matrix1.M44 == matrix2.M44;
		}

		public static bool operator !=(Matrix matrix1, Matrix matrix2)
		{
			return !(matrix1.M11 == matrix2.M11
			      && matrix1.M12 == matrix2.M12
			      && matrix1.M13 == matrix2.M13
			      && matrix1.M14 == matrix2.M14
			      && matrix1.M21 == matrix2.M21
			      && matrix1.M22 == matrix2.M22
			      && matrix1.M23 == matrix2.M23
			      && matrix1.M24 == matrix2.M24
			      && matrix1.M31 == matrix2.M31
			      && matrix1.M32 == matrix2.M32
			      && matrix1.M33 == matrix2.M33
			      && matrix1.M34 == matrix2.M34
			      && matrix1.M41 == matrix2.M41
			      && matrix1.M42 == matrix2.M42
			      && matrix1.M43 == matrix2.M43
			      && matrix1.M44 == matrix2.M44);
		}

		// IEquatable<Matrix>
		public bool Equals(Matrix other)
		{
			return M11 == other.M11
			    && M12 == other.M12
			    && M13 == other.M13
			    && M14 == other.M14
			    && M21 == other.M21
			    && M22 == other.M22
			    && M23 == other.M23
			    && M24 == other.M24
			    && M31 == other.M31
			    && M32 == other.M32
			    && M33 == other.M33
			    && M34 == other.M34
			    && M41 == other.M41
			    && M42 == other.M42
			    && M43 == other.M43
			    && M44 == other.M44;
		}

		// object
		public override bool Equals(object obj)
		{
			return obj is Matrix && Equals((Matrix)obj);
		}

		// object
		public override int GetHashCode()
		{
			unchecked
			{
				return M11.GetHashCode()
			     + M12.GetHashCode()
			     + M13.GetHashCode()
			     + M14.GetHashCode()
			     + M21.GetHashCode()
			     + M22.GetHashCode()
			     + M23.GetHashCode()
			     + M24.GetHashCode()
			     + M31.GetHashCode()
			     + M32.GetHashCode()
			     + M33.GetHashCode()
			     + M34.GetHashCode()
			     + M41.GetHashCode()
			     + M42.GetHashCode()
			     + M43.GetHashCode()
			     + M44.GetHashCode();
			}
		}
		
		#endregion


		#region Vectors

		public Vector3 Right
		{
			get { return new Vector3(M11, M12, M13); }
			set { M11 = value.X; M12 = value.Y; M13 = value.Z; }
		}

		public Vector3 Left
		{
			get { return new Vector3(-M11, -M12, -M13); }
			set { M11 = -value.X; M12 = -value.Y; M13 = -value.Z; }
		}

		public Vector3 Up
		{
			get { return new Vector3(M21, M22, M23); }
			set { M21 = value.X; M22 = value.Y; M23 = value.Z; }
		}

		public Vector3 Down
		{
			get { return new Vector3(-M21, -M22, -M23); }
			set { M21 = -value.X; M22 = -value.Y; M23 = -value.Z; }
		}

		public Vector3 Forward
		{
			get { return new Vector3(-M31, -M32, -M33); }
			set { M31 = -value.X; M32 = -value.Y; M33 = -value.Z; }
		}

		public Vector3 Backward
		{
			get { return new Vector3(M31, M32, M33); }
			set { M31 = value.X; M32 = value.Y; M33 = value.Z; }
		}

		public Vector3 Translation
		{
			get { return new Vector3(M41, M42, M43); }
			set { M41 = value.X; M42 = value.Y; M43 = value.Z; }
		}

		#endregion


		#region Matrix Operations

		#region Add

		public static void Add(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
		{
			result.M11 = matrix1.M11 + matrix2.M11;
			result.M12 = matrix1.M12 + matrix2.M12;
			result.M13 = matrix1.M13 + matrix2.M13;
			result.M14 = matrix1.M14 + matrix2.M14;
			result.M21 = matrix1.M21 + matrix2.M21;
			result.M22 = matrix1.M22 + matrix2.M22;
			result.M23 = matrix1.M23 + matrix2.M23;
			result.M24 = matrix1.M24 + matrix2.M24;
			result.M31 = matrix1.M31 + matrix2.M31;
			result.M32 = matrix1.M32 + matrix2.M32;
			result.M33 = matrix1.M33 + matrix2.M33;
			result.M34 = matrix1.M34 + matrix2.M34;
			result.M41 = matrix1.M41 + matrix2.M41;
			result.M42 = matrix1.M42 + matrix2.M42;
			result.M43 = matrix1.M43 + matrix2.M43;
			result.M44 = matrix1.M44 + matrix2.M44;
		}

		public static Matrix Add(Matrix matrix1, Matrix matrix2)
		{
			Matrix result;
			result.M11 = matrix1.M11 + matrix2.M11;
			result.M12 = matrix1.M12 + matrix2.M12;
			result.M13 = matrix1.M13 + matrix2.M13;
			result.M14 = matrix1.M14 + matrix2.M14;
			result.M21 = matrix1.M21 + matrix2.M21;
			result.M22 = matrix1.M22 + matrix2.M22;
			result.M23 = matrix1.M23 + matrix2.M23;
			result.M24 = matrix1.M24 + matrix2.M24;
			result.M31 = matrix1.M31 + matrix2.M31;
			result.M32 = matrix1.M32 + matrix2.M32;
			result.M33 = matrix1.M33 + matrix2.M33;
			result.M34 = matrix1.M34 + matrix2.M34;
			result.M41 = matrix1.M41 + matrix2.M41;
			result.M42 = matrix1.M42 + matrix2.M42;
			result.M43 = matrix1.M43 + matrix2.M43;
			result.M44 = matrix1.M44 + matrix2.M44;
			return result;
		}

		public static Matrix operator +(Matrix matrix1, Matrix matrix2)
		{
			Matrix result;
			result.M11 = matrix1.M11 + matrix2.M11;
			result.M12 = matrix1.M12 + matrix2.M12;
			result.M13 = matrix1.M13 + matrix2.M13;
			result.M14 = matrix1.M14 + matrix2.M14;
			result.M21 = matrix1.M21 + matrix2.M21;
			result.M22 = matrix1.M22 + matrix2.M22;
			result.M23 = matrix1.M23 + matrix2.M23;
			result.M24 = matrix1.M24 + matrix2.M24;
			result.M31 = matrix1.M31 + matrix2.M31;
			result.M32 = matrix1.M32 + matrix2.M32;
			result.M33 = matrix1.M33 + matrix2.M33;
			result.M34 = matrix1.M34 + matrix2.M34;
			result.M41 = matrix1.M41 + matrix2.M41;
			result.M42 = matrix1.M42 + matrix2.M42;
			result.M43 = matrix1.M43 + matrix2.M43;
			result.M44 = matrix1.M44 + matrix2.M44;
			return result;
		}

		#endregion

		#region Subtract

		public static void Subtract(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
		{
			result.M11 = matrix1.M11 - matrix2.M11;
			result.M12 = matrix1.M12 - matrix2.M12;
			result.M13 = matrix1.M13 - matrix2.M13;
			result.M14 = matrix1.M14 - matrix2.M14;
			result.M21 = matrix1.M21 - matrix2.M21;
			result.M22 = matrix1.M22 - matrix2.M22;
			result.M23 = matrix1.M23 - matrix2.M23;
			result.M24 = matrix1.M24 - matrix2.M24;
			result.M31 = matrix1.M31 - matrix2.M31;
			result.M32 = matrix1.M32 - matrix2.M32;
			result.M33 = matrix1.M33 - matrix2.M33;
			result.M34 = matrix1.M34 - matrix2.M34;
			result.M41 = matrix1.M41 - matrix2.M41;
			result.M42 = matrix1.M42 - matrix2.M42;
			result.M43 = matrix1.M43 - matrix2.M43;
			result.M44 = matrix1.M44 - matrix2.M44;
		}

		public static Matrix Subtract(Matrix matrix1, Matrix matrix2)
		{
			Matrix result;
			result.M11 = matrix1.M11 - matrix2.M11;
			result.M12 = matrix1.M12 - matrix2.M12;
			result.M13 = matrix1.M13 - matrix2.M13;
			result.M14 = matrix1.M14 - matrix2.M14;
			result.M21 = matrix1.M21 - matrix2.M21;
			result.M22 = matrix1.M22 - matrix2.M22;
			result.M23 = matrix1.M23 - matrix2.M23;
			result.M24 = matrix1.M24 - matrix2.M24;
			result.M31 = matrix1.M31 - matrix2.M31;
			result.M32 = matrix1.M32 - matrix2.M32;
			result.M33 = matrix1.M33 - matrix2.M33;
			result.M34 = matrix1.M34 - matrix2.M34;
			result.M41 = matrix1.M41 - matrix2.M41;
			result.M42 = matrix1.M42 - matrix2.M42;
			result.M43 = matrix1.M43 - matrix2.M43;
			result.M44 = matrix1.M44 - matrix2.M44;
			return result;
		}

		public static Matrix operator -(Matrix matrix1, Matrix matrix2)
		{
			Matrix result;
			result.M11 = matrix1.M11 - matrix2.M11;
			result.M12 = matrix1.M12 - matrix2.M12;
			result.M13 = matrix1.M13 - matrix2.M13;
			result.M14 = matrix1.M14 - matrix2.M14;
			result.M21 = matrix1.M21 - matrix2.M21;
			result.M22 = matrix1.M22 - matrix2.M22;
			result.M23 = matrix1.M23 - matrix2.M23;
			result.M24 = matrix1.M24 - matrix2.M24;
			result.M31 = matrix1.M31 - matrix2.M31;
			result.M32 = matrix1.M32 - matrix2.M32;
			result.M33 = matrix1.M33 - matrix2.M33;
			result.M34 = matrix1.M34 - matrix2.M34;
			result.M41 = matrix1.M41 - matrix2.M41;
			result.M42 = matrix1.M42 - matrix2.M42;
			result.M43 = matrix1.M43 - matrix2.M43;
			result.M44 = matrix1.M44 - matrix2.M44;
			return result;
		}

		#endregion

		#region Negate

		public static void Negate(ref Matrix matrix, out Matrix result)
		{
			result.M11 = -matrix.M11;
			result.M12 = -matrix.M12;
			result.M13 = -matrix.M13;
			result.M14 = -matrix.M14;
			result.M21 = -matrix.M21;
			result.M22 = -matrix.M22;
			result.M23 = -matrix.M23;
			result.M24 = -matrix.M24;
			result.M31 = -matrix.M31;
			result.M32 = -matrix.M32;
			result.M33 = -matrix.M33;
			result.M34 = -matrix.M34;
			result.M41 = -matrix.M41;
			result.M42 = -matrix.M42;
			result.M43 = -matrix.M43;
			result.M44 = -matrix.M44;
		}

		public static Matrix Negate(Matrix matrix)
		{
			Matrix result;
			result.M11 = -matrix.M11;
			result.M12 = -matrix.M12;
			result.M13 = -matrix.M13;
			result.M14 = -matrix.M14;
			result.M21 = -matrix.M21;
			result.M22 = -matrix.M22;
			result.M23 = -matrix.M23;
			result.M24 = -matrix.M24;
			result.M31 = -matrix.M31;
			result.M32 = -matrix.M32;
			result.M33 = -matrix.M33;
			result.M34 = -matrix.M34;
			result.M41 = -matrix.M41;
			result.M42 = -matrix.M42;
			result.M43 = -matrix.M43;
			result.M44 = -matrix.M44;
			return result;
		}

		public static Matrix operator -(Matrix matrix)
		{
			Matrix result;
			result.M11 = -matrix.M11;
			result.M12 = -matrix.M12;
			result.M13 = -matrix.M13;
			result.M14 = -matrix.M14;
			result.M21 = -matrix.M21;
			result.M22 = -matrix.M22;
			result.M23 = -matrix.M23;
			result.M24 = -matrix.M24;
			result.M31 = -matrix.M31;
			result.M32 = -matrix.M32;
			result.M33 = -matrix.M33;
			result.M34 = -matrix.M34;
			result.M41 = -matrix.M41;
			result.M42 = -matrix.M42;
			result.M43 = -matrix.M43;
			result.M44 = -matrix.M44;
			return result;
		}

		#endregion

		#region Multiply

		public static void Multiply(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
		{
			Matrix r;

			r.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
			r.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
			r.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
			r.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;

			r.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
			r.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
			r.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
			r.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;

			r.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
			r.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
			r.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
			r.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;

			r.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
			r.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
			r.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
			r.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;

			result = r;
		}

		public static Matrix operator *(Matrix matrix1, Matrix matrix2)
		{
			Matrix r;

			r.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
			r.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
			r.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
			r.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;

			r.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
			r.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
			r.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
			r.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;

			r.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
			r.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
			r.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
			r.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;

			r.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
			r.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
			r.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
			r.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;

			return r;
		}

		#endregion

		#region Scalar Multiply

		public static Matrix Multiply(Matrix matrix, float scaleFactor)
		{
			matrix.M11 *= scaleFactor;
			matrix.M12 *= scaleFactor;
			matrix.M13 *= scaleFactor;
			matrix.M14 *= scaleFactor;
			matrix.M21 *= scaleFactor;
			matrix.M22 *= scaleFactor;
			matrix.M23 *= scaleFactor;
			matrix.M24 *= scaleFactor;
			matrix.M31 *= scaleFactor;
			matrix.M32 *= scaleFactor;
			matrix.M33 *= scaleFactor;
			matrix.M34 *= scaleFactor;
			matrix.M41 *= scaleFactor;
			matrix.M42 *= scaleFactor;
			matrix.M43 *= scaleFactor;
			matrix.M44 *= scaleFactor;
			return matrix;
		}

		public static void Multiply(ref Matrix matrix, float scaleFactor, out Matrix result)
		{
			result.M11 = matrix.M11 * scaleFactor;
			result.M12 = matrix.M12 * scaleFactor;
			result.M13 = matrix.M13 * scaleFactor;
			result.M14 = matrix.M14 * scaleFactor;
			result.M21 = matrix.M21 * scaleFactor;
			result.M22 = matrix.M22 * scaleFactor;
			result.M23 = matrix.M23 * scaleFactor;
			result.M24 = matrix.M24 * scaleFactor;
			result.M31 = matrix.M31 * scaleFactor;
			result.M32 = matrix.M32 * scaleFactor;
			result.M33 = matrix.M33 * scaleFactor;
			result.M34 = matrix.M34 * scaleFactor;
			result.M41 = matrix.M41 * scaleFactor;
			result.M42 = matrix.M42 * scaleFactor;
			result.M43 = matrix.M43 * scaleFactor;
			result.M44 = matrix.M44 * scaleFactor;
		}

		public static Matrix operator *(Matrix matrix, float scaleFactor)
		{
			Matrix result;
			result.M11 = matrix.M11 * scaleFactor;
			result.M12 = matrix.M12 * scaleFactor;
			result.M13 = matrix.M13 * scaleFactor;
			result.M14 = matrix.M14 * scaleFactor;
			result.M21 = matrix.M21 * scaleFactor;
			result.M22 = matrix.M22 * scaleFactor;
			result.M23 = matrix.M23 * scaleFactor;
			result.M24 = matrix.M24 * scaleFactor;
			result.M31 = matrix.M31 * scaleFactor;
			result.M32 = matrix.M32 * scaleFactor;
			result.M33 = matrix.M33 * scaleFactor;
			result.M34 = matrix.M34 * scaleFactor;
			result.M41 = matrix.M41 * scaleFactor;
			result.M42 = matrix.M42 * scaleFactor;
			result.M43 = matrix.M43 * scaleFactor;
			result.M44 = matrix.M44 * scaleFactor;
			return result;
		}

		public static Matrix operator *(float scaleFactor, Matrix matrix)
		{
			Matrix result;
			result.M11 = matrix.M11 * scaleFactor;
			result.M12 = matrix.M12 * scaleFactor;
			result.M13 = matrix.M13 * scaleFactor;
			result.M14 = matrix.M14 * scaleFactor;
			result.M21 = matrix.M21 * scaleFactor;
			result.M22 = matrix.M22 * scaleFactor;
			result.M23 = matrix.M23 * scaleFactor;
			result.M24 = matrix.M24 * scaleFactor;
			result.M31 = matrix.M31 * scaleFactor;
			result.M32 = matrix.M32 * scaleFactor;
			result.M33 = matrix.M33 * scaleFactor;
			result.M34 = matrix.M34 * scaleFactor;
			result.M41 = matrix.M41 * scaleFactor;
			result.M42 = matrix.M42 * scaleFactor;
			result.M43 = matrix.M43 * scaleFactor;
			result.M44 = matrix.M44 * scaleFactor;
			return result;
		}

		#endregion

		#region Divide

		public static void Divide(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
		{
			result.M11 = matrix1.M11 / matrix2.M11;
			result.M12 = matrix1.M12 / matrix2.M12;
			result.M13 = matrix1.M13 / matrix2.M13;
			result.M14 = matrix1.M14 / matrix2.M14;
			result.M21 = matrix1.M21 / matrix2.M21;
			result.M22 = matrix1.M22 / matrix2.M22;
			result.M23 = matrix1.M23 / matrix2.M23;
			result.M24 = matrix1.M24 / matrix2.M24;
			result.M31 = matrix1.M31 / matrix2.M31;
			result.M32 = matrix1.M32 / matrix2.M32;
			result.M33 = matrix1.M33 / matrix2.M33;
			result.M34 = matrix1.M34 / matrix2.M34;
			result.M41 = matrix1.M41 / matrix2.M41;
			result.M42 = matrix1.M42 / matrix2.M42;
			result.M43 = matrix1.M43 / matrix2.M43;
			result.M44 = matrix1.M44 / matrix2.M44;
		}

		public static Matrix Divide(Matrix matrix1, Matrix matrix2)
		{
			Matrix result;
			result.M11 = matrix1.M11 / matrix2.M11;
			result.M12 = matrix1.M12 / matrix2.M12;
			result.M13 = matrix1.M13 / matrix2.M13;
			result.M14 = matrix1.M14 / matrix2.M14;
			result.M21 = matrix1.M21 / matrix2.M21;
			result.M22 = matrix1.M22 / matrix2.M22;
			result.M23 = matrix1.M23 / matrix2.M23;
			result.M24 = matrix1.M24 / matrix2.M24;
			result.M31 = matrix1.M31 / matrix2.M31;
			result.M32 = matrix1.M32 / matrix2.M32;
			result.M33 = matrix1.M33 / matrix2.M33;
			result.M34 = matrix1.M34 / matrix2.M34;
			result.M41 = matrix1.M41 / matrix2.M41;
			result.M42 = matrix1.M42 / matrix2.M42;
			result.M43 = matrix1.M43 / matrix2.M43;
			result.M44 = matrix1.M44 / matrix2.M44;
			return result;
		}

		public static Matrix operator /(Matrix matrix1, Matrix matrix2)
		{
			Matrix result;
			result.M11 = matrix1.M11 / matrix2.M11;
			result.M12 = matrix1.M12 / matrix2.M12;
			result.M13 = matrix1.M13 / matrix2.M13;
			result.M14 = matrix1.M14 / matrix2.M14;
			result.M21 = matrix1.M21 / matrix2.M21;
			result.M22 = matrix1.M22 / matrix2.M22;
			result.M23 = matrix1.M23 / matrix2.M23;
			result.M24 = matrix1.M24 / matrix2.M24;
			result.M31 = matrix1.M31 / matrix2.M31;
			result.M32 = matrix1.M32 / matrix2.M32;
			result.M33 = matrix1.M33 / matrix2.M33;
			result.M34 = matrix1.M34 / matrix2.M34;
			result.M41 = matrix1.M41 / matrix2.M41;
			result.M42 = matrix1.M42 / matrix2.M42;
			result.M43 = matrix1.M43 / matrix2.M43;
			result.M44 = matrix1.M44 / matrix2.M44;
			return result;
		}

		#endregion

		#region Scalar Divide

		public static Matrix Divide(Matrix matrix, float divider)
		{
			matrix.M11 /= divider;
			matrix.M12 /= divider;
			matrix.M13 /= divider;
			matrix.M14 /= divider;
			matrix.M21 /= divider;
			matrix.M22 /= divider;
			matrix.M23 /= divider;
			matrix.M24 /= divider;
			matrix.M31 /= divider;
			matrix.M32 /= divider;
			matrix.M33 /= divider;
			matrix.M34 /= divider;
			matrix.M41 /= divider;
			matrix.M42 /= divider;
			matrix.M43 /= divider;
			matrix.M44 /= divider;
			return matrix;
		}

		public static void Divide(ref Matrix matrix, float divider, out Matrix result)
		{
			result.M11 = matrix.M11 / divider;
			result.M12 = matrix.M12 / divider;
			result.M13 = matrix.M13 / divider;
			result.M14 = matrix.M14 / divider;
			result.M21 = matrix.M21 / divider;
			result.M22 = matrix.M22 / divider;
			result.M23 = matrix.M23 / divider;
			result.M24 = matrix.M24 / divider;
			result.M31 = matrix.M31 / divider;
			result.M32 = matrix.M32 / divider;
			result.M33 = matrix.M33 / divider;
			result.M34 = matrix.M34 / divider;
			result.M41 = matrix.M41 / divider;
			result.M42 = matrix.M42 / divider;
			result.M43 = matrix.M43 / divider;
			result.M44 = matrix.M44 / divider;
		}

		public static Matrix operator /(Matrix matrix, float divider)
		{
			Matrix result;
			result.M11 = matrix.M11 / divider;
			result.M12 = matrix.M12 / divider;
			result.M13 = matrix.M13 / divider;
			result.M14 = matrix.M14 / divider;
			result.M21 = matrix.M21 / divider;
			result.M22 = matrix.M22 / divider;
			result.M23 = matrix.M23 / divider;
			result.M24 = matrix.M24 / divider;
			result.M31 = matrix.M31 / divider;
			result.M32 = matrix.M32 / divider;
			result.M33 = matrix.M33 / divider;
			result.M34 = matrix.M34 / divider;
			result.M41 = matrix.M41 / divider;
			result.M42 = matrix.M42 / divider;
			result.M43 = matrix.M43 / divider;
			result.M44 = matrix.M44 / divider;
			return result;
		}

		#endregion

		#region Invert

		public static Matrix Invert(Matrix m)
		{
			Matrix result;
			Invert(ref m, out result);
			return result;
		}

		public static void Invert(ref Matrix matrix, out Matrix result)
		{
			//
			// Use Laplace expansion theorem to calculate the inverse of a 4x4 matrix 
			//
			// 1. Calculate the 2x2 determinants needed and the 4x4 determinant based on the 2x2 determinants  
			// 3. Create the adjugate matrix, which satisfies: A * adj(A) = det(A) * I 
			// 4. Divide adjugate matrix with the determinant to find the inverse 
			float det1 = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
			float det2 = matrix.M11 * matrix.M23 - matrix.M13 * matrix.M21;
			float det3 = matrix.M11 * matrix.M24 - matrix.M14 * matrix.M21;
			float det4 = matrix.M12 * matrix.M23 - matrix.M13 * matrix.M22;
			float det5 = matrix.M12 * matrix.M24 - matrix.M14 * matrix.M22;
			float det6 = matrix.M13 * matrix.M24 - matrix.M14 * matrix.M23;
			float det7 = matrix.M31 * matrix.M42 - matrix.M32 * matrix.M41;
			float det8 = matrix.M31 * matrix.M43 - matrix.M33 * matrix.M41;
			float det9 = matrix.M31 * matrix.M44 - matrix.M34 * matrix.M41;
			float det10 = matrix.M32 * matrix.M43 - matrix.M33 * matrix.M42;
			float det11 = matrix.M32 * matrix.M44 - matrix.M34 * matrix.M42;
			float det12 = matrix.M33 * matrix.M44 - matrix.M34 * matrix.M43;

			float detMatrix = (float)(det1 * det12 - det2 * det11 + det3 * det10 + det4 * det9 - det5 * det8 + det6 * det7);

			float invDetMatrix = 1f / detMatrix;

			Matrix ret; // Allow for matrix and result to point to the same structure 

			ret.M11 = (matrix.M22 * det12 - matrix.M23 * det11 + matrix.M24 * det10) * invDetMatrix;
			ret.M12 = (-matrix.M12 * det12 + matrix.M13 * det11 - matrix.M14 * det10) * invDetMatrix;
			ret.M13 = (matrix.M42 * det6 - matrix.M43 * det5 + matrix.M44 * det4) * invDetMatrix;
			ret.M14 = (-matrix.M32 * det6 + matrix.M33 * det5 - matrix.M34 * det4) * invDetMatrix;
			ret.M21 = (-matrix.M21 * det12 + matrix.M23 * det9 - matrix.M24 * det8) * invDetMatrix;
			ret.M22 = (matrix.M11 * det12 - matrix.M13 * det9 + matrix.M14 * det8) * invDetMatrix;
			ret.M23 = (-matrix.M41 * det6 + matrix.M43 * det3 - matrix.M44 * det2) * invDetMatrix;
			ret.M24 = (matrix.M31 * det6 - matrix.M33 * det3 + matrix.M34 * det2) * invDetMatrix;
			ret.M31 = (matrix.M21 * det11 - matrix.M22 * det9 + matrix.M24 * det7) * invDetMatrix;
			ret.M32 = (-matrix.M11 * det11 + matrix.M12 * det9 - matrix.M14 * det7) * invDetMatrix;
			ret.M33 = (matrix.M41 * det5 - matrix.M42 * det3 + matrix.M44 * det1) * invDetMatrix;
			ret.M34 = (-matrix.M31 * det5 + matrix.M32 * det3 - matrix.M34 * det1) * invDetMatrix;
			ret.M41 = (-matrix.M21 * det10 + matrix.M22 * det8 - matrix.M23 * det7) * invDetMatrix;
			ret.M42 = (matrix.M11 * det10 - matrix.M12 * det8 + matrix.M13 * det7) * invDetMatrix;
			ret.M43 = (-matrix.M41 * det4 + matrix.M42 * det2 - matrix.M43 * det1) * invDetMatrix;
			ret.M44 = (matrix.M31 * det4 - matrix.M32 * det2 + matrix.M33 * det1) * invDetMatrix;

			result = ret;
		}

		#endregion

		#region Transpose

		public static Matrix Transpose(Matrix matrix)
		{
			Matrix r;
			r.M11 = matrix.M11;
			r.M12 = matrix.M21;
			r.M13 = matrix.M31;
			r.M14 = matrix.M41;
			r.M21 = matrix.M12;
			r.M22 = matrix.M22;
			r.M23 = matrix.M32;
			r.M24 = matrix.M42;
			r.M31 = matrix.M13;
			r.M32 = matrix.M23;
			r.M33 = matrix.M33;
			r.M34 = matrix.M43;
			r.M41 = matrix.M14;
			r.M42 = matrix.M24;
			r.M43 = matrix.M34;
			r.M44 = matrix.M44;
			return r;
		}

		public static void Transpose(ref Matrix matrix, out Matrix result)
		{
			Matrix r;
			r.M11 = matrix.M11;
			r.M12 = matrix.M21;
			r.M13 = matrix.M31;
			r.M14 = matrix.M41;
			r.M21 = matrix.M12;
			r.M22 = matrix.M22;
			r.M23 = matrix.M32;
			r.M24 = matrix.M42;
			r.M31 = matrix.M13;
			r.M32 = matrix.M23;
			r.M33 = matrix.M33;
			r.M34 = matrix.M43;
			r.M41 = matrix.M14;
			r.M42 = matrix.M24;
			r.M43 = matrix.M34;
			r.M44 = matrix.M44;
			result = r;
		}

		#endregion

		#endregion


		#region Create Matrices

		/* IDENTITY TEMPLATE:
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		*/

		#region Scale

		public static void CreateScale(ref Vector3 scales, out Matrix result)
		{
			result.M11 = scales.X;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = scales.Y;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = scales.Z;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		}

		public static Matrix CreateScale(Vector3 scales)
		{
			Matrix result;
			result.M11 = scales.X;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = scales.Y;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = scales.Z;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
			return result;
		}

		public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result)
		{
			result.M11 = xScale;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = yScale;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = zScale;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		}

		public static Matrix CreateScale(float xScale, float yScale, float zScale)
		{
			Matrix result;
			result.M11 = xScale;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = yScale;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = zScale;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
			return result;
		}

		public static void CreateScale(float scale, out Matrix result)
		{
			result.M11 = scale;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = scale;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = scale;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		}

		public static Matrix CreateScale(float scale)
		{
			Matrix result;
			result.M11 = scale;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = scale;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = scale;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
			return result;
		}

		#endregion

		#region Rotation

		public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
		{
			Matrix matrix;
			float x = axis.X;
			float y = axis.Y;
			float z = axis.Z;
			float num2 = (float)Math.Sin((double)angle);
			float num = (float)Math.Cos((double)angle);
			float num11 = x * x;
			float num10 = y * y;
			float num9 = z * z;
			float num8 = x * y;
			float num7 = x * z;
			float num6 = y * z;
			matrix.M11 = num11 + (num * (1f - num11));
			matrix.M12 = (num8 - (num * num8)) + (num2 * z);
			matrix.M13 = (num7 - (num * num7)) - (num2 * y);
			matrix.M14 = 0f;
			matrix.M21 = (num8 - (num * num8)) - (num2 * z);
			matrix.M22 = num10 + (num * (1f - num10));
			matrix.M23 = (num6 - (num * num6)) + (num2 * x);
			matrix.M24 = 0f;
			matrix.M31 = (num7 - (num * num7)) + (num2 * y);
			matrix.M32 = (num6 - (num * num6)) - (num2 * x);
			matrix.M33 = num9 + (num * (1f - num9));
			matrix.M34 = 0f;
			matrix.M41 = 0f;
			matrix.M42 = 0f;
			matrix.M43 = 0f;
			matrix.M44 = 1f;
			return matrix;
		}

		public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result)
		{
			float x = axis.X;
			float y = axis.Y;
			float z = axis.Z;
			float num2 = (float)Math.Sin((double)angle);
			float num = (float)Math.Cos((double)angle);
			float num11 = x * x;
			float num10 = y * y;
			float num9 = z * z;
			float num8 = x * y;
			float num7 = x * z;
			float num6 = y * z;
			result.M11 = num11 + (num * (1f - num11));
			result.M12 = (num8 - (num * num8)) + (num2 * z);
			result.M13 = (num7 - (num * num7)) - (num2 * y);
			result.M14 = 0f;
			result.M21 = (num8 - (num * num8)) - (num2 * z);
			result.M22 = num10 + (num * (1f - num10));
			result.M23 = (num6 - (num * num6)) + (num2 * x);
			result.M24 = 0f;
			result.M31 = (num7 - (num * num7)) + (num2 * y);
			result.M32 = (num6 - (num * num6)) - (num2 * x);
			result.M33 = num9 + (num * (1f - num9));
			result.M34 = 0f;
			result.M41 = 0f;
			result.M42 = 0f;
			result.M43 = 0f;
			result.M44 = 1f;
		}



		public static void CreateRotationX(float radians, out Matrix result)
		{
			float sin = (float)Math.Sin((double)radians);
			float cos = (float)Math.Cos((double)radians);
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = cos;
			result.M23 = sin;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = -sin;
			result.M33 = cos;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		}

		public static Matrix CreateRotationX(float radians)
		{
			Matrix result;
			float sin = (float)Math.Sin((double)radians);
			float cos = (float)Math.Cos((double)radians);
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = cos;
			result.M23 = sin;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = -sin;
			result.M33 = cos;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
			return result;
		}


		public static void CreateRotationY(float radians, out Matrix result)
		{
			float sin = (float)Math.Sin((double)radians);
			float cos = (float)Math.Cos((double)radians);
			result.M11 = cos;
			result.M12 = 0;
			result.M13 = -sin;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = sin;
			result.M32 = 0;
			result.M33 = cos;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		}

		public static Matrix CreateRotationY(float radians)
		{
			Matrix result;
			float sin = (float)Math.Sin((double)radians);
			float cos = (float)Math.Cos((double)radians);
			result.M11 = cos;
			result.M12 = 0;
			result.M13 = -sin;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = sin;
			result.M32 = 0;
			result.M33 = cos;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
			return result;
		}


		public static void CreateRotationZ(float radians, out Matrix result)
		{
			float sin = (float)Math.Sin((double)radians);
			float cos = (float)Math.Cos((double)radians);
			result.M11 = cos;
			result.M12 = sin;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = -sin;
			result.M22 = cos;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
		}

		public static Matrix CreateRotationZ(float radians)
		{
			Matrix result;
			float sin = (float)Math.Sin((double)radians);
			float cos = (float)Math.Cos((double)radians);
			result.M11 = cos;
			result.M12 = sin;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = -sin;
			result.M22 = cos;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = 0;
			result.M42 = 0;
			result.M43 = 0;
			result.M44 = 1;
			return result;
		}

		#endregion

		#region Translation

		public static void CreateTranslation(ref Vector3 position, out Matrix result)
		{
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = position.X;
			result.M42 = position.Y;
			result.M43 = position.Z;
			result.M44 = 1;
		}

		public static Matrix CreateTranslation(Vector3 position)
		{
			Matrix result;
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = position.X;
			result.M42 = position.Y;
			result.M43 = position.Z;
			result.M44 = 1;
			return result;
		}

		public static void CreateTranslation(float xPosition, float yPosition, float zPosition, out Matrix result)
		{
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = xPosition;
			result.M42 = yPosition;
			result.M43 = zPosition;
			result.M44 = 1;
		}

		public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition)
		{
			Matrix result;
			result.M11 = 1;
			result.M12 = 0;
			result.M13 = 0;
			result.M14 = 0;
			result.M21 = 0;
			result.M22 = 1;
			result.M23 = 0;
			result.M24 = 0;
			result.M31 = 0;
			result.M32 = 0;
			result.M33 = 1;
			result.M34 = 0;
			result.M41 = xPosition;
			result.M42 = yPosition;
			result.M43 = zPosition;
			result.M44 = 1;
			return result;
		}
	
		#endregion

		#region Orthographic Projection

		public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
		{
			Matrix output;
			CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane, out output);
			return output;
		}

		public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top,
				float zNearPlane, float zFarPlane, out Matrix result)
		{
			// From <http://www.codeguru.com/cpp/misc/misc/math/article.php/c10123__2/>
			result.M11 = 2f / (right - left);
			result.M22 = 2f / (top - bottom);
			result.M33 = 1f / (zNearPlane - zFarPlane);

			result.M12 = result.M13 = result.M14 = 0f;
			result.M21 = result.M23 = result.M24 = 0f;
			result.M31 = result.M32 = result.M34 = 0f;

			result.M41 = -((right+left) / (right-left));
			result.M42 = -((top+bottom) / (top-bottom));
			result.M43 = -(zNearPlane / (zFarPlane-zNearPlane));
			result.M44 = 1f;
		}

		#endregion

		#endregion

	}
}
