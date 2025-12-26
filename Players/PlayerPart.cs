using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK.Players
{
    public class PlayerPart
    {
        public readonly PlayerRig Rig;
        public readonly TransformObject Transform;
        
        public PlayerPart(PlayerRig rig, TransformObject transform)
        {
            Rig = rig;
            Transform = transform;
        }
    }
}