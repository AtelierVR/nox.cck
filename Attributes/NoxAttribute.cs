using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Attributes
{
    /// <summary>
    /// Tag-based marker for methods invoked via NoxAttribute.Invoke / InvokeAsync.
    /// </summary>
    /// <example>
    /// [Nox("build:any")]  public static void MyBuildStep() { }
    /// [Nox("build:mod")]  public static async UniTask MyAsyncStep() { }
    /// [Nox("build:game")] public static bool MyCheck() => true;
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NoxAttribute : Attribute
    {
        public string Tag { get; }

        public NoxAttribute(string tag)
            => Tag = tag;

        private const string LogTag = nameof(NoxAttribute);

        // ═══════════════════════════════════════════════════════════════
        // Static discovery & invocation
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Invokes all public static methods tagged [Nox(tag)] across loaded assemblies.
        /// Supports signatures: void(), bool(), UniTask(), UniTask&lt;bool&gt;().
        /// If returning bool, a false return is logged as non-fatal.
        /// Throws if any invoked method throws.
        /// </summary>
        public static void Invoke(string tag) {
            foreach (var (method, _) in Discover(tag))
                try {
                    var result = method.Invoke(null, null);
                    if (result is bool b && !b)
                        Logger.LogWarning($"[{tag}] {method.DeclaringType!.Name}.{method.Name}() returned false.", tag: LogTag);
                } catch (TargetInvocationException ex) {
                    Logger.LogError($"[{tag}] {method.DeclaringType!.Name}.{method.Name}() threw: {ex.InnerException?.Message}", tag: LogTag);
                    throw;
                }
            
        }

        /// <summary>
        /// Invokes all public static methods tagged [Nox(tag)] asynchronously.
        /// Supports signatures: UniTask(), UniTask&lt;bool&gt;(), void(), bool().
        /// Throws if any invoked method throws.
        /// </summary>
        public static async UniTask InvokeAsync(string tag) {
            foreach (var (method, _) in Discover(tag))
                try {
                    var result = method.Invoke(null, null);

                    if (result is UniTask task) {
                        await task;
                    } else if (result is UniTask<bool> boolTask) {
                        var b = await boolTask;
                        if (!b)
                            Logger.LogWarning($"[{tag}] {method.DeclaringType!.Name}.{method.Name}() returned false.", tag: LogTag);
                    } else if (result is bool b && !b) {
                        Logger.LogWarning($"[{tag}] {method.DeclaringType!.Name}.{method.Name}() returned false.", tag: LogTag);
                    }
                } catch (TargetInvocationException ex) {
                    Logger.LogError($"[{tag}] {method.DeclaringType!.Name}.{method.Name}() threw: {ex.InnerException?.Message}", tag: LogTag);
                    throw;
                }
        }

        /// <summary>
        /// Discovers all public static methods with [Nox(tag)] across all loaded assemblies.
        /// Returns tuples of (MethodInfo, NoxAttribute).
        /// </summary>
        public static IEnumerable<(MethodInfo method, NoxAttribute attr)> Discover(string tag) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException) { continue; }

                foreach (var type in types)
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
                        var attr = method.GetCustomAttribute<NoxAttribute>();
                        if (attr == null || attr.Tag != tag) continue;
                        if (method.GetParameters().Length != 0) continue;
                        yield return (method, attr);
                    }
            }
        }
    }
}

