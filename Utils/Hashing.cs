using System;
using System.Security.Cryptography;

namespace Nox.CCK.Utils {
	public static class Hashing {
		public static string Hash(string input) {
			using var sha = SHA256.Create();
			var bytes = System.Text.Encoding.UTF8.GetBytes(input);
			var hash = sha.ComputeHash(bytes);
			return BitConverter.ToString(hash).Replace("-", "").ToLower();
		}

		public static string HashFile(string path) {
			using var sha = SHA256.Create();
			using var stream = System.IO.File.OpenRead(path);
			var hash = sha.ComputeHash(stream);
			return BitConverter.ToString(hash).Replace("-", "").ToLower();
		}

		public static string HashBytes(byte[] data) {
			using var sha = SHA256.Create();
			var hash = sha.ComputeHash(data);
			return BitConverter.ToString(hash).Replace("-", "").ToLower();
		}
	}
}