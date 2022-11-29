using HarmonyLib;
using UnityEngine;

namespace NoteMovementFix.Patches
{
    [HarmonyPatch(typeof(PlayerTransforms), nameof(PlayerTransforms.GetZPos))]
    internal class Yeet
    {
        static bool Prefix(ref float start, ref float end, ref float headOffsetZ, ref float t, ref float __result)
        {
            if(Config.Instance.Enabled)
            {
                __result = Mathf.LerpUnclamped(start + headOffsetZ, end + headOffsetZ, t);
                return false;
            }

            return true;
        }
    }
}
