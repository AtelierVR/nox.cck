using System;
using UnityEngine;

namespace Nox.CCK.Development
{
    /// <summary>
    /// Interface for components that can draw custom gizmos in the scene view.
    /// <para>
    /// <see cref="OnDrawGizmos"/> is the Unity message: implemented publicly, the scene view calls it by
    /// itself. Implemented <b>explicitly</b> (<c>void IGizmos.OnDrawGizmos()</c>), Unity cannot see it and
    /// <c>Nox.CCK.Editor.GizmosRenderer</c> calls it instead, which also honours the
    /// <see cref="GizmosAttribute.DrawGizmo"/> options (selected / not selected).
    /// </para>
    /// <para>
    /// Either way, a <see cref="GizmosAttribute"/> carrying the « Pickable » option makes the glyphs
    /// pickable: a click selects the object.
    /// </para>
    /// </summary>
    public interface IGizmos
    {
        /// <summary>
        /// Draws the gizmos of this component. Empty by default.
        /// </summary>
        void OnDrawGizmos() { }

        /// <summary>
        /// World points the renderer tests for picking, a disc around each: the glyphs of a component are
        /// rarely around its pivot. Empty by default, which picks around the pivot.
        /// </summary>
        Vector3[] GetPickPoints() 
            => Array.Empty<Vector3>();

        /// <summary>
        /// Gets the gizmos attribute associated with this component's type.
        /// </summary>
        /// <returns>The gizmos attribute, or null if not found.</returns>
        public GizmosAttribute GetGizmosAttribute() {
            var type = GetType();
            var attributes = type.GetCustomAttributes(typeof(GizmosAttribute), true);
            if (attributes.Length == 0)
                return null;
            return (GizmosAttribute)attributes[0];
        }
    }
}