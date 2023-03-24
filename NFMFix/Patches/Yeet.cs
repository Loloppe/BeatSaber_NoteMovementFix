using HarmonyLib;
using System;
using UnityEngine;

namespace NoteMovementFix.Patches
{
    [HarmonyPatch(typeof(PlayerTransforms), nameof(PlayerTransforms.GetZPos))]
    internal class Yeet
    {
        static bool Prefix(ref float start, ref float end, ref float headOffsetZ, ref float t, ref float __result)
        {
            // Remove the fixed spawn position
            if (Config.Instance.Enabled && Config.Instance.DisableNJS && !Plugin.InReplay)
            {
                __result = Mathf.LerpUnclamped(start + headOffsetZ, end + headOffsetZ, t);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.ManualUpdate))]
    internal class Yeet2
    {
        static bool Prefix(ref IAudioTimeSource ____audioTimeSyncController, ref float ____startTime, ref float ____moveDuration, ref Vector3 ____startPos, ref Vector3 ____endPos, ref Vector3 __result,
            ref Vector3 ____localPosition, ref NoteFloorMovement __instance, ref Quaternion ____worldRotation, ref Action ___floorMovementDidFinishEvent)
        {
            if (Config.Instance.Enabled && !Plugin.InReplay)
            {
                float num = ____audioTimeSyncController.songTime - ____startTime;

                // Skip movement if disabled
                if (!Config.Instance.DisableFloorMovement)
                {
                    // Fix NoteFloorMovement to match player position
                    if (Config.Instance.DisableNJS)
                    {
                        ____localPosition = Vector3.Lerp(____startPos + new Vector3(0, 0, Camera.main.transform.position.z), ____endPos + new Vector3(0, 0, Camera.main.transform.position.z), num / ____moveDuration);
                    }
                    else
                    {
                        ____localPosition = Vector3.Lerp(____startPos, ____endPos, num / ____moveDuration);
                    }
                    Vector3 vector = ____worldRotation * ____localPosition;
                    __instance.transform.localPosition = vector;

                    __result = vector;
                }
                
                if (num >= ____moveDuration)
                {
                    ___floorMovementDidFinishEvent?.Invoke();
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ObstacleController), nameof(ObstacleController.GetPosForTime))]
    internal class Yeet3
    {
        static bool Prefix(float time, ref Vector3 ____startPos, ref Vector3 ____midPos, ref Vector3 ____endPos, ref float ____move1Duration, ref float ____move2Duration,
            ref PlayerTransforms ____playerTransforms, ref Quaternion ____inverseWorldRotation, ref bool ____passedAvoidedMarkReported, ref float ____passedAvoidedMarkTime,
            ref float ____finishMovementTime, ref float ____endDistanceOffset, ref Vector3 __result)
        {
            // Fix ObstacleController to match player position
            if (Config.Instance.Enabled && Config.Instance.DisableNJS && !Plugin.InReplay)
            {
                Vector3 vector;
                if (time < ____move1Duration)
                {
                    vector = Vector3.LerpUnclamped(____startPos + new Vector3(0, 0, Camera.main.transform.position.z), ____midPos + new Vector3(0, 0, Camera.main.transform.position.z), (____move1Duration < Mathf.Epsilon) ? 0f : (time / ____move1Duration));
                }
                else
                {
                    float num = (time - ____move1Duration) / ____move2Duration;
                    vector.x = ____startPos.x;
                    vector.y = ____startPos.y;
                    vector.z = ____playerTransforms.MoveTowardsHead(____midPos.z, ____endPos.z, ____inverseWorldRotation, num);
                    if (____passedAvoidedMarkReported)
                    {
                        float num2 = (time - ____passedAvoidedMarkTime) / (____finishMovementTime - ____passedAvoidedMarkTime);
                        num2 = num2 * num2 * num2;
                        vector.z -= Mathf.LerpUnclamped(0f, ____endDistanceOffset, num2);
                    }
                }
                __result = vector;

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(FlyingScoreSpawner), nameof(FlyingScoreSpawner.SpawnFlyingScore))]
    internal class Yeet4
    {
        static bool Prefix(ref IReadonlyCutScoreBuffer cutScoreBuffer, ref Color color, ref FlyingScoreSpawner __instance, ref FlyingScoreEffect.Pool ____flyingScoreEffectPool, ref FlyingScoreSpawner.InitData ____initData)
        {
            // Fix FlyingScore to match player position
            if (Config.Instance.Enabled && Config.Instance.DisableNJS && !Plugin.InReplay)
            {
                NoteCutInfo noteCutInfo = cutScoreBuffer.noteCutInfo;
                Vector3 vector = noteCutInfo.cutPoint;
                FlyingScoreEffect flyingScoreEffect = ____flyingScoreEffectPool.Spawn();
                flyingScoreEffect.didFinishEvent.Add(__instance);
                flyingScoreEffect.transform.localPosition = vector;
                vector = noteCutInfo.inverseWorldRotation * vector;
                vector.z = Camera.main.transform.position.z;
                float num = 0f;
                if (____initData.spawnPosition == FlyingScoreSpawner.SpawnPosition.Underground)
                {
                    vector.y = -0.24f;
                }
                else
                {
                    vector.y = 0.25f;
                    num = -0.1f;
                }
                Vector3 vector2 = noteCutInfo.worldRotation * (vector + new Vector3(0f, num, 7.55f));
                flyingScoreEffect.InitAndPresent(cutScoreBuffer, 0.7f, vector2, color);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NoteJump), nameof(NoteJump.ManualUpdate))]
    internal class Yeet5
    {
        static bool Prefix(ref IAudioTimeSource ____audioTimeSyncController, ref float ____jumpDuration, ref float ____beatTime, ref Vector3 ____startPos, ref Vector3 ____endPos, ref Vector3 __result,
            ref Vector3 ____localPosition, ref NoteJump __instance, ref Quaternion ____inverseWorldRotation, ref Action ___noteJumpDidPassHalfEvent, ref PlayerTransforms ____playerTransforms,
            ref float ____startVerticalVelocity, ref float ____yAvoidance, ref float ____gravity, ref Quaternion ____startRotation, ref Quaternion ____middleRotation, ref Quaternion ____endRotation,
            ref bool ____rotateTowardsPlayer, ref PlayerSpaceConvertor ____playerSpaceConvertor, ref Transform ____rotatedObject, ref bool ____halfJumpMarkReported, ref bool ____threeQuartersMarkReported,
            ref bool ____missedMarkReported, ref float ____missedTime, ref Action<NoteJump> ___noteJumpDidPassThreeQuartersEvent, ref Action ___noteJumpDidPassMissedMarkerEvent, ref Action ___noteJumpDidFinishEvent,
            ref Quaternion ____worldRotation, ref Action<float> ___noteJumpDidUpdateProgressEvent, ref float ____endDistanceOffset)
        {
            if (Config.Instance.Enabled && !Plugin.InReplay)
            {
                float songTime = ____audioTimeSyncController.songTime;
                float num = songTime - (____beatTime - ____jumpDuration * 0.5f);
                float num2 = num / ____jumpDuration;
                if (____startPos.x == ____endPos.x)
                {
                    ____localPosition.x = ____startPos.x;
                }
                else if (num2 < 0.25f)
                {
                    ____localPosition.x = ____startPos.x + (____endPos.x - ____startPos.x) * Easing.InOutQuad(num2 * 4f);
                }
                else
                {
                    ____localPosition.x = ____endPos.x;
                }
                ____localPosition.z = ____playerTransforms.MoveTowardsHead(____startPos.z, ____endPos.z, ____inverseWorldRotation, num2);
                ____localPosition.y = ____startPos.y + ____startVerticalVelocity * num - ____gravity * num * num * 0.5f;
                if (!Config.Instance.DisableFloorMovement && ____yAvoidance != 0f && num2 < 0.25f)
                {
                    float num3 = 0.5f - Mathf.Cos(num2 * 8f * Mathf.PI) * 0.5f;
                    ____localPosition.y += num3 * ____yAvoidance;
                }
                if (num2 < 0.5f)
                {
                    Quaternion quaternion;
                    // Initial rotation become instant and remove the note visual sway.
                    if (Config.Instance.DisableRotation)
                    {
                        quaternion = ____endRotation;
                    }
                    else
                    {
                        if (num2 < 0.125f)
                        {
                            quaternion = Quaternion.Slerp(____startRotation, ____middleRotation, Mathf.Sin(num2 * 3.1415927f * 4f));
                        }
                        else
                        {
                            quaternion = Quaternion.Slerp(____middleRotation, ____endRotation, Mathf.Sin((num2 - 0.125f) * 3.1415927f * 2f));
                        }
                    }
                    // Skip if disabled. Won't look toward the player while near the player.
                    if (!Config.Instance.DisableCloseRotation && ____rotateTowardsPlayer)
                    {
                        Vector3 vector = ____playerTransforms.headPseudoLocalPos;
                        vector.y = Mathf.Lerp(vector.y, ____localPosition.y, 0.8f);
                        vector = ____inverseWorldRotation * vector;
                        Vector3 normalized = (____localPosition - vector).normalized;
                        Quaternion quaternion2 = default;
                        Vector3 vector2 = ____playerSpaceConvertor.worldToPlayerSpaceRotation * ____rotatedObject.up;
                        quaternion2.SetLookRotation(normalized, ____inverseWorldRotation * vector2);
                        ____rotatedObject.localRotation = Quaternion.Lerp(quaternion, quaternion2, num2 * 2f);
                    }
                    else
                    {
                        ____rotatedObject.localRotation = quaternion;
                    }
                }
                if (num2 >= 0.5f && !____halfJumpMarkReported)
                {
                    ____halfJumpMarkReported = true;
                    ___noteJumpDidPassHalfEvent?.Invoke();
                }
                if (num2 >= 0.75f && !____threeQuartersMarkReported)
                {
                    ____threeQuartersMarkReported = true;
                    ___noteJumpDidPassThreeQuartersEvent?.Invoke(__instance);
                }
                if (songTime >= ____missedTime && !____missedMarkReported)
                {
                    ____missedMarkReported = true;
                    ___noteJumpDidPassMissedMarkerEvent?.Invoke();
                }
                if (____threeQuartersMarkReported)
                {
                    float num4 = (num2 - 0.75f) / 0.25f;
                    num4 = num4 * num4 * num4;
                    ____localPosition.z -= Mathf.LerpUnclamped(0f, ____endDistanceOffset, num4);
                }
                if (num2 >= 1f)
                {
                    if (!____missedMarkReported)
                    {
                        ____missedMarkReported = true;
                        ___noteJumpDidPassMissedMarkerEvent?.Invoke();
                    }
                    ___noteJumpDidFinishEvent?.Invoke();
                }
                Vector3 vector3 = ____worldRotation * ____localPosition;
                __instance.transform.localPosition = vector3;
                ___noteJumpDidUpdateProgressEvent?.Invoke(num2);

                __result = vector3;

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NoteMovement), nameof(NoteMovement.Init))]
    internal class Yeet6
    {
        static void Prefix(ref Vector3 moveStartPos, ref Vector3 moveEndPos, ref Vector3 jumpEndPos)
        {
            // Double notes swap become instant.
            if (Config.Instance.Enabled && Config.Instance.DisableFloorMovement && !Plugin.InReplay)
            {
                moveStartPos.x = jumpEndPos.x;
                moveEndPos.x = jumpEndPos.x;
            }
        }
    }
}
