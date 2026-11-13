using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Hash helpers. The typeless overloads return a bare lowercase hexadecimal digest, while the
	/// typed overloads (<c>Hash(type, ...)</c>) return the typed value <c>&lt;type&gt;:&lt;hex&gt;</c>
	/// used by the asset pipeline (e.g. <c>sha256:0123...</c>).
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
			return ToHex(hash);
		}

		public static string HashFile(string path) {
			using var sha = SHA256.Create();
			using var stream = File.OpenRead(path);
			var hash = sha.ComputeHash(stream);
			return ToHex(hash);
		}

		public static string HashBytes(byte[] data) {
			using var sha = SHA256.Create();
			var hash = sha.ComputeHash(data);
			return ToHex(hash);
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

		/// <summary>
		/// Memoized normalized names. Callers normalize inside per-element predicates
		/// (<c>Hashs.FirstOrDefault(hash =&gt; NormalizeType(hash.Type) == NormalizeType(type))</c>)
		/// while the set of distinct algorithm names is tiny, so every call used to pay a
		/// StringBuilder + one culture-aware <c>char.ToLowerInvariant</c> per character.
		/// </summary>
		private static readonly ConcurrentDictionary<string, string> NormalizedTypes = new();

		/// <summary>Normalizes an algorithm name (<c>SHA-256</c> → <c>sha256</c>); defaults when empty.</summary>
		public static string NormalizeType(string type) {
			if (string.IsNullOrWhiteSpace(type))
				return DefaultType;

			if (NormalizedTypes.TryGetValue(type, out var cached))
				return cached;

			var normalized = NormalizeTypeSlow(type);

			// Only memoize short names: this keeps an unexpected caller from growing the
			// cache without bound while still covering every real algorithm name.
			if (type.Length <= 32 && NormalizedTypes.Count < 256)
				NormalizedTypes[type] = normalized;

			return normalized;
		}

		private static string NormalizeTypeSlow(string type) {
			// Fast path: a name that is already lowercase ASCII (the common case) is returned
			// as-is, without allocating a StringBuilder or touching the globalization code.
			var plain = true;
			for (var i = 0; i < type.Length; i++) {
				var c = type[i];
				if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
					continue;

				plain = false;
				break;
			}

			if (plain)
				return type;

			var builder = new StringBuilder(type.Length);
			foreach (var character in type) {
				if (character >= 'a' && character <= 'z' || character >= '0' && character <= '9') {
					builder.Append(character);
				} else if (character >= 'A' && character <= 'Z') {
					builder.Append((char)(character + 32));
				} else if (char.IsLetterOrDigit(character)) {
					builder.Append(char.ToLowerInvariant(character));
				}
			}

			return builder.Length == 0 ? DefaultType : builder.ToString();
		}

		/// <summary>Combines an algorithm and a digest into <c>&lt;type&gt;:&lt;hex&gt;</c>.</summary>
		public static string Format(string type, string digest)
			=> string.IsNullOrEmpty(digest) ? null : $"{NormalizeType(type)}:{digest}";

		/// <summary>
		/// Lowercase hexadecimal digest, built digit by digit on purpose.
		/// <c>BitConverter.ToString(hash).Replace("-", "").ToLower()</c> allocated three strings per
		/// call and routed the casing through the culture-aware TextInfo machinery: one profiler
		/// capture showed ~750k <c>ToLower</c> calls coming from this method alone.
		/// </summary>
		public static string ToHex(byte[] hash) {
			if (hash == null)
				return null;

			const string digits = "0123456789abcdef";
			var chars = new char[hash.Length * 2];
			for (var i = 0; i < hash.Length; i++) {
				var b = hash[i];
				chars[i * 2]     = digits[b >> 4];
				chars[i * 2 + 1] = digits[b & 0x0F];
			}

			return new string(chars);
		}

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
