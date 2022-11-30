using HarmonyLib;
using System;
using UnityEngine;

namespace NoteMovementFix.Patches
{
    // Remove the fixed spawn position
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

    // Fix NoteFloorMovement to match player position
    [HarmonyPatch(typeof(NoteFloorMovement), nameof(NoteFloorMovement.ManualUpdate))]
    internal class Yeet2
    {
        static bool Prefix(ref IAudioTimeSource ____audioTimeSyncController, ref float ____startTime, ref float ____moveDuration, ref Vector3 ____startPos, ref Vector3 ____endPos, ref Vector3 __result,
            ref Vector3 ____localPosition, ref NoteFloorMovement __instance, ref Quaternion ____worldRotation, ref Action ___floorMovementDidFinishEvent)
        {
            if (Config.Instance.Enabled && !Plugin.InReplay)
            {
                float num = ____audioTimeSyncController.songTime - ____startTime;
                ____localPosition = Vector3.Lerp(____startPos + new Vector3(0, 0, Camera.main.transform.position.z), ____endPos + new Vector3(0, 0, Camera.main.transform.position.z), num / ____moveDuration);
                Vector3 vector = ____worldRotation * ____localPosition;
                __instance.transform.localPosition = vector;
                if (num >= ____moveDuration)
                {
                    ___floorMovementDidFinishEvent?.Invoke();
                }
                __result = vector;

                return false;
            }

            return true;
        }
    }

    // Fix ObstacleController to match player position
    [HarmonyPatch(typeof(ObstacleController), nameof(ObstacleController.GetPosForTime))]
    internal class Yeet3
    {
        static bool Prefix(float time, ref Vector3 ____startPos, ref Vector3 ____midPos, ref Vector3 ____endPos, ref float ____move1Duration, ref float ____move2Duration,
            ref PlayerTransforms ____playerTransforms, ref Quaternion ____inverseWorldRotation, ref bool ____passedAvoidedMarkReported, ref float ____passedAvoidedMarkTime,
            ref float ____finishMovementTime, ref float ____endDistanceOffset, ref Vector3 __result)
        {
            if (Config.Instance.Enabled && !Plugin.InReplay)
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

    // Fix FlyingScore to match player position
    [HarmonyPatch(typeof(FlyingScoreSpawner), nameof(FlyingScoreSpawner.SpawnFlyingScore))]
    internal class Yeet4
    {
        static bool Prefix(ref IReadonlyCutScoreBuffer cutScoreBuffer, ref Color color, ref FlyingScoreSpawner __instance, ref FlyingScoreEffect.Pool ____flyingScoreEffectPool, ref FlyingScoreSpawner.InitData ____initData)
        {
            if (Config.Instance.Enabled && !Plugin.InReplay)
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
}
