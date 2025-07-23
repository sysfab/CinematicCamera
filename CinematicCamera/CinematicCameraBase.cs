using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using CinematicCamera.Patches;
using UnityEngine;

namespace CinematicCamera
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class CinematicCameraBase : BaseUnityPlugin
    {
        private const string modGUID = "sysfab.cinematiccamera";
        private const string modName = "Cinematic Camera";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static CinematicCameraBase Instance;

        internal ManualLogSource Log;

        internal static ConfigEntry<bool> PositionEffects;
        internal static ConfigEntry<bool> RotationEffects;

        internal static ConfigEntry<bool> HealthCondition;
        internal static ConfigEntry<int> HealthConditionTriggerLimit;
        internal static ConfigEntry<bool> HealthConditionSmoothing;

        internal static ConfigEntry<bool> EnableMouseFactor;
        internal static ConfigEntry<float> MouseFactorSmoothness;
        internal static ConfigEntry<float> MouseFactorXMultiplier;
        internal static ConfigEntry<float> MouseFactorYMultiplier;
        internal static ConfigEntry<float> MouseFactorZMultiplier;
        internal static ConfigEntry<float> MouseFactorRunningMultiplier;

        internal static ConfigEntry<bool> EnableFallingFactor;
        internal static ConfigEntry<float> FallingFactorSmoothness;
        internal static ConfigEntry<float> FallingFactorMultiplier;

        internal static ConfigEntry<bool> EnableMoveFactor;
        internal static ConfigEntry<float> MoveFactorSmoothness;
        internal static ConfigEntry<float> MoveFactorStrafeMultiplier;
        internal static ConfigEntry<float> MoveFactorForwardMultiplier;
        internal static ConfigEntry<float> MoveFactorBackwardMultiplier;
        internal static ConfigEntry<float> MoveFactorRunningMultiplier;

        internal static ConfigEntry<bool> EnableFallShake;
        internal static ConfigEntry<float> FallShakeMultiplier;
        internal static ConfigEntry<float> FallShakeMinVelocity;
        internal static ConfigEntry<float> FallShakeMaxVelocity;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Log = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            Log.LogInfo($"Loading config entries...");
            PositionEffects = Config.Bind("Effects", "Position Effects", true, "Mod will change camera's position if enabled");
            RotationEffects = Config.Bind("Effects", "Rotation Effects", true, "Mod will change camera's rotation if enabled");

            HealthCondition = Config.Bind("Health Condition", "Enabled", false, "Mod will work if health condition (or others) is met");
            HealthConditionTriggerLimit = Config.Bind("Health Condition", "Trigger limit", 20, "If health is <= trigger value then mod will start to work");
            HealthConditionSmoothing = Config.Bind("Health Condition", "Smoothing", false, "If health is close to trigger value then effect will be small");

            EnableMouseFactor = Config.Bind("Mouse Factor", "Enabled", true, "Moving mouse will affect camera");
            MouseFactorSmoothness = Config.Bind("Mouse Factor", "Smoothness", 1.0f, "Smoothness of mouse factor, Default: 1. Values from 0.5 to 2 recommended");
            MouseFactorXMultiplier = Config.Bind("Mouse Factor", "X Multiplier", 1.0f, "X axis multiplier, Default: 1");
            MouseFactorYMultiplier = Config.Bind("Mouse Factor", "Y Multiplier", 1.0f, "Y axis multiplier, Default: 1");
            MouseFactorZMultiplier = Config.Bind("Mouse Factor", "Z Multiplier", 1.0f, "Z axis multiplier, Default: 1");
            MouseFactorRunningMultiplier = Config.Bind("Mouse Factor", "Running Multiplier", 1.0f, "Running multiplier, Default: 1");

            EnableFallingFactor = Config.Bind("Falling Factor", "Enabled", true, "Falling will affect camera");
            FallingFactorSmoothness = Config.Bind("Falling Factor", "Smoothness", 1.0f, "Smoothness of falling factor, Default: 1. Values from 0.5 to 2 recommended");
            FallingFactorMultiplier = Config.Bind("Falling Factor", "Multiplier", 1.0f, "Falling effect multiplier, Default: 1");

            EnableMoveFactor = Config.Bind("Move Factor", "Enabled", true, "Moving will affect camera");
            MoveFactorSmoothness = Config.Bind("Move Factor", "Smoothness", 1.0f, "Smoothness of move factor, Default: 1. Values from 0.5 to 2 recommended");
            MoveFactorStrafeMultiplier = Config.Bind("Move Factor", "Strafe Multiplier", 1.0f, "Strafe multiplier, Default: 1");
            MoveFactorForwardMultiplier = Config.Bind("Move Factor", "Forward Multiplier", 1.0f, "Forward multiplier, Default: 1");
            MoveFactorBackwardMultiplier = Config.Bind("Move Factor", "Backward Multiplier", 1.0f, "Backward multiplier, Default: 1");
            MoveFactorRunningMultiplier = Config.Bind("Move Factor", "Running Multiplier", 1.0f, "Running multiplier, Default: 1");

            EnableFallShake = Config.Bind("Fall Shake", "Enabled", true, "Falling will shake camera");
            FallShakeMultiplier = Config.Bind("Fall Shake", "Multiplier", 1.0f, "Falling shake multiplier, Default: 1");
            FallShakeMinVelocity = Config.Bind("Fall Shake", "Min Velocity", 15.0f, "Minimum velocity to shake camera, Default: 15");
            FallShakeMaxVelocity = Config.Bind("Fall Shake", "Max Velocity", 500.0f, "Maximum velocity to shake camera, Default: 500");
            Log.LogInfo($"Config entries loaded!");

            Log.LogInfo($"{modName} is loaded!");

            harmony.PatchAll(typeof(CinematicCameraBase));
            harmony.PatchAll(typeof(CameraPatch));
        }
    }
}
