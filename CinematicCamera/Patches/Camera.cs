using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

namespace CinematicCamera.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class CameraPatch
    {
        internal static CinematicCameraBase CinematicCamera => CinematicCameraBase.Instance;
        internal static ManualLogSource Log => CinematicCamera.Log;

        internal static ConfigEntry<bool> PositionEffects = CinematicCameraBase.PositionEffects;
        internal static ConfigEntry<bool> RotationEffects = CinematicCameraBase.RotationEffects;

        internal static ConfigEntry<bool> HealthCondition = CinematicCameraBase.HealthCondition;
        internal static ConfigEntry<int> HealthConditionTriggerLimit = CinematicCameraBase.HealthConditionTriggerLimit;
        internal static ConfigEntry<bool> HealthConditionSmoothing = CinematicCameraBase.HealthConditionSmoothing;

        internal static ConfigEntry<bool> EnableMouseFactor = CinematicCameraBase.EnableMouseFactor;
        internal static ConfigEntry<float> MouseFactorSmoothness = CinematicCameraBase.MouseFactorSmoothness;
        internal static ConfigEntry<float> MouseFactorXMultiplier = CinematicCameraBase.MouseFactorXMultiplier;
        internal static ConfigEntry<float> MouseFactorYMultiplier = CinematicCameraBase.MouseFactorYMultiplier;
        internal static ConfigEntry<float> MouseFactorZMultiplier = CinematicCameraBase.MouseFactorZMultiplier;

        internal static ConfigEntry<bool> EnableFallingFactor = CinematicCameraBase.EnableFallingFactor;
        internal static ConfigEntry<float> FallingFactorSmoothness = CinematicCameraBase.FallingFactorSmoothness;
        internal static ConfigEntry<float> FallingFactorMultiplier = CinematicCameraBase.FallingFactorMultiplier;

        internal static ConfigEntry<bool> EnableMoveFactor = CinematicCameraBase.EnableMoveFactor;
        internal static ConfigEntry<float> MoveFactorSmoothness = CinematicCameraBase.MoveFactorSmoothness;
        internal static ConfigEntry<float> MoveFactorStrafeMultiplier = CinematicCameraBase.MoveFactorStrafeMultiplier;
        internal static ConfigEntry<float> MoveFactorForwardMultiplier = CinematicCameraBase.MoveFactorForwardMultiplier;
        internal static ConfigEntry<float> MoveFactorBackwardMultiplier = CinematicCameraBase.MoveFactorBackwardMultiplier;

        internal static ConfigEntry<bool> EnableFallShake = CinematicCameraBase.EnableFallShake;
        internal static ConfigEntry<float> FallShakeMultiplier = CinematicCameraBase.FallShakeMultiplier;
        internal static ConfigEntry<float> FallShakeMinVelocity = CinematicCameraBase.FallShakeMinVelocity;
        internal static ConfigEntry<float> FallShakeMaxVelocity = CinematicCameraBase.FallShakeMaxVelocity;

        private static PlayerControllerB Instance;

        private static Vector3 SavedCameraPosition = Vector3.zero;
        private static Quaternion SavedCameraRotation = Quaternion.identity;

        private static int inVehicleTicks = 0;
        private static bool isInVehicle = false;
        private static Vector3 prevLookVector = Vector3.zero;
        private static Vector3 lastLookVector = Vector3.zero;
        private static Vector3 prevVelocity = Vector3.zero;
        private static Vector3 lastVelocity = Vector3.zero;
        private static Vector2 prevMove = Vector2.zero;
        private static Vector2 lastMove = Vector2.zero;
        private static float fallShakeAmount = 0f;
        private static float fallShakeSmooth = 0f;
        private static Vector3 smoothLookVector = Vector3.zero;
        private static Vector3 smoothVelocity = Vector3.zero;
        private static Vector2 smoothMove = Vector2.zero;

        private static float Ticks = 0f;

        private static void SaveCameraState()
        {
            SavedCameraPosition = Instance.cameraContainerTransform.transform.position;
            SavedCameraRotation = Instance.cameraContainerTransform.transform.localRotation;
        }
        private static void RestoreCameraState()
        {
            if (SavedCameraPosition == Vector3.zero && SavedCameraRotation == Quaternion.identity)
            {
                Log.LogWarning("RestoreCameraState | Camera state is not saved yet. Skipping restoration.");
                return;
            }

            if (Instance == null)
            {
                Log.LogError("RestoreCameraState | Instance is null!");
                return;
            }

            if (Instance.cameraContainerTransform == null)
            {
                Log.LogError("RestoreCameraState | cameraContainerTransform is null!");
                return;
            }

            if (Instance.gameplayCamera == null)
            {
                Log.LogError("RestoreCameraState | gameplayCamera is null!");
                return;
            }

            if (PositionEffects != null && PositionEffects.Value == true)
            {
                Instance.cameraContainerTransform.transform.position = SavedCameraPosition;
            }

            if (RotationEffects != null && RotationEffects.Value == true)
            {
                Instance.cameraContainerTransform.transform.localRotation = SavedCameraRotation;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
        [HarmonyPrefix]
        private static void Awake(ref PlayerControllerB __instance)
        {
            Instance = __instance;
            Log.LogInfo("CameraPatch Awake called");

            // from https://thunderstore.io/c/lethal-company/p/kz/RemoveMotionSway/source/
            GameObject val = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel/ScavengerHelmet");
            MeshRenderer component = val.GetComponent<MeshRenderer>();
            ((Renderer)component).enabled = false;
        }

        [HarmonyPrefix, HarmonyPatch("Update")]
        private static void Update(ref PlayerControllerB __instance)
        {
            if (Instance == null)
            {
                Log.LogWarning("Instance is null in Update");
                Instance = __instance;  // fallback, but not ideal
                return;
            }
            Ticks += Time.deltaTime;

            RestoreCameraState();
        }

        [HarmonyPostfix, HarmonyPatch("LateUpdate")]
        private static void LateUpdate(ref PlayerControllerB __instance,
                                ref bool ___inTerminalMenu,
                                ref QuickMenuManager ___quickMenuManager,
                                ref bool ___isTypingChat,
                                ref bool ___disableMoveInput,
                                ref bool ___inSpecialInteractAnimation,
                                ref bool ___isClimbingLadder,
                                ref bool ___inShockingMinigame,
                                ref bool ___inVehicleAnimation,
                                ref int ___health)
        {
            SaveCameraState();

            prevLookVector = lastLookVector;
            lastLookVector = __instance.playerActions.Movement.Look.ReadValue<Vector2>();

            prevVelocity = lastVelocity;
            lastVelocity = __instance.thisController.velocity;

            prevMove = lastMove;
            lastMove = IngamePlayerSettings.Instance.playerInput
                             .actions.FindAction("Move", false)
                             .ReadValue<Vector2>();

            if (___inVehicleAnimation)
            {
                inVehicleTicks = 10;
            } else
            {
                inVehicleTicks--;
            }

            if (inVehicleTicks > 0)
            {
                isInVehicle = true;
            } else
            {
                isInVehicle = false;
            }

            if (___inTerminalMenu | ___quickMenuManager.isMenuOpen | ___isTypingChat | ___disableMoveInput | ___inSpecialInteractAnimation | ___isClimbingLadder | ___inShockingMinigame | isInVehicle) { return; }

            Transform camTransform = __instance.cameraContainerTransform;

            // MOUSE FACTOR
            if (EnableMouseFactor.Value)
            {
                if (MouseFactorSmoothness.Value > 0)
                {
                    smoothLookVector = Vector3.Lerp(smoothLookVector, lastLookVector, (Time.deltaTime * 1.4f) / MouseFactorSmoothness.Value);
                }
                else
                {
                    smoothLookVector = lastLookVector;
                }

                camTransform.localRotation = camTransform.localRotation * Quaternion.Euler(-smoothLookVector.y * 0.014f * MouseFactorXMultiplier.Value, smoothLookVector.x * 0.01f * MouseFactorYMultiplier.Value, smoothLookVector.x * 0.018f * MouseFactorZMultiplier.Value);
            }

            // FALLING FACTOR
            if (EnableFallingFactor.Value)
            {
                if (FallingFactorSmoothness.Value > 0)
                {
                    smoothVelocity = Vector3.Lerp(smoothVelocity, lastVelocity, (Time.deltaTime * 2.8f) / FallingFactorSmoothness.Value);
                }
                else
                {
                    smoothVelocity = lastVelocity;
                }

                camTransform.localRotation = camTransform.localRotation * Quaternion.Euler(smoothVelocity.y, 0, 0);
            }

            // MOVE FACTOR
            if (EnableMoveFactor.Value)
            {
                if (MoveFactorSmoothness.Value > 0)
                {
                    smoothMove = Vector2.Lerp(smoothMove, lastMove, (Time.deltaTime * 1.4f) / MoveFactorSmoothness.Value);
                }
                else
                {
                    smoothMove = lastMove;
                }

                // Strafing
                if (Math.Abs(smoothMove.x) > 0.1f)
                {
                    camTransform.localRotation = camTransform.localRotation * Quaternion.Euler(0, 0, -smoothMove.x * MoveFactorStrafeMultiplier.Value);
                }

                // Forward/Backward
                if (smoothMove.y > 0.1f)
                {
                    camTransform.localRotation = camTransform.localRotation * Quaternion.Euler(smoothMove.y * 0.8f * MoveFactorForwardMultiplier.Value, 0, 0);
                }
                else if (smoothMove.y < -0.1f)
                {
                    camTransform.localRotation = camTransform.localRotation * Quaternion.Euler(smoothMove.y * 0.8f * MoveFactorBackwardMultiplier.Value, 0, 0);
                }
            }

            // FALL SHAKE
            if (EnableFallShake.Value)
            {
                float shakeDif = (lastVelocity.y - prevVelocity.y);

                if ((shakeDif > FallShakeMinVelocity.Value) & (lastVelocity.y < 0.1f) & (lastVelocity.y > -0.1f))
                {
                    float shakePowerNormalized = Mathf.InverseLerp(FallShakeMinVelocity.Value, FallShakeMaxVelocity.Value, shakeDif);
                    shakePowerNormalized = Mathf.Clamp(shakePowerNormalized, 0, 1);
                    fallShakeAmount = shakePowerNormalized;
                    fallShakeSmooth = shakePowerNormalized;
                }

                fallShakeSmooth = Mathf.Lerp(fallShakeSmooth, 0, Time.deltaTime * 1f);
                camTransform.localPosition = camTransform.localPosition + UnityEngine.Random.insideUnitSphere * (fallShakeSmooth * 0.2f * FallShakeMultiplier.Value);
            }

            // CONDITIONS
            if (HealthCondition.Value == false)
            {
                return;
            } else {
                // HEALTH CONDITION
                float HealthConditionValue;
                if (HealthConditionSmoothing.Value == true)
                {
                    if (___health <= HealthConditionTriggerLimit.Value)
                    {
                        HealthConditionValue = 1f - (___health / HealthConditionTriggerLimit.Value);
                    } else
                    {
                        HealthConditionValue = 0f;
                    }
                } else {
                    if (___health <= HealthConditionTriggerLimit.Value)
                    {
                        HealthConditionValue = 1f;
                    } else
                    {
                        HealthConditionValue = 0f;
                    }
                }

                // FINAL CONDITIONS CODE
                float conditions = HealthConditionValue;
                if (conditions > 0)
                {
                    camTransform.localPosition = Vector3.Lerp(SavedCameraPosition, camTransform.localPosition, conditions);
                    camTransform.localRotation = Quaternion.Slerp(SavedCameraRotation, camTransform.localRotation, conditions);
                } else
                {
                    RestoreCameraState();
                }
            }
        }
    }
}
