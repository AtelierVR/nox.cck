namespace Nox.CCK.Utils {
	public static class Hash {
		public static int CRC32(string s)
			=> CRC32(System.Text.Encoding.UTF8.GetBytes(s));

		// The CRC tables are built once: they used to be rebuilt (256 iterations + a 1 KB / 2 KB
		// allocation) on every call, and CRC32 is called for every log line that carries a tag.
		private static readonly uint[] Crc32Table = CreateCrc32Table();
		private static readonly ulong[] Crc64Table = CreateCrc64Table();

		private static uint[] CreateCrc32Table() {
			unchecked {
				const uint polynomial = 0xEDB88320;
				var        table      = new uint[256];
				for (uint i = 0; i < table.Length; i++) {
					var crc = i;
					for (var j = 8; j > 0; j--)
						if ((crc & 1) == 1) {
							crc = (crc >> 1) ^ polynomial;
						} else crc >>= 1;

					table[i] = crc;
				}

				return table;
			}
		}

		private static ulong[] CreateCrc64Table() {
			unchecked {
				const ulong polynomial = 0xC96C5795D7870F42;
				var         table      = new ulong[256];
				for (ulong i = 0; i < (ulong)table.Length; i++) {
					var crc = i;
					for (var j = 8; j > 0; j--)
						if ((crc & 1) == 1) {
							crc = (crc >> 1) ^ polynomial;
						} else crc >>= 1;

					table[i] = crc;
				}

				return table;
			}
		}

		public static int CRC32(byte[] data) {
			unchecked {
				var table = Crc32Table;
				var hash  = 0xFFFFFFFF;
				foreach (var b in data) {
					var index = (byte)((hash & 0xFF) ^ b);
					hash = (hash >> 8) ^ table[index];
				}

				return (int)(hash ^ 0xFFFFFFFF);
			}
		}

		public static long CRC64(string s)
			=> CRC64(System.Text.Encoding.UTF8.GetBytes(s));

		public static long CRC64(byte[] data) {
			unchecked {
				var table = Crc64Table;
				var hash  = 0xFFFFFFFFFFFFFFFF;
				foreach (var b in data) {
					var index = (byte)((hash & 0xFF) ^ b);
					hash = (hash >> 8) ^ table[index];
				}

				return (long)(hash ^ 0xFFFFFFFFFFFFFFFF);
			}
		}
	}
}