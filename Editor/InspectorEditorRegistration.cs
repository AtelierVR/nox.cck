using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Editor {
	/// <summary>
	/// Registers every <see cref="InspectorEditor{T}"/> as the custom editor of the component it edits, so a
	/// derived editor does not have to declare <c>[CustomEditor(typeof(...))]</c>.
	/// <para>
	/// Unity builds its inspector table (<c>UnityEditor.CustomEditorAttributes</c>) once, by scanning the
	/// <c>[CustomEditor]</c> attributes, and exposes no API to add an entry. What holds the entries is reachable
	/// though: the table is a singleton whose cache (<c>m_Cache</c>) has an
	/// <c>Add(Type editorType, CustomEditor attribute, MonoEditorType descriptor)</c> — the very call its own scan
	/// makes per attribute. The descriptor is a
	/// <c>MonoEditorType(Type editorType, Type[] renderPipelines, bool forChildClasses, bool isFallback)</c>;
	/// a <c>null</c> pipeline array means "whatever the render pipeline is", which is what a plain
	/// <c>[CustomEditor]</c> produces.
	/// </para>
	/// <para>
	/// The members are looked up by shape and not only by name (the cache method is "three parameters, the first
	/// being the inspected type"; the descriptor is "the third parameter of that method"), so a rename does not break
	/// it. The registration is then verified with <c>CustomEditorAttributes.FindCustomEditorTypeByType</c> — the
	/// method the inspector itself calls — retried after an assembly reload (that is when the table is rebuilt) and
	/// reported once, naming the member that is missing, when it cannot happen.
	/// </para>
	/// </summary>
	[InitializeOnLoad]
	internal static class InspectorEditorRegistration {
		private const string AttributesTypeName = "UnityEditor.CustomEditorAttributes";
		private const string CacheFieldName     = "m_Cache";
		private const string AddMethodName      = "Add";
		private const string DescriptorTypeName = "MonoEditorType";
		private const string ProbeMethodName    = "FindCustomEditorTypeByType";
		private const string InstanceProbeName  = "GetCustomEditorType";

		private const BindingFlags All      = BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags Instance = BindingFlags.Instance | All;
		private const BindingFlags Static   = BindingFlags.Static | All;

		private static bool _registered;
		private static bool _failureLogged;

		/// <summary>What was found of Unity's table: any piece may be <c>null</c> when Unity moved it.</summary>
		private sealed class Table {
			public object          Instance;
			public object          Cache;
			public MethodInfo      Add;
			public Type            DescriptorType;
			public MethodInfo      Probe;
			public ConstructorInfo DescriptorConstructor;
		}

		static InspectorEditorRegistration() {
			AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			// Delayed: every assembly and asset of the project is ready by then.
			EditorApplication.delayCall -= Register;
			EditorApplication.delayCall += Register;
		}

		/// <summary>The table was just rebuilt: what was registered in the previous domain is gone.</summary>
		private static void OnAfterAssemblyReload() {
			_registered    = false;
			_failureLogged = false;
			EditorApplication.delayCall += Register;
		}

		/// <summary>
		/// Another chance to register: the table may have been rebuilt after the delayed call, in which case the
		/// entries are lost and the inspectors would silently fall back to the default one.
		/// </summary>
		private static void OnSelectionChanged() {
			if (!_registered)
				Register();
		}

		private static void Register() {
			if (_registered)
				return;

			try {
				var attributes = AttributesType();
				var table      = attributes == null ? null : TableOf(attributes);

				if (table == null || table.Add == null || table.DescriptorType == null) {
					Fail(Inspectors().Select(pair => pair.editor.Name), Missing(attributes, table));
					return;
				}

				var failed     = new List<string>();
				var registered = 0;

				foreach (var (component, editor) in Inspectors()) {
					if (table.Probe != null && Resolve(table, component) == editor) {
						registered++; // Already resolved — registered earlier, or an attribute of its own.
						continue;
					}

					try {
						table.Add.Invoke(table.Cache, new object[] {
							editor,
							new CustomEditor(component),
							DescriptorOf(table, editor)
						});
					} catch (Exception exception) {
						Debug.LogWarning("[Nox.CCK] Could not register " + editor.Name + ": " + Inner(exception));
						failed.Add(editor.Name);
						continue;
					}

					// Without a lookup to ask, the entry is trusted: Unity resolves it with the same cache.
					if (table.Probe == null || Resolve(table, component) != null)
						registered++;
					else
						failed.Add(editor.Name);
				}

				_registered = failed.Count == 0;

				if (!_registered) {
					Fail(failed, "the cache accepted the entry but the lookup does not see it");
					return;
				}

				if (table.Probe == null) {
					Debug.LogWarning("[Nox.CCK] Registered " + registered
						+ " inspector(s) without a [CustomEditor] attribute, but " + ProbeMethodName
						+ " could not be found to verify it.");
					return;
				}

				Debug.Log($"[Nox.CCK] {registered} inspector(s) registered without a [CustomEditor] attribute.");
			} catch (Exception exception) {
				Fail(Inspectors().Select(pair => pair.editor.Name), exception.ToString());
			}
		}

		private static string Inner(Exception exception)
			=> exception is TargetInvocationException { InnerException: { } inner } ? inner.ToString() : exception.ToString();

		private static void Fail(IEnumerable<string> editors, string reason) {
			if (_failureLogged)
				return;

			_failureLogged = true;
			Debug.LogError("[Nox.CCK] No inspector registered for: " + string.Join(", ", editors)
				+ " — " + reason + ". Declare [CustomEditor(typeof(...))] on those, and fix this class if Unity moved the table again.");
		}

		/// <summary>The pieces of the table that could not be reached, for the failure message.</summary>
		private static string Missing(Type attributes, Table table) {
			if (attributes == null)
				return AttributesTypeName + " not found";

			if (table == null)
				return AttributesTypeName + " found, but neither its instance nor its cache";

			var missing = new List<string>();
			if (table.Add == null)
				missing.Add("a cache method Add(Type, CustomEditor, descriptor)");
			if (table.DescriptorType == null)
				missing.Add("the descriptor type (" + DescriptorTypeName + ")");

			return "the table of " + AttributesTypeName + " changed shape — missing: " + string.Join(", ", missing)
				+ " · cache: " + table.Cache.GetType().Name
				+ " · instance: " + table.Instance.GetType().Name
				+ " · cache methods: " + string.Join("/", table.Cache.GetType()
					.GetMethods(Instance)
					.Select(method => method.Name + "(" + method.GetParameters().Length + ")")
					.Distinct());
		}

		/// <summary>
		/// <c>UnityEditor.CustomEditorAttributes</c>, resolved by name first: it lives in <c>UnityEditor</c>, an
		/// assembly that is not necessarily loaded yet when this runs.
		/// </summary>
		private static Type AttributesType() {
			var type = Type.GetType(AttributesTypeName + ", UnityEditor", false);
			if (type != null)
				return type;

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				type = assembly.GetType(AttributesTypeName, false);
				if (type != null)
					return type;
			}

			try {
				return Assembly.Load("UnityEditor").GetType(AttributesTypeName, false);
			} catch (Exception) {
				return null;
			}
		}

		/// <summary>Every piece of the table that can be reached, or <c>null</c> when there is no table at all.</summary>
		private static Table TableOf(Type attributes) {
			var table = new Table();
			try {
				table.Instance = InstanceOf(attributes);
			} catch (Exception) {
				return null;
			}

			if (table.Instance == null)
				return null;

			table.Cache = CacheOf(attributes, table.Instance);
			if (table.Cache == null)
				return null;

			table.Add                   = AddMethodOf(table.Cache);
			table.DescriptorType        = DescriptorTypeOf(attributes, table.Add);
			table.DescriptorConstructor = table.DescriptorType == null ? null : DescriptorConstructorOf(table.DescriptorType);
			table.Probe                 = ProbeOf(attributes);

			return table;
		}

		/// <summary>The instance of the singleton table, from its property or from its <c>Lazy&lt;T&gt;</c> field.</summary>
		private static object InstanceOf(Type attributes) {
			var property = attributes.GetProperty("instance", Static) ?? attributes.GetProperty("Instance", Static);
			if (property != null)
				return property.GetValue(null);

			var field = attributes.GetField("k_Instance", Static);
			var lazy  = field?.GetValue(null);

			var instance = lazy?.GetType().GetProperty("Value", Instance)?.GetValue(lazy);
			if (instance != null)
				return instance;

			// Last resort: whatever static member holds the table itself (or a lazy wrapping it).
			foreach (var candidate in attributes.GetFields(Static).Select(member => (object)member.GetValue(null))
				.Concat(attributes.GetProperties(Static).Select(member => member.GetValue(null)))) {
				if (candidate == null)
					continue;

				if (attributes.IsInstanceOfType(candidate))
					return candidate;

				var inner = candidate.GetType().GetProperty("Value", Instance)?.GetValue(candidate);
				if (inner != null && attributes.IsInstanceOfType(inner))
					return inner;
			}

			return null;
		}

		/// <summary>The object holding the descriptors: the <c>m_Cache</c> field, or any field that looks like a cache.</summary>
		private static object CacheOf(Type attributes, object instance) {
			for (var type = attributes; type != null; type = type.BaseType) {
				if (type.GetField(CacheFieldName, Instance) is { } field)
					return field.GetValue(instance);
			}

			for (var type = attributes; type != null; type = type.BaseType) {
				foreach (var field in type.GetFields(Instance)) {
					if (field.FieldType.Name.IndexOf("Cache", StringComparison.OrdinalIgnoreCase) >= 0)
						return field.GetValue(instance);
				}
			}

			return null;
		}

		/// <summary>
		/// <c>CustomEditorCache.Add(Type, CustomEditor, MonoEditorType)</c>: found by name, then by shape (three
		/// parameters, the first being the inspected type).
		/// </summary>
		private static MethodInfo AddMethodOf(object cache) {
			var candidates = cache.GetType()
				.GetMethods(Instance)
				.Where(method => method.GetParameters().Length == 3)
				.ToArray();

			return candidates.FirstOrDefault(method => method.Name == AddMethodName)
				?? candidates.FirstOrDefault(method => method.GetParameters()[0].ParameterType == typeof(Type)
					&& method.GetParameters()[1].ParameterType.Name.IndexOf("CustomEditor", StringComparison.OrdinalIgnoreCase) >= 0);
		}

		/// <summary>The descriptor type: nested in the table, or the third parameter of the cache method.</summary>
		private static Type DescriptorTypeOf(Type attributes, MethodInfo add) {
			return attributes.GetNestedType(DescriptorTypeName, All)
				?? add?.GetParameters()[2].ParameterType;
		}

		/// <summary>
		/// <c>MonoEditorType(Type, Type[], bool, bool)</c>: its exact shape only — anything else is built by
		/// <see cref="DescriptorOf"/> from an empty instance, which does not care about the constructor at all.
		/// </summary>
		private static ConstructorInfo DescriptorConstructorOf(Type descriptor) {
			return descriptor.GetConstructors(All).FirstOrDefault(constructor => {
				var parameters = constructor.GetParameters();
				return parameters.Length == 4
					&& parameters[0].ParameterType == typeof(Type)
					&& parameters[1].ParameterType == typeof(Type[])
					&& parameters[2].ParameterType == typeof(bool)
					&& parameters[3].ParameterType == typeof(bool);
			});
		}

		/// <summary>
		/// The lookup Unity uses for an inspector: <c>CustomEditorAttributes.FindCustomEditorTypeByType</c>, or the
		/// instance method behind it.
		/// </summary>
		private static MethodInfo ProbeOf(Type attributes) {
			return attributes.GetMethod(ProbeMethodName, Static, null, new[] { typeof(Type), typeof(bool) }, null)
				?? attributes.GetMethod(InstanceProbeName, Instance, null, new[] { typeof(Type), typeof(bool) }, null);
		}

		/// <summary>The editor Unity resolves for the component, or <c>null</c> when it has none.</summary>
		private static Type Resolve(Table table, Type component) {
			if (table.Probe == null)
				return null;

			try {
				var target = table.Probe.IsStatic ? null : table.Instance;
				return table.Probe.Invoke(target, new object[] { component, false }) as Type;
			} catch (Exception) {
				return null;
			}
		}

		/// <summary>
		/// A descriptor of <paramref name="table"/>'s type for that editor: through its constructor when it has the
		/// expected shape, else from an empty instance whose type fields are filled.
		/// </summary>
		private static object DescriptorOf(Table table, Type editor) {
			if (table.DescriptorConstructor != null)
				return table.DescriptorConstructor.Invoke(new object[] { editor, null, false, false });

			var descriptor = Activator.CreateInstance(table.DescriptorType, true);

			foreach (var field in table.DescriptorType.GetFields(Instance)) {
				if (field.FieldType == typeof(Type))
					field.SetValue(descriptor, editor);
				else if (field.FieldType == typeof(Type[]))
					field.SetValue(descriptor, null);
				else if (field.FieldType == typeof(bool))
					field.SetValue(descriptor, false);
			}

			return descriptor;
		}

		/// <summary>Every (component, editor) pair of the <see cref="InspectorEditor{T}"/> of the project.</summary>
		private static IEnumerable<(Type component, Type editor)> Inspectors() {
			foreach (var type in TypeCache.GetTypesDerivedFrom<UnityEditor.Editor>()) {
				if (type.IsAbstract || type.IsGenericTypeDefinition)
					continue;

				for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType) {
					if (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(InspectorEditor<>))
						continue;

					// An attribute of its own is the authority: a second entry would only fight it.
					if (type.GetCustomAttribute<CustomEditor>(false) != null)
						break;

					yield return (baseType.GetGenericArguments()[0], type);
					break;
				}
			}
		}
	}
}
