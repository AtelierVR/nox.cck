using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Nox.CCK.Utils {
	public struct ReportPoint {
		public long Tick;
		public string Msg;
		public int IndentLevel;
		public DateTime Time;
	}
	public class Report {
		public Stopwatch Stopwatch = null;
		public readonly List<ReportPoint> Points = new List<ReportPoint>();
		private static Report instance;
		private int currentIndentLevel;

		public static Report Instance {
			get => instance ??= New();
			private set => instance = value;
		}

		public static Report New()
			=> Instance = new Report().Start();

		public Report Step(string msg = null) {
			Points.Add(new ReportPoint {
				Tick = 0,
				Msg = msg,
				IndentLevel = currentIndentLevel,
				Time = DateTime.UtcNow
			});
			return this;
		}

		public Report Indent() {
			currentIndentLevel++;
			return this;
		}

		public Report Unindent() {
			if (currentIndentLevel > 0) currentIndentLevel--;
			return this;
		}

		public Report Start() {
			Stopwatch = Stopwatch ?? Stopwatch.StartNew();
#if UNITY_EDITOR
			EditorApplication.update += OnUnityUpdate;
#else
			Application.onBeforeRender += OnUnityUpdate;
#endif
			Step("Start");
			return this;
		}

		private void OnUnityUpdate() {
			if (Points.Count == 0) return;
			var lastPoint = Points[^1];
			lastPoint.Tick++;
			Points[^1] = lastPoint;
		}

		public Report Stop() {
#if UNITY_EDITOR
			EditorApplication.update -= OnUnityUpdate;
#else
			Application.onBeforeRender -= OnUnityUpdate;
#endif
			Stopwatch?.Stop();
			return this;
		}

		public override string ToString() {
			var result = "Report:\n";
			long lastTick = 0;
			DateTime lastRealTime = DateTime.MinValue;

			for (int i = 0; i < Points.Count; i++) {
				var point = Points[i];
				var delta = point.Tick - lastTick;

				// Calculer le temps réel écoulé depuis le dernier point
				long realTimeDelta = 0;
				if (lastRealTime != DateTime.MinValue) {
					realTimeDelta = (long)(point.Time - lastRealTime).TotalMilliseconds;
				}

				// Indentation basée sur le niveau
				var indent = new string(' ', point.IndentLevel * 2);

				// Détecter les freezes: si le temps réel est significativement plus long que le temps mesuré
				// Tolérance de 10ms pour les variations normales
				var freezeDetected = realTimeDelta > delta + 10 && realTimeDelta > 50;
				var freezeDuration = freezeDetected ? realTimeDelta - delta : 0;

				// Marqueur de performance
				string perfMarker = "";
				if (freezeDetected) {
					perfMarker = $" 🔴 REAL FREEZE (+{freezeDuration}ms hidden, real: {realTimeDelta}ms)";
				}
				else if (delta > 100) {
					perfMarker = " ⚠️ FREEZE";
				}
				else if (delta > 50) {
					perfMarker = " ⏱️ SLOW";
				}

				result += $"  {indent}+{delta}ms{perfMarker}";
				if (!string.IsNullOrEmpty(point.Msg))
					result += $" - {point.Msg}";
				result += "\n";

				lastTick = point.Tick;
				lastRealTime = point.Time;
			}
			result += $"Total: {Stopwatch.ElapsedMilliseconds}ms";
			return result;
		}

		public Report Complete() {
			Step("Complete");
			Stop();
			UnityEngine.Debug.Log(ToString());

			// Log des freezes détectés
			var freezeReport = GetFreezeReport();
			if (!string.IsNullOrEmpty(freezeReport)) {
				UnityEngine.Debug.LogWarning($"Freeze Detection Report:\n{freezeReport}");
			}

			Instance = null;
			return this;
		}

		public string GetFreezeReport() {
			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Freeze Detection Report:");
			var time = DateTime.MinValue;

			for (var i = 0; i < Points.Count; i++) {
				var point = Points[i];
				var delta = point.Time - time;

				// Détecter les freezes: si le temps réel est significativement plus long que le temps mesuré
				// Tolérance de 10ms pour les variations normales
				var ratio = delta.TotalMilliseconds / (point.Tick == 0 ? 1 : point.Tick);

				builder.AppendLine($"{point.Msg} ({ratio}ms) at {point.Time:HH:mm:ss.fff} (delta: {delta.TotalMilliseconds}ms)");
				time = point.Time;
			}

			return builder.ToString();
		}
	}
}