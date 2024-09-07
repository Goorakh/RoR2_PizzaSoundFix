using BepInEx;
using RoR2;
using RoR2.EntityLogic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PizzaSoundFix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public class PizzaSoundFixPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "PizzaSoundFix";
        public const string PluginVersion = "1.0.0";

        internal static PizzaSoundFixPlugin Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            fixPizzaSoundEvent("RoR2/Base/Brother/BrotherUltLineGhost.prefab");
            fixPizzaSoundEvent("RoR2/Base/BrotherHaunt/BrotherUltLineGhost, Simple.prefab");

            static void fixPizzaSoundEvent(string path)
            {
                Addressables.LoadAssetAsync<GameObject>(path).Completed += handle =>
                {
                    GameObject ghostPrefab = handle.Result;

                    Transform earlySfx = ghostPrefab ? ghostPrefab.transform.Find("Size/EarlySFX") : null;
                    if (earlySfx)
                    {
                        fixOnEnableSoundEvent(earlySfx);
                    }
                    else
                    {
                        Log.Error($"Failed to find EarlySFX on '{path}'");
                    }
                };
            }

            stopwatch.Stop();
            Log.Message_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        }

        void OnDestroy()
        {
            Instance = SingletonHelper.Unassign(Instance, this);
        }

        static void fixOnEnableSoundEvent(Transform earlySfx)
        {
            // Presumably due to projectile ghost pooling, it seems that all StartEvents have been replaced by OnEnableEvents,
            // however, for this instance they copied the wrong event into the OnEnableEvent, causing the sound to play immediately

            DelayedEvent playSoundEvent = earlySfx.GetComponent<DelayedEvent>();
            if (!playSoundEvent)
            {
                Log.Error($"Failed to find DelayedEvent on {Util.BuildPrefabTransformPath(earlySfx.root, earlySfx, false, true)}");
                return;
            }

            OnEnableEvent onEnableEvent = earlySfx.GetComponent<OnEnableEvent>();
            if (!onEnableEvent)
            {
                Log.Error($"Failed to find EnableEvent on {Util.BuildPrefabTransformPath(earlySfx.root, earlySfx, false, true)}");
                return;
            }

            PersistentCallGroup persistentCalls = onEnableEvent.action.m_PersistentCalls;
            persistentCalls.Clear();
            int callIndex = persistentCalls.Count;
            persistentCalls.AddListener();
            persistentCalls.RegisterFloatPersistentListener(callIndex, playSoundEvent, typeof(DelayedEvent), 0.7f, nameof(DelayedEvent.CallDelayed));
        }
    }
}
