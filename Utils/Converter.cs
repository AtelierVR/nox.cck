using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Nox.CCK {
	public static class Converter {
		private static short FromBigEndianInt16(byte[] data, int offset)
			=> (short)((data[offset] << 8) | data[offset + 1]);

		private static ushort FromBigEndianUInt16(byte[] data, int offset)
			=> (ushort)((data[offset] << 8) | data[offset + 1]);

		private static int FromBigEndianInt32(byte[] data, int offset)
			=> (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

		private static uint FromBigEndianUInt32(byte[] data, int offset)
			=> (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);

		private static long FromBigEndianInt64(byte[] data, int offset)
			=> ((long)data[offset] << 56) | ((long)data[offset + 1] << 48) | ((long)data[offset + 2] << 40) | ((long)data[offset + 3] << 32) | ((long)data[offset + 4] << 24) | ((long)data[offset + 5] << 16) | ((long)data[offset + 6] << 8) | data[offset + 7];

		private static ulong FromBigEndianUInt64(byte[] data, int offset)
			=> ((ulong)data[offset] << 56) | ((ulong)data[offset + 1] << 48) | ((ulong)data[offset + 2] << 40) | ((ulong)data[offset + 3] << 32) | ((ulong)data[offset + 4] << 24) | ((ulong)data[offset + 5] << 16) | ((ulong)data[offset + 6] << 8) | data[offset + 7];

		private static float FromBigEndianSingle(byte[] data, int offset) {
			var bytes = new byte[4];
			Array.Copy(data, offset, bytes, 0, 4);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToSingle(bytes, 0);
		}

		private static double FromBigEndianDouble(byte[] data, int offset) {
			var bytes = new byte[8];
			Array.Copy(data, offset, bytes, 0, 8);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToDouble(bytes, 0);
		}

		public static bool ToBool(this object value)
			=> value switch {
				bool b    => b,
				byte b    => b  != 0,
				short s   => s  != 0,
				ushort us => us != 0,
				long l    => l  != 0,
				ulong ul  => ul != 0,
				int i     => i  != 0,
				uint ui   => ui != 0,
				float f   => !Mathf.Approximately(f, 0f),
				double d  => !Mathf.Approximately((float)d, 0f),
				byte[] b  => b.Length > 0                     && b[0] != 0,
				string s  => bool.TryParse(s, out var result) && result,
				_         => false
			};

		public static byte ToByte(this object value)
			=> value switch {
				bool b    => b ? (byte)1 : (byte)0,
				byte b    => b,
				short s   => (byte)s,
				ushort us => (byte)us,
				long l    => (byte)l,
				ulong ul  => (byte)ul,
				int i     => (byte)i,
				uint ui   => (byte)ui,
				float f   => (byte)f,
				double d  => (byte)d,
				byte[] b  => b.Length >= 1 ? b[0] : (byte)0,
				string s  => byte.TryParse(s, out var result) ? result : (byte)0,
				_         => 0
			};

		public static short ToShort(this object value)
			=> value switch {
				bool b    => b ? (short)1 : (short)0,
				byte b    => b,
				short s   => s,
				ushort us => (short)us,
				long l    => (short)l,
				ulong ul  => (short)ul,
				int i     => (short)i,
				uint ui   => (short)ui,
				float f   => (short)f,
				double d  => (short)d,
				byte[] b  => b.Length >= 2 ? FromBigEndianInt16(b, 0) : (short)0,
				string s  => short.TryParse(s, out var result) ? result : (short)0,
				_         => 0
			};

		public static ushort ToUShort(this object value)
			=> value switch {
				bool b    => b ? (ushort)1 : (ushort)0,
				byte b    => b,
				short s   => (ushort)s,
				ushort us => us,
				long l    => (ushort)l,
				ulong ul  => (ushort)ul,
				int i     => (ushort)i,
				uint ui   => (ushort)ui,
				float f   => (ushort)f,
				double d  => (ushort)d,
				byte[] b  => b.Length >= 2 ? FromBigEndianUInt16(b, 0) : (ushort)0,
				string s  => ushort.TryParse(s, out var result) ? result : (ushort)0,
				_         => 0
			};

		public static int ToInt(this object value)
			=> value switch {
				bool b    => b ? 1 : 0,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => (int)l,
				ulong ul  => (int)ul,
				int i     => i,
				uint ui   => (int)ui,
				float f   => (int)f,
				double d  => (int)d,
				byte[] b  => b.Length >= 4 ? FromBigEndianInt32(b, 0) : 0,
				string s  => int.TryParse(s, out var result) ? result : 0,
				_         => 0
			};

		public static uint ToUInt(this object value)
			=> value switch {
				bool b    => b ? (uint)1 : (uint)0,
				byte b    => b,
				short s   => (uint)s,
				ushort us => us,
				long l    => (uint)l,
				ulong ul  => (uint)ul,
				int i     => (uint)i,
				uint ui   => ui,
				float f   => (uint)f,
				double d  => (uint)d,
				byte[] b  => b.Length >= 4 ? FromBigEndianUInt32(b, 0) : (uint)0,
				string s  => uint.TryParse(s, out var result) ? result : (uint)0,
				_         => 0
			};

		public static long ToLong(this object value)
			=> value switch {
				bool b    => b ? 1L : 0L,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => l,
				ulong ul  => (long)ul,
				int i     => i,
				uint ui   => ui,
				float f   => (long)f,
				double d  => (long)d,
				byte[] b  => b.Length >= 8 ? FromBigEndianInt64(b, 0) : 0L,
				string s  => long.TryParse(s, out var result) ? result : 0L,
				_         => 0L
			};

		public static ulong ToULong(this object value)
			=> value switch {
				bool b    => b ? 1UL : 0UL,
				byte b    => b,
				short s   => (ulong)s,
				ushort us => us,
				long l    => (ulong)l,
				ulong ul  => ul,
				int i     => (ulong)i,
				uint ui   => ui,
				float f   => (ulong)f,
				double d  => (ulong)d,
				byte[] b  => b.Length >= 8 ? FromBigEndianUInt64(b, 0) : 0UL,
				string s  => ulong.TryParse(s, out var result) ? result : 0UL,
				_         => 0UL
			};

		public static float ToFloat(this object value)
			=> value switch {
				bool b    => b ? 1f : 0f,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => l,
				ulong ul  => ul,
				int i     => i,
				uint ui   => ui,
				float f   => f,
				double d  => (float)d,
				byte[] b  => b.Length >= 4 ? FromBigEndianSingle(b, 0) : 0f,
				string s  => float.TryParse(s, out var result) ? result : 0f,
				_         => 0f
			};

		public static double ToDouble(this object value)
			=> value switch {
				bool b    => b ? 1.0 : 0.0,
				byte b    => b,
				short s   => s,
				ushort us => us,
				long l    => l,
				ulong ul  => ul,
				int i     => i,
				uint ui   => ui,
				float f   => f,
				double d  => d,
				byte[] b  => b.Length >= 8 ? FromBigEndianDouble(b, 0) : 0.0,
				string s  => double.TryParse(s, out var result) ? result : 0.0,
				_         => 0.0
			};

		public static string ToString(this object value)
			=> value switch {
				bool b    => b.ToString(),
				byte b    => b.ToString(),
				short s   => s.ToString(),
				ushort us => us.ToString(),
				long l    => l.ToString(),
				ulong ul  => ul.ToString(),
				int i     => i.ToString(),
				uint ui   => ui.ToString(),
				float f   => f.ToString(CultureInfo.InvariantCulture),
				double d  => d.ToString(CultureInfo.InvariantCulture),
				byte[] b  => Encoding.UTF8.GetString(b),
				string s  => s,
				_         => value?.ToString() ?? ""
			};

		public static Vector3 ToVector3(this object value)
			=> value switch {
				Vector3 v => v,
				byte[] { Length: 12 } b => new Vector3(
					FromBigEndianSingle(b, 0),
					FromBigEndianSingle(b, 4),
					FromBigEndianSingle(b, 8)
				),
				_ => Vector3.zero
			};

		public static Quaternion ToQuaternion(this object value)
			=> value switch {
				Quaternion q => q,
				byte[] { Length: 16 } b => new Quaternion(
					FromBigEndianSingle(b, 0),
					FromBigEndianSingle(b, 4),
					FromBigEndianSingle(b, 8),
					FromBigEndianSingle(b, 12)
				),
				_ => Quaternion.identity
			};

		// ===== Encodage big-endian vers byte[] (miroir des décodeurs ci-dessus) =====

		private static byte[] ToBigEndianBytes(float value) {
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		private static byte[] ToBigEndianBytes(double value) {
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		private static byte[] Combine(params byte[][] arrays) {
			var length = 0;
			foreach (var array in arrays) length += array.Length;

			var result = new byte[length];
			var offset = 0;
			foreach (var array in arrays) {
				Buffer.BlockCopy(array, 0, result, offset, array.Length);
				offset += array.Length;
			}

			return result;
		}

		public static byte[] ToBytes(this object value)
			=> value switch {
				string str   => Encoding.UTF8.GetBytes(str),
				byte[] bytes => bytes,
				bool b       => new[] { b ? (byte)1 : (byte)0 },
				byte b       => new[] { b },
				short s      => new[] { (byte)(s >> 8), (byte)s },
				ushort us    => new[] { (byte)(us >> 8), (byte)us },
				int i        => new[] {
					(byte)(i >> 24), (byte)(i >> 16),
					(byte)(i >> 8),  (byte)i
				},
				uint ui      => new[] {
					(byte)(ui >> 24), (byte)(ui >> 16),
					(byte)(ui >> 8),  (byte)ui
				},
				long l       => new[] {
					(byte)(l >> 56), (byte)(l >> 48),
					(byte)(l >> 40), (byte)(l >> 32),
					(byte)(l >> 24), (byte)(l >> 16),
					(byte)(l >> 8),  (byte)l
				},
				ulong ul     => new[] {
					(byte)(ul >> 56), (byte)(ul >> 48),
					(byte)(ul >> 40), (byte)(ul >> 32),
					(byte)(ul >> 24), (byte)(ul >> 16),
					(byte)(ul >> 8),  (byte)ul
				},
				float f      => ToBigEndianBytes(f),
				double d     => ToBigEndianBytes(d),
				Vector3 v    => Combine(
					ToBigEndianBytes(v.x), 
					ToBigEndianBytes(v.y), 
					ToBigEndianBytes(v.z)
				),
				Quaternion q => Combine(
					ToBigEndianBytes(q.x), 
					ToBigEndianBytes(q.y), 
					ToBigEndianBytes(q.z), 
					ToBigEndianBytes(q.w)
				),
				_            => Array.Empty<byte>()
			};

		/// <summary>
		/// Décode génériquement des octets big-endian vers le type demandé <typeparamref name="T"/>.
		/// </summary>
		public static T To<T>(this byte[] value)
			=> typeof(T) switch {
				Type t when t == typeof(bool)       => (T)(object)value.ToBool(),
				Type t when t == typeof(byte)       => (T)(object)value.ToByte(),
				Type t when t == typeof(short)      => (T)(object)value.ToShort(),
				Type t when t == typeof(ushort)     => (T)(object)value.ToUShort(),
				Type t when t == typeof(int)        => (T)(object)value.ToInt(),
				Type t when t == typeof(uint)       => (T)(object)value.ToUInt(),
				Type t when t == typeof(long)       => (T)(object)value.ToLong(),
				Type t when t == typeof(ulong)      => (T)(object)value.ToULong(),
				Type t when t == typeof(float)      => (T)(object)value.ToFloat(),
				Type t when t == typeof(double)     => (T)(object)value.ToDouble(),
				Type t when t == typeof(string)     => (T)(object)ToString(value),
				Type t when t == typeof(byte[])     => (T)(object)value,
				Type t when t == typeof(Vector3)    => (T)(object)value.ToVector3(),
				Type t when t == typeof(Quaternion) => (T)(object)value.ToQuaternion(),
				_ => throw new InvalidOperationException($"Unsupported type: {typeof(T)}")
			};
	}
}
