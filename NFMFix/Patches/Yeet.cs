using HarmonyLib;

namespace NoteMovementFix.Patches
{
    [HarmonyPatch(typeof(PlayerTransforms), nameof(PlayerTransforms.GetZPos))]
    internal class Yeet
    {
        static void Prefix(ref float start, ref float end, ref float headOffsetZ, ref float t)
        {
            headOffsetZ = 0f;
        }
    }
}
