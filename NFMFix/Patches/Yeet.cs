using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace NoteMovementFix.Patches
{
    [HarmonyPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.ManualUpdate))]
    internal class DisableFloorMovement
    {
        static bool Prefix(ref NoteFloorMovement __instance, ref Vector3 __result)
        {
            // Skip floor movement if disabled
            if (Config.Instance.Enabled && !Plugin.InReplay && Config.Instance.HiddenFloorMovement)
            {
                return false;
            }

            // Instant Swap during NoteFloorMovement
            if (Config.Instance.Enabled && Config.Instance.InstantSwap)
            {
                float num = __instance._audioTimeSyncController.songTime - (__instance._beatTime - __instance._variableMovementDataProvider.moveDuration - __instance._variableMovementDataProvider.halfJumpDuration);
                __instance._localPosition = Vector3.LerpUnclamped(__instance._variableMovementDataProvider.moveStartPosition + __instance._moveStartOffset, __instance._variableMovementDataProvider.moveEndPosition + __instance._moveEndOffset, num / __instance._variableMovementDataProvider.moveDuration);
                __instance._localPosition.x = __instance._variableMovementDataProvider.moveEndPosition.x + __instance._moveEndOffset.x;
                Vector3 result = __instance._worldRotation * __instance._localPosition;
                __instance.transform.localPosition = result;
                __result = result;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NoteJump), nameof(NoteJump.ManualUpdate))]
    internal class NoteJumpStuff
    {
        static bool Prefix(ref NoteJump __instance, ref Vector3 __result, ref Action ___noteJumpDidStartEvent, ref Action ___noteJumpDidFinishEvent,
            ref Action ___noteJumpDidPassMissedMarkerEvent, ref Action<NoteJump> ___noteJumpDidPassThreeQuartersEvent, ref Action ___noteJumpDidPassHalfEvent,
            ref Action<float> ___noteJumpDidUpdateProgressEvent)
        {
            if (Config.Instance.Enabled && !Plugin.InReplay)
            {
                if (!__instance._missedMarkReported)
                {
                    __instance._halfJumpDuration = __instance._variableMovementDataProvider.halfJumpDuration;
                    __instance._jumpDuration = __instance._variableMovementDataProvider.jumpDuration;
                    __instance._gravity = __instance._variableMovementDataProvider.CalculateCurrentNoteJumpGravity(__instance._gravityBase);
                    __instance._startPos = __instance._variableMovementDataProvider.moveEndPosition + __instance._startOffset;
                    __instance._endPos = __instance._variableMovementDataProvider.jumpEndPosition + __instance._endOffset;
                    __instance._missedTime = __instance._noteTime + 0.15f;
                }

                float songTime = __instance._audioTimeSyncController.songTime;
                float num = songTime - (__instance._noteTime - __instance._halfJumpDuration);
                float num2 = num / __instance._jumpDuration;
                // Instant Swap during NoteJump
                if (Config.Instance.InstantSwap)
                {
                    __instance._localPosition.x = __instance._endPos.x;
                }
                else
                {
                    if (__instance._startPos.x == __instance._endPos.x)
                    {
                        __instance._localPosition.x = __instance._startPos.x;
                    }
                    else if (num2 < 0.25f)
                    {
                        __instance._localPosition.x = __instance._startPos.x + (__instance._endPos.x - __instance._startPos.x) * Easing.InOutQuad(num2 * 4f);
                    }
                    else
                    {
                        __instance._localPosition.x = __instance._endPos.x;
                    }
                }

                __instance._localPosition.z = Mathf.LerpUnclamped(__instance._startPos.z, __instance._endPos.z, num2);
                float num3 = __instance._gravity * __instance._halfJumpDuration;
                __instance._localPosition.y = __instance._startPos.y + num3 * num - __instance._gravity * num * num * 0.5f;
                // No need to avoid if InstantSwap is on.
                if (!Config.Instance.InstantSwap && __instance._yAvoidance != 0f && num2 < 0.25f)
                {
                    float num4 = 0.5f - Mathf.Cos(num2 * 8f * (float)Math.PI) * 0.5f;
                    __instance._localPosition.y += num4 * __instance._yAvoidance;
                }

                if (num2 < 0.5f)
                {
                    // Fake Ghost Mode
                    if (num2 > 0.25f)
                    {
                        if (Config.Instance.Enabled && Config.Instance.FakeGhostMode && Config.Instance.FakeGhostArrow)
                        {
                            var note = __instance._rotatedObject.parent.GetComponentsInChildren<Transform>();
                            foreach (var child in note)
                            {
                                if (child.name.Contains("NoteArrow") || child.name.Contains("Circle"))
                                {
                                    child.gameObject.layer = (int)Config.Instance.Layer;
                                }
                            }
                        }
                    }

                    Quaternion quaternion;
                    // Initial rotation become instant and remove the note visual sway.
                    if (Config.Instance.InstantRotation)
                    {
                        quaternion = __instance._endRotation;
                    }
                    else
                    {
                        quaternion = ((num2 < 0.125f) ? Quaternion.Slerp(__instance._startRotation, __instance._middleRotation, Mathf.Sin(num2 * (float)Math.PI * 4f)) : Quaternion.Slerp(__instance._middleRotation, __instance._endRotation, Mathf.Sin((num2 - 0.125f) * (float)Math.PI * 2f)));
                    }
                    // Skip if disabled. Won't look toward the player while near the player.
                    if (!Config.Instance.DisableCloseRotation && __instance._rotateTowardsPlayer)
                    {
                        Vector3 headPseudoLocalPos = __instance._playerTransforms.headPseudoLocalPos;
                        headPseudoLocalPos.y = Mathf.Lerp(headPseudoLocalPos.y, __instance._localPosition.y, 0.8f);
                        headPseudoLocalPos = __instance._inverseWorldRotation * headPseudoLocalPos;
                        Vector3 normalized = (__instance._localPosition - headPseudoLocalPos).normalized;
                        Quaternion b = default;
                        Vector3 vector = __instance._playerSpaceConvertor.worldToPlayerSpaceRotation * __instance._rotatedObject.up;
                        b.SetLookRotation(normalized, __instance._inverseWorldRotation * vector);
                        __instance._rotatedObject.localRotation = Quaternion.Lerp(quaternion, b, num2 * 2f);
                    }
                    else
                    {
                        __instance._rotatedObject.localRotation = quaternion;
                    }
                }

                if (!__instance._jumpStartedReported)
                {
                    __instance._jumpStartedReported = true;
                    ___noteJumpDidStartEvent?.Invoke();
                }

                if (num2 >= 0.5f && !__instance._halfJumpMarkReported)
                {
                    __instance._halfJumpMarkReported = true;
                    ___noteJumpDidPassHalfEvent?.Invoke();
                }

                if (num2 >= 0.75f && !__instance._threeQuartersMarkReported)
                {
                    __instance._threeQuartersMarkReported = true;
                    ___noteJumpDidPassThreeQuartersEvent?.Invoke(__instance);
                }

                if (songTime >= __instance._missedTime && !__instance._missedMarkReported)
                {
                    __instance._missedMarkReported = true;
                    ___noteJumpDidPassMissedMarkerEvent?.Invoke();
                }

                if (__instance._threeQuartersMarkReported)
                {
                    float num5 = (num2 - 0.75f) / 0.25f;
                    num5 = num5 * num5 * num5;
                    __instance._localPosition.z -= Mathf.LerpUnclamped(0f, __instance._endDistanceOffset, num5);
                }

                if (num2 >= 1f)
                {
                    if (!__instance._missedMarkReported)
                    {
                        __instance._missedMarkReported = true;
                        ___noteJumpDidPassMissedMarkerEvent?.Invoke();
                    }

                    ___noteJumpDidFinishEvent?.Invoke();
                }

                Vector3 result = __instance._worldRotation * __instance._localPosition;
                __instance.transform.localPosition = result;
                ___noteJumpDidUpdateProgressEvent?.Invoke(num2);
                __result =  result;

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PauseController), nameof(PauseController.HandleFocusWasCaptured))]
    internal class DisableFocusPause
    {
        static bool Prefix()
        {
            if(Config.Instance.Enabled && Config.Instance.RemoveHMDPause)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PauseController), nameof(PauseController.HandleHMDUnmounted))]
    internal class DisableHMDPause
    {
        static bool Prefix()
        {
            if (Config.Instance.Enabled && Config.Instance.RemoveHMDPause)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PauseController), nameof(PauseController.HandleMenuButtonTriggered))]
    internal class DisablePauseButton
    {
        static bool Prefix()
        {
            if (Config.Instance.Enabled && Config.Instance.DisablePause)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.Init))]
    internal class FakeGhostMode
    {
        static void Prefix(ref NoteFloorMovement __instance)
        {
            if (Config.Instance.Enabled && Config.Instance.FakeGhostMode && Config.Instance.FakeGhostNote)
            {
                var p = __instance._rotatedObject.parent;
                foreach (Transform t in p)
                {
                    t.gameObject.layer = (int)Config.Instance.Layer;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameplayCoreInstaller), nameof(GameplayCoreInstaller.InstallBindings))]
    static class DisableNE
    {
        static void Postfix(ref GameplayCoreInstaller __instance)
        {
            var key = (BeatmapKey)__instance._sceneSetupData?.beatmapKey;
            if (key != null)
            {
                var hasRequirement = SongCore.Collections.GetCustomLevelSongDifficultyData(key)?
                .additionalDifficultyData?
                ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;
                if (hasRequirement) Config.Instance.Enabled = false;
            }
        }
    }
}
