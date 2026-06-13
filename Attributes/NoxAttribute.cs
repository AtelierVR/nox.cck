using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Attributes
{
    /// <summary>
    /// Tag-based marker for methods invoked via NoxInvokableAttribute.Invoke / InvokeAsync.
    /// </summary>
    /// <example>
    /// [Nox("build:any")]  public static void MyBuildStep() { }
    /// [Nox("build:mod")]  public static async UniTask MyAsyncStep() { }
    /// [Nox("build:game")] public static bool MyCheck() => true;
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NoxInvokableAttribute : Attribute
    {
        public string Tag { get; }

        public NoxInvokableAttribute(string tag)
            => Tag = tag;

        private const string LogTag = nameof(NoxInvokableAttribute);

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
            foreach (var (method, _) in Discover(tag, 0))
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
        /// Invokes all public static methods tagged [Nox(tag)] with matching parameter count and types.
        /// Parameters are matched positionally by type.
        /// </summary>
        public static void Invoke(string tag, params object[] args) {
            foreach (var (method, _) in Discover(tag, args?.Length ?? 0)) {
                var parameters = method.GetParameters();
                if (parameters.Length != (args?.Length ?? 0)) continue;
                if (!ParametersMatch(parameters, args)) continue;
                try {
                    var result = method.Invoke(null, args);
                    if (result is bool b && !b)
                        Logger.LogWarning($"[{tag}] {method.DeclaringType!.Name}.{method.Name}(...) returned false.", tag: LogTag);
                } catch (TargetInvocationException ex) {
                    Logger.LogError($"[{tag}] {method.DeclaringType!.Name}.{method.Name}(...) threw: {ex.InnerException?.Message}", tag: LogTag);
                    throw;
                }
            }
        }

        /// <summary>
        /// Invokes all public static methods tagged [Nox(tag)] asynchronously.
        /// Supports signatures: UniTask(), UniTask&lt;bool&gt;(), void(), bool().
        /// Throws if any invoked method throws.
        /// </summary>
        public static async UniTask InvokeAsync(string tag) {
            foreach (var (method, _) in Discover(tag, 0))
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
        /// Invokes all public static methods tagged [Nox(tag)] asynchronously with arguments.
        /// Parameters are matched positionally by type.
        /// </summary>
        public static async UniTask InvokeAsync(string tag, params object[] args) {
            foreach (var (method, _) in Discover(tag, args?.Length ?? 0)) {
                var parameters = method.GetParameters();
                if (parameters.Length != (args?.Length ?? 0)) continue;
                if (!ParametersMatch(parameters, args)) continue;
                try {
                    var result = method.Invoke(null, args);

                    if (result is UniTask task) {
                        await task;
                    } else if (result is UniTask<bool> boolTask) {
                        var b = await boolTask;
                        if (!b)
                            Logger.LogWarning($"[{tag}] {method.DeclaringType!.Name}.{method.Name}(...) returned false.", tag: LogTag);
                    } else if (result is bool b && !b) {
                        Logger.LogWarning($"[{tag}] {method.DeclaringType!.Name}.{method.Name}(...) returned false.", tag: LogTag);
                    }
                } catch (TargetInvocationException ex) {
                    Logger.LogError($"[{tag}] {method.DeclaringType!.Name}.{method.Name}(...) threw: {ex.InnerException?.Message}", tag: LogTag);
                    throw;
                }
            }
        }

        /// <summary>
        /// Discovers all public static methods with [Nox(tag)] across all loaded assemblies.
        /// When paramCount is -1, any parameter count is accepted. Otherwise filters by exact count.
        /// Returns tuples of (MethodInfo, NoxInvokableAttribute..
        /// </summary>
        public static IEnumerable<(MethodInfo method, NoxInvokableAttribute attr)> Discover(string tag, int paramCount = -1) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException) { continue; }

                foreach (var type in types)
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
                        var attr = method.GetCustomAttribute<NoxInvokableAttribute>();
                        if (attr == null || attr.Tag != tag) continue;
                        if (paramCount >= 0 && method.GetParameters().Length != paramCount) continue;
                        yield return (method, attr);
                    }
            }
        }

        private static bool ParametersMatch(ParameterInfo[] parameters, object[] args) {
            for (int i = 0; i < parameters.Length; i++) {
                if (args[i] == null) continue; // null matches any reference type
                var paramType = parameters[i].ParameterType;
                var argType = args[i].GetType();
                if (!paramType.IsAssignableFrom(argType))
                    return false;
            }
            return true;
        }
    }
}

