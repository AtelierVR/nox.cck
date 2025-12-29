using System;
using UnityEngine;

namespace Nox.CCK.Utils {
	public class Buffer {
		public byte[] Data;
		public ushort Offset;
		public ushort Length;

		public int Remaining
			=> Length - Offset;

		public Buffer(ushort offset = 0) {
			Clear();
			Offset = offset;
			Length = offset;
		}

		public bool Write(byte value) {
			if (Offset + 1 > Data.Length) return false;
			Data[Offset++] = value;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(short value) {
			if (Offset + 2 > Data.Length) return false;
			Data[Offset++] = (byte)(value >> 8);
			Data[Offset++] = (byte)value;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(ushort value)
			=> Write((short)value);

		public bool Write(string value) {
			if (Offset + value.Length + 2 > Data.Length) return false;
			if (!Write((ushort)value.Length)) return false;
			foreach (var c in value)
				Data[Offset++] = (byte)c;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(DateTimeOffset value)
			=> Write(value.ToUnixTimeMilliseconds());

		public bool Write(int value) {
			if (Offset + 4 > Data.Length) return false;
			Data[Offset++] = (byte)(value >> 24);
			Data[Offset++] = (byte)(value >> 16);
			Data[Offset++] = (byte)(value >> 8);
			Data[Offset++] = (byte)value;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(uint value)
			=> Write((int)value);

		public bool Write(long value) {
			if (Offset + 8 > Data.Length) return false;
			Data[Offset++] = (byte)(value >> 56);
			Data[Offset++] = (byte)(value >> 48);
			Data[Offset++] = (byte)(value >> 40);
			Data[Offset++] = (byte)(value >> 32);
			Data[Offset++] = (byte)(value >> 24);
			Data[Offset++] = (byte)(value >> 16);
			Data[Offset++] = (byte)(value >> 8);
			Data[Offset++] = (byte)value;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(ulong value)
			=> Write((long)value);

		public bool Write(float value) {
			if (Offset + 4 > Data.Length) return false;
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			foreach (var b in bytes)
				Data[Offset++] = b;
			if (Offset > Length) Length = Offset;
			return true;
		}


		public bool Write(double value) {
			if (Offset + 8 > Data.Length) return false;
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			foreach (var b in bytes)
				Data[Offset++] = b;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(Vector3 value) {
			if (Offset + 12 > Data.Length) return false;
			if (!Write(value.x)) return false;
			if (!Write(value.y)) return false;
			if (!Write(value.z)) return false;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(Quaternion value) {
			if (Offset + 16 > Data.Length) return false;
			if (!Write(value.x)) return false;
			if (!Write(value.y)) return false;
			if (!Write(value.z)) return false;
			if (!Write(value.w)) return false;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public bool Write(byte[] value) {
			if (Offset + value.Length > Data.Length) return false;
			foreach (var b in value)
				Data[Offset++] = b;
			if (Offset > Length) Length = Offset;
			return true;
		}

		public byte[] ToArray() {
			var buffer = new byte[Length];
			Array.Copy(Data, buffer, Length);
			return buffer;
		}

		public void Seek(ushort pos)
			=> Offset = pos;

		public void End()
			=> Offset = Length;

		public void Start()
			=> Offset = 0;

		public void Move(short move)
			=> Offset = (ushort)(Offset + move);

		public override string ToString() {
			var res = $"{GetType().Name}[(offset={Offset}, length={Length})";
			for (var i = 0; i < Length; i++)
				res += " " + Data[i].ToString("X2");
			return res + "]";
		}

		public byte ReadByte() {
			if (Offset + 1 > Length) return 0;
			return Data[Offset++];
		}

		public ushort ReadUShort() {
			if (Offset + 2 > Length) return 0;
			var value = (ushort)(Data[Offset++] << 8);
			value |= Data[Offset++];
			return value;
		}

		public string ReadString() {
			var length = ReadUShort();
			if (Offset + length > this.Length) return string.Empty;
			var value = System.Text.Encoding.UTF8.GetString(Data, Offset, length);
			Offset += length;
			return value;
		}

		public DateTime ReadDateTime() {
			var value = ReadLong();
			return DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
		}

		public int ReadInt() {
			if (Offset + 4 > Length) return 0;
			var value = Data[Offset++] << 24;
			value |= Data[Offset++] << 16;
			value |= Data[Offset++] << 8;
			value |= Data[Offset++];
			return value;
		}

		public long ReadLong() {
			if (Offset + 8 > Length) return 0;
			var value = (long)Data[Offset++] << 56;
			value |= (long)Data[Offset++] << 48;
			value |= (long)Data[Offset++] << 40;
			value |= (long)Data[Offset++] << 32;
			value |= (long)Data[Offset++] << 24;
			value |= (long)Data[Offset++] << 16;
			value |= (long)Data[Offset++] << 8;
			value |= Data[Offset++];
			return value;
		}

		public float ReadFloat() {
			if (Offset + 4 > Length) return 0;
			var bytes = new byte[4];
			for (var i = 0; i < 4; i++)
				bytes[i] = Data[Offset++];
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToSingle(bytes, 0);
		}

		public double ReadDouble() {
			if (Offset + 8 > Length) return 0;
			var bytes = new byte[8];
			for (var i = 0; i < 8; i++)
				bytes[i] = Data[Offset++];
			if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return BitConverter.ToDouble(bytes, 0);
		}

		public Vector3 ReadVector3() {
			if (Offset + 12 > Length) return Vector3.zero;
			var x = ReadFloat();
			var y = ReadFloat();
			var z = ReadFloat();
			return new Vector3(x, y, z);
		}

		public Quaternion ReadQuaternion() {
			if (Offset + 16 > Length) return Quaternion.identity;
			var x = ReadFloat();
			var y = ReadFloat();
			var z = ReadFloat();
			var w = ReadFloat();
			return new Quaternion(x, y, z, w);
		}

		public byte[] ReadBytes(ushort length) {
			if (Offset + length > this.Length) return Array.Empty<byte>();
			var value = new byte[length];
			for (var i = 0; i < length; i++)
				value[i] = Data[Offset++];
			return value;
		}

		public uint ReadUInt()
			=> (uint)ReadInt();

		public Buffer Clone(ushort start = 0, ushort end = 0) {
			if (end == 0) end = Length;
			var buffer        = new Buffer();
			for (var i = start; i < end; i++)
				buffer.Write(Data[i]);
			buffer.Start();
			return buffer;
		}

		public void Clear() {
			Offset = 0;
			Length = 0;
			Data   = new byte[1024];
		}

		public bool Write(Buffer buffer) {
			if (Offset + buffer.Length > Data.Length) return false;
			for (var i = 0; i < buffer.Length; i++)
				Data[Offset++] = buffer.Data[i];
			if (Offset > Length) Length = Offset;
			return true;
		}

		public T ReadEnum<T>() where T : Enum {
			var type = Enum.GetUnderlyingType(typeof(T));
			if (type == typeof(byte)) return (T)(object)ReadByte();
			if (type == typeof(ushort)) return (T)(object)ReadUShort();
			if (type == typeof(uint)) return (T)(object)ReadUInt();
			return default;
		}

		public bool Write(Enum value) {
			var type = Enum.GetUnderlyingType(value.GetType());
			if (type == typeof(byte)) return Write(Convert.ToByte(value));
			if (type == typeof(ushort)) return Write(Convert.ToUInt16(value));
			if (type == typeof(uint)) return Write(Convert.ToUInt32(value));
			return false;
		}

		public void Compact() {
			if (Offset == 0) return;
			var newLength = Length - Offset;
			var newData   = new byte[Data.Length];
			Array.Copy(Data, Offset, newData, 0, newLength);
			Data   = newData;
			Length = (ushort)newLength;
			Offset = 0;
		}
	}
}