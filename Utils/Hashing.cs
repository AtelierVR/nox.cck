using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Hash helpers. The typeless overloads return a bare lowercase hexadecimal digest, while the
	/// typed overloads (<c>Hash(type, …)</c>) return the typed value <c>&lt;type&gt;:&lt;hex&gt;</c>
	/// used by the asset pipeline (e.g. <c>sha256:0123…</c>).
	/// </summary>
	public static class Hashing {
		/// <summary>Algorithm used by the typeless overloads and when no type is given.</summary>
		public const string DefaultType = "sha256";

		/// <summary>Read size used when a file is hashed with progress.</summary>
		private const int ChunkSize = 1024 * 1024;

		public static string Hash(string input) {
			using var sha = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(input);
			var hash = sha.ComputeHash(bytes);
			return BitConverter.ToString(hash).Replace("-", "").ToLower();
		}

		public static string HashFile(string path) {
			using var sha = SHA256.Create();
			using var stream = File.OpenRead(path);
			var hash = sha.ComputeHash(stream);
			return BitConverter.ToString(hash).Replace("-", "").ToLower();
		}

		public static string HashBytes(byte[] data) {
			using var sha = SHA256.Create();
			var hash = sha.ComputeHash(data);
			return BitConverter.ToString(hash).Replace("-", "").ToLower();
		}

		/// <summary>Hashes a string and returns <c>&lt;type&gt;:&lt;hex&gt;</c>.</summary>
		public static string Hash(string type, string input)
			=> Format(type, Hex(type, Encoding.UTF8.GetBytes(input)));

		/// <summary>Hashes a file and returns <c>&lt;type&gt;:&lt;hex&gt;</c>.</summary>
		public static string HashFile(string type, string path)
			=> HashFile(type, path, null);

		/// <summary>
		/// Hashes a file and reports how much of it has been read, in 0..1. The report is what makes
		/// a long hash (a gigabyte-scale bundle) visible instead of a frozen progress bar.
		/// </summary>
		public static string HashFile(string type, string path, Action<float> onProgress) {
			using var algorithm = Create(type);
			using var stream    = File.OpenRead(path);

			var length = stream.Length;
			var buffer = new byte[ChunkSize];
			var read   = 0L;
			int count;

			while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
				algorithm.TransformBlock(buffer, 0, count, null, 0);
				read += count;
				onProgress?.Invoke(length > 0 ? (float)((double)read / length) : 1f);
			}

			algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			return Format(type, ToHex(algorithm.Hash));
		}

		/// <summary>
		/// Hashes a file off the main thread while reporting progress: <paramref name="onProgress"/> is
		/// called on the caller's thread (usually the main one), so it can drive a UI directly.
		/// </summary>
		public static async UniTask<string> HashFileAsync(
			string type,
			string path,
			Action<float> onProgress = null,
			CancellationToken token = default
		) {
			var progress = 0f;
			var finished = 0;

			var hashing = UniTask.RunOnThreadPool(
				() => {
					try {
						return HashFile(type, path, ratio => progress = ratio);
					} finally {
						Volatile.Write(ref finished, 1);
					}
				},
				cancellationToken: token
			);

			while (Volatile.Read(ref finished) == 0) {
				onProgress?.Invoke(progress);
				await UniTask.Yield();
			}

			var hash = await hashing;
			onProgress?.Invoke(1f);
			return hash;
		}

		/// <summary>Hashes a byte array and returns <c>&lt;type&gt;:&lt;hex&gt;</c>.</summary>
		public static string HashBytes(string type, byte[] data)
			=> Format(type, Hex(type, data));

		/// <summary>Bare lowercase hexadecimal digest of the given type.</summary>
		public static string Hex(string type, byte[] data) {
			using var algorithm = Create(type);
			return ToHex(algorithm.ComputeHash(data));
		}

		/// <summary>Normalizes an algorithm name (<c>SHA-256</c> → <c>sha256</c>); defaults when empty.</summary>
		public static string NormalizeType(string type) {
			if (string.IsNullOrWhiteSpace(type))
				return DefaultType;

			var builder = new StringBuilder(type.Length);
			foreach (var character in type)
				if (char.IsLetterOrDigit(character))
					builder.Append(char.ToLowerInvariant(character));

			return builder.Length == 0 ? DefaultType : builder.ToString();
		}

		/// <summary>Combines an algorithm and a digest into <c>&lt;type&gt;:&lt;hex&gt;</c>.</summary>
		public static string Format(string type, string digest)
			=> string.IsNullOrEmpty(digest) ? null : $"{NormalizeType(type)}:{digest}";

		public static string ToHex(byte[] hash)
			=> hash == null ? null : BitConverter.ToString(hash).Replace("-", "").ToLower();

		private static HashAlgorithm Create(string type)
			=> NormalizeType(type) switch {
				"md5"    => MD5.Create(),
				"sha1"   => SHA1.Create(),
				"sha256" => SHA256.Create(),
				"sha384" => SHA384.Create(),
				"sha512" => SHA512.Create(),
				_        => throw new ArgumentException($"Unsupported hash type '{type}'", nameof(type))
			};
	}
}
