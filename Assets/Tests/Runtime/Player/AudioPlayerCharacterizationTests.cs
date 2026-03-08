// =============================================================================
// AudioPlayerCharacterizationTests.cs
//
// CHARACTERIZATION TESTS for AudioPlayer (partial class, 6 files).
// Goal: lock down CURRENT behavior exactly — bugs and all.
//       Do NOT interpret these as specifications of correct behavior.
//
// Test framework : Unity Test Framework (Play Mode)
// Assembly setup : Requires a Play Mode test .asmdef that references BroAudio.
//                  Internal members are accessed via System.Reflection because
//                  AudioPlayer has no InternalsVisibleTo for a test assembly.
//
// SoundManager dependency note
// ─────────────────────────────
// AudioPlayer.PlayInternal(), EndPlaying(), and Recycle() all call
// SoundManager.Instance (a concrete singleton with no injection point).
// Tests that trigger those paths call SetupStubSoundManager(), which
// invokes SoundManager.Init() to load the real prefab from Resources
// (BroAudioMixer and AudioPlayer prefab are already wired in the prefab,
// so Awake succeeds without errors or LogAssert expectations).
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using Ami.BroAudio;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using Ami.Extension;

namespace Ami.BroAudio.Tests
{
    [TestFixture]
    public class AudioPlayerCharacterizationTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private GameObject _go;
        private AudioPlayer _player;
        private GameObject _soundManagerGo;

        /// <summary>
        /// Write a private/internal instance field on a target instance via reflection.
        /// Needed by sections 18, 19, 20, 24, 26.
        /// </summary>
        private static void SetField<TTarget, TValue>(TTarget target, string name, TValue value)
        {
            var field = typeof(TTarget).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{name}' not found on {nameof(TTarget)} — has it been renamed?");
            field.SetValue(target, value);
        }

        /// <summary>
        /// Invoke an explicit interface implementation method on target instance via
        /// GetInterfaceMap, which maps interface slots to concrete target methods.
        /// Needed by sections 20, 21, 26.
        /// </summary>
        private static object InvokeInterfaceMethod<T>(T instance, Type interfaceType, string name, params object[] args)
        {
            var map = typeof(AudioPlayer).GetInterfaceMap(interfaceType);
            MethodInfo targetMethod = null;
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i].Name == name)
                {
                    targetMethod = map.TargetMethods[i];
                    break;
                }
            }
            Assert.IsNotNull(targetMethod, $"Method '{interfaceType.Name}.{name}' not found on {nameof(T)}");
            return targetMethod.Invoke(instance, args);
        }

        /// <summary>
        /// Read a private/internal instance field from a target instance via reflection.
        /// </summary>
        private static TValue GetField<TTarget,TValue>(TTarget target, string name)
        {
            var field = typeof(TTarget).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{name}' not found on {nameof(TTarget)} — has it been renamed?");
            return (TValue)field.GetValue(target);
        }

        /// <summary>
        /// Call an internal/private instance method on AudioPlayer via reflection.
        /// </summary>
        private static object InvokeMethod(AudioPlayer player, string name, params object[] args)
        {
            var method = typeof(AudioPlayer).GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, $"Method '{name}' not found on AudioPlayer — has it been renamed?");
            return method.Invoke(player, args);
        }

        /// <summary>
        /// Create a minimal AudioEntity ScriptableObject usable for constructing a SoundID.
        /// Does NOT set Clips, AudioAsset, or any SoundManager-dependent data.
        /// </summary>
        private static AudioEntity MakeEntity(string entityName = "TestEntity")
        {
            var entity = ScriptableObject.CreateInstance<AudioEntity>();
            entity.name = entityName;
            return entity;
        }

        /// <summary>
        /// Initializes a real SoundManager singleton by loading the prefab from Resources.
        /// The prefab already has BroAudioMixer and AudioPlayer prefab serialized, so Awake
        /// succeeds without errors. Must be paired with TearDownStubSoundManager (handled
        /// automatically by TearDown).
        /// </summary>
        private void SetupStubSoundManager()
        {
            if (SoundManager.HasInstance)
            {
                return;
            }
            SoundManager.Init();
            var sm = typeof(SoundManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null) as SoundManager;
            _soundManagerGo = sm != null ? sm.gameObject : null;
        }

        private void TearDownStubSoundManager()
        {
            if (_soundManagerGo != null)
            {
                typeof(SoundManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(_soundManagerGo);
                _soundManagerGo = null;
            }
        }

        /// <summary>
        /// Creates an AudioEntity with a test AudioClip set via reflection.
        /// Caller must call DestroyEntityWithClip to clean up.
        /// </summary>
        private static AudioEntity MakeEntityWithClip(BroAudioType audioType = BroAudioType.SFX,
            int sampleLength = 44100, float masterVolume = 1f)
        {
            var entity = ScriptableObject.CreateInstance<AudioEntity>();
            entity.name = "TestEntityWithClip";

            SetAutoProperty(entity, "AudioType", audioType);
            SetAutoProperty(entity, "MasterVolume", masterVolume);
            SetAutoProperty(entity, "Pitch", AudioConstant.DefaultPitch);

            var testClip = AudioClip.Create("TestClip", sampleLength, 1, 44100, false);

            var broClip = new BroAudioClip();
            typeof(BroAudioClip).GetField("AudioClip", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(broClip, testClip);
            broClip.Volume = AudioConstant.FullVolume;

            entity.Clips = new[] { broClip };
            return entity;
        }

        private static void SetAutoProperty<T>(object obj, string propertyName, T value)
        {
            var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Backing field for '{propertyName}' not found");
            field.SetValue(obj, value);
        }

        private static void DestroyEntityWithClip(AudioEntity entity)
        {
            if (entity != null)
            {
                if (entity.Clips != null)
                {
                    foreach (var clip in entity.Clips)
                    {
                        var audioClip = clip.GetAudioClip();
                        if (audioClip != null)
                            UnityEngine.Object.DestroyImmediate(audioClip);
                    }
                }
                UnityEngine.Object.DestroyImmediate(entity);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestAudioPlayer");
            _player = _go.AddComponent<AudioPlayer>();
            var audioSource = _player.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
            TearDownStubSoundManager();
        }

        // =====================================================================
        // 1. INITIAL STATE
        //    Every property on a freshly constructed (Awake'd) AudioPlayer.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void ID_OnFreshPlayer_IsInvalid()
        {
            Assert.IsFalse(_player.ID.IsValid());
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsActive_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsActive);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsPlaying_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsPlaying);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsStopping_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsStopping);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsFadingOut_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsFadingOut);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void CurrentActiveTrackEffects_OnFreshPlayer_IsNone()
        {
            Assert.AreEqual(EffectType.None, _player.CurrentActiveTrackEffects);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsUsingTrackEffect_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsUsingTrackEffect);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // StaticPitch starts at 1.0 (AudioConstant.DefaultPitch), NOT 0.
        public void StaticPitch_OnFreshPlayer_IsDefaultPitch()
        {
            Assert.AreEqual(AudioConstant.DefaultPitch, _player.StaticPitch);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void PlaybackStartingTime_OnFreshPlayer_IsZero()
        {
            Assert.AreEqual(0, _player.PlaybackStartingTime);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // HasStartedPlaying is defined as PlaybackStartingTime > 0 — sentinel contract.
        public void HasStartedPlaying_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.HasStartedPlaying);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void CurrentPlayingClip_OnFreshPlayer_IsNull()
        {
            Assert.IsNull(_player.CurrentPlayingClip);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsBGM_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsBGM);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void IsDominator_OnFreshPlayer_IsFalse()
        {
            Assert.IsFalse(_player.IsDominator);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // OnPlaybackHandover starts as null; it is a public field, not an event.
        public void OnPlaybackHandover_OnFreshPlayer_IsNull()
        {
            Assert.IsNull(_player.OnPlaybackHandover);
        }

        // =====================================================================
        // 2. SetPlaybackData
        //    Sets ID and _pref without touching SoundManager.
        //    NOTE: PlaybackPreference's constructor calls SoundManager.FadeInEase,
        //    so we pass default(PlaybackPreference) (zero-init, no ctor call).
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void SetPlaybackData_WithValidEntity_SetsIDAndMakesIsActiveTrue()
        {
            var entity = MakeEntity();
            var id = new SoundID(entity);

            _player.SetPlaybackData(id, default(PlaybackPreference), null);

            // IsActive = ID.IsValid() = entity != null
            Assert.IsTrue(_player.IsActive);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void SetPlaybackData_WithInvalidID_IsActiveRemainsFalse()
        {
            _player.SetPlaybackData(SoundID.Invalid, default(PlaybackPreference), null);

            Assert.IsFalse(_player.IsActive);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void SetPlaybackData_SetsIDProperty()
        {
            var entity = MakeEntity();
            var id = new SoundID(entity);

            _player.SetPlaybackData(id, default(PlaybackPreference), null);

            Assert.AreEqual(id, _player.ID);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetPlaybackData called twice overwrites with the second ID.
        public void SetPlaybackData_CalledTwice_OverwritesWithSecondID()
        {
            var entityA = MakeEntity("A");
            var entityB = MakeEntity("B");

            _player.SetPlaybackData(new SoundID(entityA), default(PlaybackPreference), null);
            _player.SetPlaybackData(new SoundID(entityB), default(PlaybackPreference), null);

            Assert.AreEqual(new SoundID(entityB), _player.ID);
            UnityEngine.Object.DestroyImmediate(entityA);
            UnityEngine.Object.DestroyImmediate(entityB);
        }

        // =====================================================================
        // 3. IsActive / IsUsingTrackEffect — computed property characterization
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // IsActive is purely derived from ID.IsValid() (entity != null).
        // It does NOT reflect whether audio is actually outputting sound.
        public void IsActive_ReflectsOnlyWhetherIDEntityIsNonNull()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            Assert.IsTrue(_player.IsActive, "valid entity → IsActive true");

            _player.SetPlaybackData(SoundID.Invalid, default(PlaybackPreference), null);
            Assert.IsFalse(_player.IsActive, "Invalid SoundID → IsActive false");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // IsUsingTrackEffect is purely derived from CurrentActiveTrackEffects != EffectType.None.
        // Calling SetTrackEffect with a valid ID + mixer would change it, but we test the
        // derived relationship here using the public bitmask property.
        public void IsUsingTrackEffect_IsFalseWhenCurrentActiveTrackEffectsIsNone()
        {
            // CurrentActiveTrackEffects is None on a fresh player
            Assert.AreEqual(EffectType.None, _player.CurrentActiveTrackEffects);
            Assert.IsFalse(_player.IsUsingTrackEffect);
        }

        // =====================================================================
        // 4. Play() guard conditions
        //    Play() has three early-return guards before calling PlayInternal():
        //    IsStopping, IsOnHold (_stopMode==Pause && !HasStartedPlaying),
        //    and _pref.ScheduledStartTime > 0.
        //    PlayInternal() itself has a guard: !ID.IsValid() → logs error + returns.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // On a fresh player ID is invalid → PlayInternal logs an error but does not throw.
        public void Play_OnFreshPlayerWithInvalidID_LogsErrorAndDoesNotThrow()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Cannot play audio"));

            Assert.DoesNotThrow(() => _player.Play());
        }

        // =====================================================================
        // 5. Stop() SoundManager-free guard paths
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop(Pause) when AudioSource is not playing AND _stopMode is not already Pause:
        //   • sets _stopMode = Pause internally
        //   • fires _onPaused immediately (synchronously)
        //   • does NOT set IsStopping
        //   • does NOT call EndPlaying / Recycle (no SoundManager hit)
        public void Stop_PauseModeWhenNotPlaying_SetsStopModeAndFiresOnPausedSynchronously()
        {
            int pausedCallCount = 0;
            _player.OnPause(_ => pausedCallCount++);

            _player.Stop(0f, StopMode.Pause, null);

            Assert.IsFalse(_player.IsStopping, "IsStopping must remain false; no coroutine started");
            Assert.AreEqual(1, pausedCallCount, "OnPaused must fire synchronously on first Pause stop");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // A second Stop(Pause) call while _stopMode is already Pause:
        //   • does NOT re-fire _onPaused (hasPaused guard)
        public void Stop_PauseModeSecondCall_DoesNotRefireOnPaused()
        {
            int pausedCallCount = 0;
            _player.OnPause(_ => pausedCallCount++);

            _player.Stop(0f, StopMode.Pause, null); // hasPaused = false → fires
            _player.Stop(0f, StopMode.Pause, null); // hasPaused = true  → skips

            Assert.AreEqual(1, pausedCallCount,
                "OnPaused must NOT fire a second time when _stopMode is already Pause");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop() while IsStopping=false does not throw regardless of parameters.
        // Requires both a stub SoundManager (Fader.StopCoroutine calls SoundManager.Instance
        // via _coroutineExecutor) and a NullMixerPool (Recycle calls MixerPool.ReturnPlayer).
        public void Stop_WhileNotStopping_DoesNotThrow()
        {
            SetupStubSoundManager();
            _player.MixerPool = new NullMixerPool();
            Assert.DoesNotThrow(() => _player.Stop(0f, StopMode.Stop, null));
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // UnPause() when _stopMode is not Pause logs a warning and returns without calling PlayInternal.
        public void UnPause_WhenNotPaused_LogsWarningAndDoesNotPlay()
        {
            // _stopMode defaults to StopMode.Stop (0), which is != Pause
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Cannot UnPause"));

            IAudioStoppable stoppable = _player;
            Assert.DoesNotThrow(() => stoppable.UnPause());
        }

        // =====================================================================
        // 6. Event subscription idempotency
        //    OnEnd / OnStart / OnUpdate / OnPause all use the pattern:
        //        _event -= handler; _event += handler;
        //    This means the same handler registered twice fires only once.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Registering the same OnEnd callback twice fires it only once (idempotent subscribe).
        public void OnEnd_SameCallbackRegisteredTwice_FiresOnlyOnce()
        {
            int callCount = 0;
            Action<SoundID> callback = _ => callCount++;

            _player.OnEnd(callback);
            _player.OnEnd(callback); // removes then re-adds → net: 1 subscriber

            // Invoke the private backing field directly to observe the call count.
            var field = typeof(AudioPlayer).GetField(
                "_onEnd", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "_onEnd field not found — has it been renamed?");
            (field.GetValue(_player) as Action<SoundID>)?.Invoke(SoundID.Invalid);

            Assert.AreEqual(1, callCount,
                "Idempotent subscribe pattern (−= then +=) must prevent duplicate invocations");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnStart_SameCallbackRegisteredTwice_FiresOnlyOnce()
        {
            int callCount = 0;
            Action<IAudioPlayer> callback = _ => callCount++;

            _player.OnStart(callback);
            _player.OnStart(callback);

            var field = typeof(AudioPlayer).GetField(
                "_onStart", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "_onStart field not found");
            (field.GetValue(_player) as Action<IAudioPlayer>)?.Invoke(_player);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnUpdate_SameCallbackRegisteredTwice_FiresOnlyOnce()
        {
            int callCount = 0;
            Action<IAudioPlayer> callback = _ => callCount++;

            _player.OnUpdate(callback);
            _player.OnUpdate(callback);

            var field = typeof(AudioPlayer).GetField(
                "_onUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "_onUpdate field not found");
            (field.GetValue(_player) as Action<IAudioPlayer>)?.Invoke(_player);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnPause_SameCallbackRegisteredTwice_FiresOnlyOnce()
        {
            int callCount = 0;
            Action<IAudioPlayer> callback = _ => callCount++;

            _player.OnPause(callback);
            _player.OnPause(callback);

            var field = typeof(AudioPlayer).GetField(
                "_onPaused", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "_onPaused field not found");
            (field.GetValue(_player) as Action<IAudioPlayer>)?.Invoke(_player);

            Assert.AreEqual(1, callCount);
        }

        // =====================================================================
        // 7. Fluent API — all On* and SetFade*Ease return 'this'
        //    This allows chaining. Tests verify identity, not just type.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnEnd_ReturnsThisInstance()
        {
            IAudioPlayer result = _player.OnEnd(_ => { });
            Assert.AreSame(_player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnStart_ReturnsThisInstance()
        {
            IAudioPlayer result = _player.OnStart(_ => { });
            Assert.AreSame(_player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnUpdate_ReturnsThisInstance()
        {
            IAudioPlayer result = _player.OnUpdate(_ => { });
            Assert.AreSame(_player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void OnPause_ReturnsThisInstance()
        {
            IAudioPlayer result = _player.OnPause(_ => { });
            Assert.AreSame(_player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void SetFadeInEase_ReturnsThisInstance()
        {
            IAudioPlayer result = _player.SetFadeInEase(Ease.Linear);
            Assert.AreSame(_player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void SetFadeOutEase_ReturnsThisInstance()
        {
            IAudioPlayer result = _player.SetFadeOutEase(Ease.Linear);
            Assert.AreSame(_player, result);
        }

        // =====================================================================
        // 8. Internal Transfer methods
        //    These are internal and are accessed via reflection.
        //    They are used by AudioPlayerInstanceWrapper.UpdateInstance() to
        //    hand delegates from one AudioPlayer to another during loop handover.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // TransferOnUpdates with no subscribers: returns false, out is null.
        public void TransferOnUpdates_WithNoSubscribers_ReturnsFalseAndNullOut()
        {
            var method = typeof(AudioPlayer).GetMethod(
                "TransferOnUpdates", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, "TransferOnUpdates not found");

            var args = new object[] { null };
            bool result = (bool)method.Invoke(_player, args);
            var delegates = args[0] as Delegate[];

            Assert.IsFalse(result);
            Assert.IsNull(delegates);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // TransferOnUpdates with a subscriber: returns true, out has 1 element,
        // AND clears the backing event so a second transfer returns false.
        public void TransferOnUpdates_WithSubscriber_ReturnsTrueAndClearsSource()
        {
            _player.OnUpdate(_ => { });

            var method = typeof(AudioPlayer).GetMethod(
                "TransferOnUpdates", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var args = new object[] { null };
            bool firstResult = (bool)method.Invoke(_player, args);
            var delegates = (Delegate[])args[0];

            Assert.IsTrue(firstResult);
            Assert.IsNotNull(delegates);
            Assert.AreEqual(1, delegates.Length);

            // Source event must be null after transfer
            var args2 = new object[] { null };
            bool secondResult = (bool)method.Invoke(_player, args2);
            Assert.IsFalse(secondResult, "After transfer, source event must be cleared");
            Assert.IsNull(args2[0]);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void TransferOnEnds_WithNoSubscribers_ReturnsFalseAndNullOut()
        {
            var method = typeof(AudioPlayer).GetMethod(
                "TransferOnEnds", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var args = new object[] { null };
            bool result = (bool)method.Invoke(_player, args);

            Assert.IsFalse(result);
            Assert.IsNull(args[0]);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void TransferOnEnds_WithSubscriber_ReturnsTrueAndClearsSource()
        {
            _player.OnEnd(_ => { });

            var method = typeof(AudioPlayer).GetMethod(
                "TransferOnEnds", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var args = new object[] { null };
            bool result = (bool)method.Invoke(_player, args);
            Assert.IsTrue(result);
            Assert.IsNotNull(args[0]);

            var args2 = new object[] { null };
            Assert.IsFalse((bool)method.Invoke(_player, args2));
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void TransferOnPauses_WithNoSubscribers_ReturnsFalseAndNullOut()
        {
            var method = typeof(AudioPlayer).GetMethod(
                "TransferOnPauses", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var args = new object[] { null };
            bool result = (bool)method.Invoke(_player, args);

            Assert.IsFalse(result);
            Assert.IsNull(args[0]);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void TransferOnPauses_WithSubscriber_ReturnsTrueAndClearsSource()
        {
            _player.OnPause(_ => { });

            var method = typeof(AudioPlayer).GetMethod(
                "TransferOnPauses", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var args = new object[] { null };
            bool result = (bool)method.Invoke(_player, args);
            Assert.IsTrue(result);

            var args2 = new object[] { null };
            Assert.IsFalse((bool)method.Invoke(_player, args2));
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // TransferDecorators with no decorators: returns false, out is null.
        public void TransferDecorators_WithNoDecorators_ReturnsFalseAndNullOut()
        {
            var method = typeof(AudioPlayer).GetMethod(
                "TransferDecorators", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var args = new object[] { null };
            bool result = (bool)method.Invoke(_player, args);

            Assert.IsFalse(result);
            Assert.IsNull(args[0]);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // TransferDecorators nulls out the _decorators field on the source player.
        // After transfer the source player has no decorator list at all.
        public void TransferDecorators_AfterTransfer_NullsSourceDecoratorsList()
        {
            // Manually inject a non-null (but empty) decorator list via reflection
            var field = typeof(AudioPlayer).GetField(
                "_decorators", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "_decorators field not found");
            field.SetValue(_player, new List<AudioPlayerDecorator>());

            var method = typeof(AudioPlayer).GetMethod(
                "TransferDecorators", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var args = new object[] { null };
            method.Invoke(_player, args);

            // Source _decorators must be null after transfer
            Assert.IsNull(field.GetValue(_player),
                "_decorators on source must be null after TransferDecorators");
        }

        // =====================================================================
        // 9. SetTrackEffect guards
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Guard: !ID.IsValid() → early return, no state change.
        public void SetTrackEffect_WithInvalidID_IsNoOpAndLeavesEffectsAtNone()
        {
            _player.SetTrackEffect(EffectType.Volume, SetEffectMode.Add);

            Assert.AreEqual(EffectType.None, _player.CurrentActiveTrackEffects);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Guard: effect == EffectType.None && mode != Override → early return.
        // Even with a valid ID the guard fires before any bitmask update.
        public void SetTrackEffect_NoneEffectWithAddMode_IsNoOp()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            _player.SetTrackEffect(EffectType.None, SetEffectMode.Add);

            Assert.AreEqual(EffectType.None, _player.CurrentActiveTrackEffects);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Guard: effect == EffectType.None WITH Override mode is NOT blocked
        // by the "None + non-Override" guard — it proceeds. But it then hits the
        // TryGetMixerAndTrack guard (no mixer on fresh player) and still returns early.
        // Net observable result: CurrentActiveTrackEffects stays None.
        public void SetTrackEffect_NoneEffectWithOverrideMode_StillLeavesEffectsAtNone()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            // No AudioMixerGroup assigned → TryGetMixerAndTrack returns false → early return
            _player.SetTrackEffect(EffectType.None, SetEffectMode.Override);

            Assert.AreEqual(EffectType.None, _player.CurrentActiveTrackEffects);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 10. IAudioPlayer.AudioSource property
        //     Returns Empty.AudioSource (not null!) and logs an error when inactive.
        //     This is an explicit interface implementation — must cast to IAudioPlayer.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // When IsActive is false, AudioSource property returns the shared Empty.AudioSource
        // singleton AND emits a LogError. It does NOT return null.
        public void AudioSourceProperty_WhenNotActive_ReturnsEmptyAudioSourceAndLogsError()
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("audio player is not playing"));

            IAudioPlayer player = _player;
            IAudioSourceProxy proxy = player.AudioSource;

            Assert.AreSame(Empty.AudioSource, proxy,
                "Inactive player must return Empty.AudioSource, not null");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Second access after the first creates/reuses the same proxy (lazy init via ??=).
        // Both calls return the same Empty.AudioSource when inactive.
        public void AudioSourceProperty_AccessedTwiceWhileInactive_ReturnsSameEmptyInstance()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));

            IAudioPlayer player = _player;
            var first = player.AudioSource;
            var second = player.AudioSource;

            Assert.AreSame(first, second);
        }

        // =====================================================================
        // 11. AddAudioEffect / RemoveAudioEffect guard paths
        //     Both are explicit interface implementations (IAudioPlayer).
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // AddAudioEffect on an inactive player logs an error and returns 'this' (fluent).
        public void AddAudioEffect_WhenNotActive_LogsErrorAndReturnsSelf()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Cannot add"));

            IAudioPlayer player = _player;
            IAudioPlayer result = player.AddLowPassEffect();

            // Returns 'this' (the AudioPlayer), not Empty.AudioPlayer
            Assert.AreSame(player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // RemoveAudioEffect on an inactive player logs an error and returns 'this'.
        public void RemoveAudioEffect_WhenNotActive_LogsErrorAndReturnsSelf()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Cannot remove"));

            IAudioPlayer player = _player;
            IAudioPlayer result = player.RemoveLowPassEffect();

            Assert.AreSame(player, result);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // RemoveAudioEffect when active but effect list is empty logs a warning.
        public void RemoveAudioEffect_WhenActiveButNoEffects_LogsWarningAndReturnsSelf()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No effects to remove"));

            IAudioPlayer player = _player;
            IAudioPlayer result = player.RemoveLowPassEffect();

            Assert.AreSame(player, result);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 12. GetOutputData / GetSpectrumData — passthrough to AudioSource
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // GetOutputData is a direct passthrough; does not throw on a non-playing source.
        public void GetOutputData_WhenNotPlaying_DoesNotThrow()
        {
            var samples = new float[256];
            Assert.DoesNotThrow(() => _player.GetOutputData(samples, 0));
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        public void GetSpectrumData_WhenNotPlaying_DoesNotThrow()
        {
            var samples = new float[256];
            Assert.DoesNotThrow(() =>
                _player.GetSpectrumData(samples, 0, FFTWindow.Rectangular));
        }

        // =====================================================================
        // 13. UpdateVolume guard
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // UpdateVolume returns immediately when HasStartedPlaying is false.
        // No SoundManager call, no mixer write, no exception.
        public void UpdateVolume_WhenNotStarted_IsNoOpAndDoesNotThrow()
        {
            Assert.IsFalse(_player.HasStartedPlaying);
            Assert.DoesNotThrow(() => _player.UpdateVolume());
        }

        // =====================================================================
        // 14. StaticPitch — SetPitch stores value in StaticPitch and writes AudioSource.pitch
        //     SetPitch is an explicit interface implementation (IAudioPlayer).
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetPitch assigns the given value to StaticPitch and immediately writes AudioSource.pitch.
        public void SetPitch_SetsStaticPitchAndAudioSourcePitch()
        {
            IAudioPlayer player = _player;
            player.SetPitch(2f, 0f);

            Assert.AreEqual(2f, _player.StaticPitch, "StaticPitch must be assigned");
            Assert.AreEqual(2f, _go.GetComponent<AudioSource>().pitch, "AudioSource.pitch must be written immediately");
        }

        // =====================================================================
        // 15. AudioPlayerInstanceWrapper — boundary behavior
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // After Recycle(), IsAvailable(logWarning: false) returns false.
        // IsActive calls IsAvailable(false) so no log is emitted.
        public void InstanceWrapper_AfterRecycle_IsActiveFalseWithoutLog()
        {
            var wrapper = new AudioPlayerInstanceWrapper(_player);
            wrapper.Recycle(); // sets _instance = null

            Assert.IsFalse(wrapper.IsActive);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // After Recycle(), ID returns SoundID.Invalid.
        // The implementation: IsAvailable() ? Instance.ID : SoundID.Invalid
        public void InstanceWrapper_AfterRecycle_IDIsInvalid()
        {
            SetupStubSoundManager();
            var wrapper = new AudioPlayerInstanceWrapper(_player);
            wrapper.Recycle();

            Assert.IsFalse(wrapper.ID.IsValid());
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Explicit cast (AudioPlayer)wrapper returns null when the instance was recycled.
        public void InstanceWrapper_ExplicitCastToAudioPlayer_WhenRecycled_ReturnsNull()
        {
            var wrapper = new AudioPlayerInstanceWrapper(_player);
            wrapper.Recycle();

            AudioPlayer cast = (AudioPlayer)wrapper;
            Assert.IsNull(cast);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // After Recycle(), all fluent calls that check IsAvailable() return Empty.AudioPlayer.
        // This is the Wrap(object method) pattern: method executes but result is discarded;
        // 'this' (wrapper) is returned when available, Empty.AudioPlayer when not.
        public void InstanceWrapper_FluentCallsAfterRecycle_ReturnEmptyAudioPlayer()
        {
            SetupStubSoundManager();
            var wrapper = new AudioPlayerInstanceWrapper(_player);
            wrapper.Recycle();

            IAudioPlayer setVolResult = ((IAudioPlayer)wrapper).SetVolume(0.5f, 0f);
            Assert.AreSame(Empty.AudioPlayer, setVolResult,
                "Fluent calls on recycled wrapper must return Empty.AudioPlayer, not null");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // While the instance is still alive, fluent calls return the WRAPPER itself
        // (not the underlying AudioPlayer), enabling continued chaining on the wrapper.
        // This is the Wrap(object method) behavior: return this (wrapper), not inner result.
        public void InstanceWrapper_FluentCallsWhileAlive_ReturnWrapper()
        {
            var wrapper = new AudioPlayerInstanceWrapper(_player);

            IAudioPlayer setFadeResult = wrapper.SetFadeInEase(Ease.Linear);
            Assert.AreSame(wrapper, setFadeResult,
                "Fluent calls on a live wrapper must return the wrapper, not the underlying player");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // UpdateInstance swaps the internal AudioPlayer reference.
        // After the swap, ID is taken from the new instance.
        public void InstanceWrapper_UpdateInstance_SwitchesToNewPlayerID()
        {
            var wrapper = new AudioPlayerInstanceWrapper(_player);

            var newGo = new GameObject("NewPlayer");
            var newPlayer = newGo.AddComponent<AudioPlayer>();
            var entity = MakeEntity("NewEntity");
            newPlayer.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            wrapper.UpdateInstance(newPlayer);

            Assert.IsTrue(wrapper.ID.IsValid(),
                "After UpdateInstance, wrapper.ID must reflect the new player's ID");

            UnityEngine.Object.DestroyImmediate(newGo);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 16. HasEndPlaying sentinel logic (private method, via reflection)
        //     Characterizes the sample-position latch that detects clip end.
        //     Bug note (from analysis): fragile against DSP jitter.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // HasEndPlaying requires hasPlayed to become true before it returns true.
        // If hasPlayed is still false, returns false even if currentSample >= endSample.
        public void HasEndPlaying_BeforeHasPlayedIsSet_ReturnsFalseEvenIfPastEndSample()
        {
            // Set up an AudioClip so AudioSource.timeSamples is readable
            var clip = AudioClip.Create("test", 1024, 1, 44100, false);
            var audioSource = _go.GetComponent<AudioSource>();
            audioSource.clip = clip;
            // timeSamples defaults to 0; startSample for StartPosition=0 is also 0.
            // hasPlayed = false → currentSample (0) > startSample (0) is false → hasPlayed stays false.
            // return: hasPlayed(false) && (...) → false.

            var method = typeof(AudioPlayer).GetMethod(
                "HasEndPlaying", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "HasEndPlaying not found");

            // _clip is null on a fresh player; inject a minimal BroAudioClip (StartPosition=0)
            // so that _clip.StartPosition inside HasEndPlaying doesn't throw NullReferenceException.
            var clipField = typeof(AudioPlayer).GetField(
                "_clip", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(clipField, "_clip field not found");
            clipField.SetValue(_player, new Ami.BroAudio.Data.BroAudioClip());

            bool hasPlayed = false;
            var args = new object[] { hasPlayed, /* endSample */ 512, /* sampleRate */ 44100 };
            bool result = (bool)method.Invoke(_player, args);

            Assert.IsFalse(result,
                "HasEndPlaying must return false when hasPlayed has never been set to true");

            UnityEngine.Object.DestroyImmediate(clip);
        }

        // =====================================================================
        // 17. INTEGRATION TESTS (stub SoundManager)
        //     These tests exercise paths that require SoundManager.Instance.
        //     A minimal SoundManager singleton is created via reflection
        //     (SetupStubSoundManager) to satisfy the dependency without a
        //     full scene or AudioMixer setup.
        // =====================================================================

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Play() starts PlayControl coroutine, sets PlaybackStartingTime,
        // fires _onStart once (then nulls it), fires _onUpdate every frame.
        public IEnumerator Play_WithValidSetup_StartsPlaybackAndFiresOnStart()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100);
            var id = new SoundID(entity);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(id, pref, new NullMixerPool());

            int onStartCount = 0;
            int onUpdateCount = 0;
            _player.OnStart(_ => onStartCount++);
            _player.OnUpdate(_ => onUpdateCount++);

            _player.Play();
            yield return null; // let PlayControl coroutine advance

            Assert.IsTrue(_player.HasStartedPlaying,
                "PlaybackStartingTime must be set after Play starts");
            Assert.AreEqual(1, onStartCount,
                "_onStart must fire once during first PlayControl iteration");

            var onStartField = typeof(AudioPlayer).GetField(
                "_onStart", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(onStartField.GetValue(_player),
                "_onStart must be null after firing (one-shot behavior)");

            Assert.GreaterOrEqual(onUpdateCount, 1,
                "_onUpdate must fire at least once");

            // Clean up: stop the player
            _player.Stop(0f, StopMode.Stop, null);
            yield return null;

            DestroyEntityWithClip(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop(0f, Stop, cb) with isPlaying=true starts StopControl coroutine,
        // sets IsStopping=true, fades clip volume to 0, calls EndPlaying, fires callback.
        public IEnumerator Stop_StopModeWhilePlaying_FadesAndCallsEndPlaying()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5); // 5 sec clip
            var id = new SoundID(entity);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(id, pref, new NullMixerPool());

            _player.Play();
            yield return null; // let PlayControl start and AudioSource begin playing

            Assert.IsTrue(_player.IsPlaying, "AudioSource should be playing before Stop");

            bool callbackFired = false;
            _player.Stop(0f, StopMode.Stop, () => callbackFired = true);

            // With fadeTime=0, StopControl runs synchronously (no yield) — EndPlaying is immediate
            Assert.IsTrue(callbackFired, "Stop callback must fire");
            Assert.IsFalse(_player.IsActive, "Player must be recycled (IsActive false)");
            Assert.AreEqual(0, _player.PlaybackStartingTime,
                "PlaybackStartingTime must reset after EndPlaying");

            DestroyEntityWithClip(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetVolume(vol, fadeTime>0) starts a Fade coroutine on _trackVolume.
        // UpdateVolume writes clip*track*audioType product to mixer decibels.
        public IEnumerator SetVolume_WithFadeTime_StartsCoroutineAndUpdatesVolume()
        {
            SetupStubSoundManager();
            _player.MixerPool = new NullMixerPool();

            IAudioPlayer player = _player;
            float targetVol = 0.5f;
            float fadeTime = 0.2f;

            // _trackVolume starts at DefaultTrackVolume (1.0)
            player.SetVolume(targetVol, fadeTime);

            // Wait one frame — the Fader should have started interpolating
            yield return null;

            var trackVolumeField = typeof(AudioPlayer).GetField(
                "_trackVolume", BindingFlags.Instance | BindingFlags.NonPublic);
            var trackVolume = trackVolumeField.GetValue(_player);
            float current = (float)trackVolume.GetType().GetProperty("Current").GetValue(trackVolume);

            Assert.Less(current, AudioConstant.FullVolume,
                "_trackVolume.Current should have started decreasing after one frame");

            // Wait for fade to complete
            yield return new WaitForSeconds(fadeTime + 0.1f);

            current = (float)trackVolume.GetType().GetProperty("Current").GetValue(trackVolume);
            Assert.AreEqual(targetVol, current, 0.05f,
                "_trackVolume.Current must reach target after fade completes");
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetPitch(pitch, fadeTime>0) sets StaticPitch immediately,
        // then lerps AudioSource.pitch over fadeTime frames via PitchControl coroutine.
        public IEnumerator SetPitch_WithFadeTime_LerpsAudioSourcePitchOverTime()
        {
            SetupStubSoundManager();

            IAudioPlayer player = _player;
            float targetPitch = 2f;
            float fadeTime = 0.2f;
            float initialPitch = _go.GetComponent<AudioSource>().pitch;

            player.SetPitch(targetPitch, fadeTime);

            // StaticPitch is set immediately
            Assert.AreEqual(targetPitch, _player.StaticPitch,
                "StaticPitch must be set immediately");

            // Wait one frame — pitch should have started lerping
            yield return null;
            float midPitch = _go.GetComponent<AudioSource>().pitch;
            Assert.Greater(midPitch, initialPitch,
                "Pitch should have started lerping after one frame");

            // Wait for fade to complete
            yield return new WaitForSeconds(fadeTime + 0.1f);

            float finalPitch = _go.GetComponent<AudioSource>().pitch;
            Assert.AreEqual(targetPitch, finalPitch, 0.05f,
                "AudioSource.pitch must reach target after fade completes");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // After EndPlaying(), PlaybackStartingTime resets to 0,
        // ID resets to Invalid, _onEnd fires once, Recycle() is called.
        public void EndPlaying_ResetsAllStateAndFiresOnEnd()
        {
            SetupStubSoundManager();

            var entity = MakeEntity();
            var id = new SoundID(entity);
            _player.SetPlaybackData(id, default(PlaybackPreference), new NullMixerPool());
            Assert.IsTrue(_player.IsActive);

            int onEndCount = 0;
            SoundID endedId = SoundID.Invalid;
            _player.OnEnd(soundId => { onEndCount++; endedId = soundId; });

            // Stop on a non-playing player with valid ID triggers EndPlaying directly
            _player.Stop(0f, StopMode.Stop, null);

            Assert.AreEqual(0, _player.PlaybackStartingTime,
                "PlaybackStartingTime must reset to 0");
            Assert.IsFalse(_player.IsActive,
                "ID must reset to Invalid, making IsActive false");
            Assert.AreEqual(1, onEndCount,
                "_onEnd must fire exactly once");
            Assert.AreEqual(id, endedId,
                "_onEnd must receive the original SoundID");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetTrackEffect(Volume, Add) ORs EffectType.Volume into CurrentActiveTrackEffects.
        // When IsUsingTrackEffect flips false→true, ChangeChannel is called:
        //   mixer.SafeSetFloat(trackName,           MinDecibelVolume)  // mute direct send
        //   mixer.SafeSetFloat(trackName + "_Effect", originalVol)     // unmute effect send
        // Uses BroAudioMixer from Resources which has "Track3" and "Track3_Effect" exposed.
        public void SetTrackEffect_AddVolume_UpdatesBitmaskAndSwitchesMixerChannel()
        {
            var mixer = Resources.Load<AudioMixer>("BroAudioMixer");
            Assert.IsNotNull(mixer, "BroAudioMixer must be present in Resources/");

            // "Track3" and "Track3_Effect" are both exposed parameters on BroAudioMixer
            var groups = mixer.FindMatchingGroups("Track3");
            var track3 = System.Array.Find(groups, g => g.name == "Track3");
            Assert.IsNotNull(track3, "AudioMixerGroup 'Track3' must exist in BroAudioMixer");

            // Seed the exposed parameter so TryGetMixerDecibelVolume succeeds
            const float initialDecibel = AudioConstant.FullDecibelVolume; // 0f
            mixer.SetFloat("Track3", initialDecibel);

            // Wire the player to the Track3 group and give it a valid ID
            _go.GetComponent<AudioSource>().outputAudioMixerGroup = track3;
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            Assert.IsFalse(_player.IsUsingTrackEffect, "Precondition: no effect active yet");

            // Act
            _player.SetTrackEffect(EffectType.Volume, SetEffectMode.Add);

            // Bitmask: Volume flag must be set
            Assert.IsTrue((_player.CurrentActiveTrackEffects & EffectType.Volume) != 0,
                "EffectType.Volume must be ORed into CurrentActiveTrackEffects");
            Assert.IsTrue(_player.IsUsingTrackEffect,
                "IsUsingTrackEffect must be true after adding Volume effect");

            // Mixer channel switch verification
            mixer.GetFloat("Track3", out float fromVol);
            Assert.AreEqual(AudioConstant.MinDecibelVolume, fromVol, 0.001f,
                "Track3 parameter must be muted (MinDecibelVolume) after switching to effect channel");

            mixer.GetFloat("Track3_Effect", out float toVol);
            Assert.AreEqual(initialDecibel, toVol, 0.001f,
                "Track3_Effect parameter must be set to the original volume after channel switch");

            // Restore mixer state for subsequent tests in the same play-mode session
            mixer.SetFloat("Track3", initialDecibel);
            mixer.SetFloat("Track3_Effect", AudioConstant.MinDecibelVolume);

            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 18. Play() Early-Return Guards (Gap A)
        //     Play() has three guards before calling PlayInternal():
        //       IsStopping, IsOnHold, and _pref.ScheduledStartTime > 0.
        //     None of these were covered. All return without calling PlayInternal,
        //     so no SoundManager access occurs.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // When IsStopping is true, Play() returns immediately without calling PlayInternal.
        public void Play_WhenIsStopping_ReturnsWithoutPlaying()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            SetAutoProperty(_player, "IsStopping", true);

            _player.Play();

            Assert.IsFalse(_player.HasStartedPlaying,
                "HasStartedPlaying must remain false — Play() must return early when IsStopping");
            Assert.AreEqual(0, _player.PlaybackStartingTime);

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // IsOnHold = (_stopMode == Pause && !HasStartedPlaying).
        // When IsOnHold is true, Play() returns immediately without calling PlayInternal.
        public void Play_WhenIsOnHold_ReturnsWithoutPlaying()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            // HasStartedPlaying is false by default; set _stopMode = Pause to trigger IsOnHold
            SetField(_player, "_stopMode", StopMode.Pause);

            _player.Play();

            Assert.IsFalse(_player.HasStartedPlaying,
                "HasStartedPlaying must remain false — Play() must return early when IsOnHold");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // When _pref.ScheduledStartTime > 0, Play() returns immediately (scheduled playback
        // is driven by ISchedulable.SetScheduledStartTime, not Play()).
        public void Play_WhenScheduledStartTimePositive_ReturnsWithoutPlaying()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            // Read the _pref struct, set ScheduledStartTime > 0, write it back.
            var pref = GetField<AudioPlayer, PlaybackPreference>(_player, "_pref");
            pref.ScheduledStartTime = 1.0;
            SetField(_player, "_pref", pref);

            _player.Play();

            Assert.IsFalse(_player.HasStartedPlaying,
                "HasStartedPlaying must remain false — Play() must return early when ScheduledStartTime > 0");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 19. Stop() Edge Cases (Gap B)
        //     Stop() has multiple paths not tested by the existing suite.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop(nonImmediateFade) when IsStopping=true hits the early-return guard and
        // returns without starting StopControl or modifying any state.
        public void Stop_WhenIsStopping_WithNonImmediateFade_ReturnsEarly()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            SetAutoProperty(_player, "IsStopping", true);

            _player.Stop(1.0f, StopMode.Stop, null); // non-immediate fade

            Assert.IsTrue(_player.IsStopping,
                "IsStopping must still be true — Stop() must return early when already stopping with non-immediate fade");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop(Immediate) when IsStopping=true bypasses the early-return guard
        // (the guard only fires for non-immediate fades) and proceeds into StopControl.
        public IEnumerator Stop_WhenIsStopping_WithImmediateFade_Proceeds()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());

            _player.Play();
            yield return null; // let PlayControl start AudioSource.Play()

            Assert.IsTrue(_player.IsPlaying, "Precondition: AudioSource must be playing");

            SetAutoProperty(_player, "IsStopping", true);

            // With Immediate (0f) fade, the guard `IsStopping && !Approx(fade, Immediate)` is FALSE
            // so Stop() falls through to StopControl, which completes synchronously with fade=0.
            _player.Stop(FadeData.Immediate, StopMode.Stop, null);

            // StopControl with no fade runs synchronously → EndPlaying → Recycle → ID invalid
            Assert.IsFalse(_player.IsActive,
                "Player must be recycled after Stop(Immediate) bypasses the IsStopping guard");

            DestroyEntityWithClip(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop() with an invalid ID and a non-playing AudioSource hits the
        // "!ID.IsValid() || !isPlaying" guard: fires onFinished and calls EndPlaying.
        public void Stop_InvalidID_NotPlaying_FiresOnFinishedAndEndsPlaying()
        {
            SetupStubSoundManager();
            // Player has no valid ID (default); AudioSource is not playing.
            // NullMixerPool is required because EndPlaying → Recycle → MixerPool.ReturnPlayer.
            _player.MixerPool = new NullMixerPool();

            bool callbackFired = false;
            _player.Stop(0f, StopMode.Stop, () => callbackFired = true);

            Assert.IsTrue(callbackFired, "onFinished callback must be invoked");
            Assert.IsFalse(_player.IsActive,
                "EndPlaying must have been called (ID remains invalid after recycle)");
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop(Pause, Immediate) while playing: StopControl pauses the AudioSource
        // but does NOT call EndPlaying (player is not recycled).
        public IEnumerator Stop_PauseMode_WhilePlaying_PausesAudioSourceWithoutRecycling()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());

            int pausedCount = 0;
            _player.OnPause(_ => pausedCount++);

            _player.Play();
            yield return null; // let PlayControl start AudioSource.Play()

            Assert.IsTrue(_player.IsPlaying, "Precondition: AudioSource must be playing");

            // Immediate fade (0f) → StopControl completes synchronously: Pause() + _onPaused fires.
            _player.Stop(FadeData.Immediate, StopMode.Pause, null);

            Assert.IsFalse(_player.GetComponent<AudioSource>().isPlaying,
                "AudioSource must be paused after Stop(Pause)");
            Assert.AreEqual(1, pausedCount, "_onPaused must fire once");
            Assert.IsTrue(_player.IsActive,
                "Player must NOT be recycled after Pause (ID must remain valid)");

            DestroyEntityWithClip(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Stop(Mute, Immediate) while playing: StopControl calls SetVolume(0f),
        // leaving AudioSource still playing (Mute just silences, doesn't stop).
        public IEnumerator Stop_MuteMode_WhilePlaying_SilencesWithoutStoppingAudioSource()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());

            _player.Play();
            yield return null; // let PlayControl start AudioSource.Play()

            Assert.IsTrue(_player.IsPlaying, "Precondition: AudioSource must be playing");

            // Immediate fade → StopControl completes synchronously, ends with SetVolume(0f).
            _player.Stop(FadeData.Immediate, StopMode.Mute, null);

            Assert.IsTrue(_player.IsActive,
                "Player must NOT be recycled after Mute (ID must remain valid)");

            var trackVol = GetField<AudioPlayer, Fader>(_player, "_trackVolume");
            Assert.AreEqual(0f, trackVol.Target, 0.001f,
                "_trackVolume.Target must be 0 after SetVolume(0f) from Mute path");

            DestroyEntityWithClip(entity);
        }

        // =====================================================================
        // 20. Scheduling (Gap C)
        //     AudioPlayer.Scheduling.cs was entirely uncovered.
        // =====================================================================

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetScheduledStartTime() when AudioSource is not yet playing calls PlayInternal(),
        // which starts the PlayControl coroutine and makes AudioSource.isPlaying true.
        public IEnumerator SetScheduledStartTime_WhenNotPlaying_CallsPlayInternal()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());
            // Do NOT call Play() — SetScheduledStartTime must trigger PlayInternal itself.

            double scheduledTime = AudioSettings.dspTime + 5.0;
            InvokeInterfaceMethod(_player, typeof(ISchedulable), "SetScheduledStartTime", scheduledTime);

            // PlayInternal starts the coroutine; AudioSource.PlayScheduled makes isPlaying=true
            yield return null;

            var prefAfter = GetField<AudioPlayer, PlaybackPreference>(_player, "_pref");
            Assert.AreEqual(scheduledTime, prefAfter.ScheduledStartTime, 0.001,
                "_pref.ScheduledStartTime must equal the value passed to SetScheduledStartTime");

            float timeBeforeStart = GetField<AudioPlayer, float>(_player, "_timeBeforeStartSchedule");
            Assert.Greater(timeBeforeStart, 0f,
                "_timeBeforeStartSchedule must be set to a positive value");

            // Stop and clean up
            _player.Stop(FadeData.Immediate, StopMode.Stop, null);
            yield return null;

            DestroyEntityWithClip(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetScheduledEndTime() always adds CheckScheduledEnd to _onUpdate, regardless of
        // whether AudioSource is playing. With AudioSource not playing, no AudioSource call
        // is made, so no SoundManager is needed.
        public void SetScheduledEndTime_AddsCheckScheduledEndToOnUpdate()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            double endTime = AudioSettings.dspTime + 10.0;
            InvokeInterfaceMethod(_player, typeof(ISchedulable), "SetScheduledEndTime", endTime);

            var prefAfter = GetField<AudioPlayer, PlaybackPreference>(_player, "_pref");
            Assert.AreEqual(endTime, prefAfter.ScheduledEndTime, 0.001,
                "_pref.ScheduledEndTime must be set");

            var onUpdate = GetField<AudioPlayer, Action<IAudioPlayer>>(_player, "_onUpdate");
            Assert.IsNotNull(onUpdate, "_onUpdate must be non-null after SetScheduledEndTime");

            bool hasCheckScheduledEnd = false;
            foreach (var d in onUpdate.GetInvocationList())
            {
                if (d.Method.Name == "CheckScheduledEnd")
                {
                    hasCheckScheduledEnd = true;
                    break;
                }
            }
            Assert.IsTrue(hasCheckScheduledEnd,
                "CheckScheduledEnd must be registered in _onUpdate after SetScheduledEndTime");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetDelay(delay) calls SetScheduledStartTime(dspTime + delay), which calls PlayInternal.
        // _pref.ScheduledStartTime is set to approximately dspTime + delay.
        public IEnumerator SetDelay_ConvertsToScheduledStartTime()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);
            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());

            float delay = 2.0f;
            double dspBefore = AudioSettings.dspTime;
            InvokeInterfaceMethod(_player, typeof(ISchedulable), "SetDelay", delay);

            var prefAfter = GetField<AudioPlayer, PlaybackPreference>(_player, "_pref");
            Assert.AreEqual(dspBefore + delay, prefAfter.ScheduledStartTime, 0.1,
                "_pref.ScheduledStartTime must equal dspTime + delay");

            float timeBeforeStart = GetField<AudioPlayer, float>(_player, "_timeBeforeStartSchedule");
            Assert.AreEqual(delay, timeBeforeStart, 0.1f,
                "_timeBeforeStartSchedule must approximately equal the delay");

            yield return null; // let PlayControl start

            _player.Stop(FadeData.Immediate, StopMode.Stop, null);
            yield return null;

            DestroyEntityWithClip(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // When _clip.Delay > 0 and ScheduledStartTime is 0, SchedulePlayback uses PlayDelayed,
        // setting _timeBeforeStartSchedule to the clip's delay value.
        public IEnumerator SchedulePlayback_WithClipDelay_SetsTimeBeforeStartSchedule()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);

            // BroAudioClip is a class with a public Delay field — set it directly.
            Assert.IsNotNull(entity.Clips, "Entity must have clips");
            Assert.Greater(entity.Clips.Length, 0, "Entity must have at least one clip");
            entity.Clips[0].Delay = 0.5f;

            var pref = new PlaybackPreference(entity);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());

            _player.Play();
            yield return null; // let PlayControl run one frame (calls SchedulePlayback)

            float timeBeforeStart = GetField<AudioPlayer, float>(_player, "_timeBeforeStartSchedule");
            Assert.AreEqual(0.5f, timeBeforeStart, 0.05f,
                "_timeBeforeStartSchedule must equal clip.Delay after PlayDelayed path");

            _player.Stop(FadeData.Immediate, StopMode.Stop, null);
            yield return null;

            DestroyEntityWithClip(entity);
        }

        // =====================================================================
        // 21. Decorator Creation (Gap D)
        //     IMusicDecoratable.AsBGM() and IEffectDecoratable.AsDominator() were
        //     never invoked in the test suite.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // AsBGM() creates a MusicPlayer decorator, sets IsBGM=true, returns MusicPlayer.
        public void AsBGM_SetsIsBGMTrue_ReturnsMusicPlayer()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            var result = InvokeInterfaceMethod(_player, typeof(IMusicDecoratable), "AsBGM");

            Assert.IsTrue(_player.IsBGM, "IsBGM must be true after AsBGM()");
            Assert.IsInstanceOf<MusicPlayer>(result, "AsBGM() must return a MusicPlayer");

            var decorators = GetField<AudioPlayer, List<AudioPlayerDecorator>>(_player, "_decorators");
            Assert.IsNotNull(decorators, "_decorators must be non-null after AsBGM()");
            Assert.AreEqual(1, decorators.Count, "_decorators must contain exactly one entry");

            UnityEngine.Object.DestroyImmediate(entity);
        }

#if !UNITY_WEBGL
        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // AsDominator() creates a DominatorPlayer decorator and sets IsDominator=true.
        // Guarded by #if !UNITY_WEBGL in source (no AudioMixer on WebGL).
        public void AsDominator_SetsIsDominatorTrue_ReturnsDominatorPlayer()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            var result = InvokeInterfaceMethod(_player, typeof(IEffectDecoratable), "AsDominator");

            Assert.IsTrue(_player.IsDominator, "IsDominator must be true after AsDominator()");
            Assert.IsInstanceOf<DominatorPlayer>(result, "AsDominator() must return a DominatorPlayer");

            UnityEngine.Object.DestroyImmediate(entity);
        }
#endif

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // AsBGM() called twice returns the same MusicPlayer instance (get-or-create pattern).
        // _decorators count stays at 1 — no duplicates.
        public void AsBGM_CalledTwice_ReturnsSameInstance()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);

            var first  = InvokeInterfaceMethod(_player, typeof(IMusicDecoratable), "AsBGM");
            var second = InvokeInterfaceMethod(_player, typeof(IMusicDecoratable), "AsBGM");

            Assert.AreSame(first, second,
                "AsBGM() called twice must return the same MusicPlayer instance (get-or-create)");

            var decorators = GetField<AudioPlayer, List<AudioPlayerDecorator>>(_player, "_decorators");
            Assert.AreEqual(1, decorators.Count,
                "_decorators must still have count=1 after duplicate AsBGM() call");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 22. AddAudioEffect / RemoveAudioEffect Lifecycle (Gap E)
        //     These paths beyond the inactive-guard were never tested.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Adding the same effect type a second time logs a warning and returns 'this'.
        public void AddAudioEffect_WhenSameTypeAlreadyExists_LogsWarning()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            IAudioPlayer player = _player;

            player.AddLowPassEffect(); // first add — succeeds silently

            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("already exists"));
            var result = player.AddLowPassEffect(); // second add — logs warning

            Assert.AreSame(player, result, "AddAudioEffect must return 'this' even when warning is logged");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // RemoveLowPassEffect() removes the entry from _addedEffects immediately.
        // (The Unity component Destroy is deferred, but the list entry is removed now.)
        public void RemoveAudioEffect_WhenEffectExists_RemovesFromList()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            IAudioPlayer player = _player;

            player.AddLowPassEffect();

            var listBefore = (System.Collections.ICollection)typeof(AudioPlayer)
                .GetField("_addedEffects", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_player);
            Assert.AreEqual(1, listBefore.Count, "Precondition: _addedEffects must have 1 entry");

            var result = player.RemoveLowPassEffect();

            var listAfter = (System.Collections.ICollection)typeof(AudioPlayer)
                .GetField("_addedEffects", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_player);
            Assert.AreEqual(0, listAfter.Count, "_addedEffects must have 0 entries after remove");
            Assert.AreSame(player, result, "RemoveAudioEffect must return 'this'");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Full add→remove round trip: _addedEffects count goes 1 → 0.
        public void AddThenRemove_RoundTrip_CleansUpList()
        {
            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), null);
            IAudioPlayer player = _player;

            var effectsField = typeof(AudioPlayer)
                .GetField("_addedEffects", BindingFlags.Instance | BindingFlags.NonPublic);

            player.AddLowPassEffect();
            Assert.AreEqual(1, ((System.Collections.ICollection)effectsField.GetValue(_player)).Count,
                "After add: count must be 1");

            player.RemoveLowPassEffect();
            Assert.AreEqual(0, ((System.Collections.ICollection)effectsField.GetValue(_player)).Count,
                "After remove: count must be 0");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 23. Volume Internals (Gap F)
        //     Direct tests of SetAudioTypeVolume, SetVolume (zero fade), ResetVolume.
        //     SoundManager is required because Fader.SetVolumeInternal accesses it.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetAudioTypeVolume(0.5f, 0f) sets _audioTypeVolume.Current immediately (no fade).
        public void SetAudioTypeVolume_StoresValueInFader()
        {
            SetupStubSoundManager();
            _player.MixerPool = new NullMixerPool();

            _player.SetAudioTypeVolume(0.5f, 0f);

            var audioTypeVol = GetField<AudioPlayer, Fader>(_player, "_audioTypeVolume");
            Assert.AreEqual(0.5f, audioTypeVol.Current, 0.001f,
                "_audioTypeVolume.Current must equal 0.5f after SetAudioTypeVolume with zero fade");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetVolume(0.5f, 0f) sets _trackVolume.Current immediately (no fade coroutine).
        public void SetVolume_ZeroFadeTime_CompletesImmediately()
        {
            SetupStubSoundManager();
            _player.MixerPool = new NullMixerPool();

            ((IAudioPlayer)_player).SetVolume(0.5f, 0f);

            var trackVol = GetField<AudioPlayer, Fader>(_player, "_trackVolume");
            Assert.AreEqual(0.5f, trackVol.Current, 0.001f,
                "_trackVolume.Current must equal 0.5f after SetVolume with zero fade");
            Assert.IsFalse(trackVol.IsFading,
                "_trackVolume.IsFading must be false after instant set");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // ResetVolume() restores all three Faders to their defaults.
        public void ResetVolume_RestoresDefaults()
        {
            SetupStubSoundManager();
            _player.MixerPool = new NullMixerPool();

            // Deviate all three volumes from their defaults
            _player.SetAudioTypeVolume(0.3f, 0f);
            ((IAudioPlayer)_player).SetVolume(0.5f, 0f);
            // _clipVolume starts at DefaultClipVolume (0f), already non-default after SetAudioTypeVolume

            InvokeMethod(_player, "ResetVolume");

            var trackVol     = GetField<AudioPlayer, Fader>(_player, "_trackVolume");
            var audioTypeVol = GetField<AudioPlayer, Fader>(_player, "_audioTypeVolume");
            var clipVol      = GetField<AudioPlayer, Fader>(_player, "_clipVolume");

            Assert.AreEqual(AudioPlayer.DefaultTrackVolume, trackVol.Current, 0.001f,
                "_trackVolume.Current must reset to DefaultTrackVolume (1.0)");
            Assert.AreEqual(AudioPlayer.DefaultTrackVolume, audioTypeVol.Current, 0.001f,
                "_audioTypeVolume.Current must reset to DefaultTrackVolume (1.0)");
            Assert.AreEqual(AudioPlayer.DefaultClipVolume, clipVol.Current, 0.001f,
                "_clipVolume.Current must reset to DefaultClipVolume (0.0)");
        }

        // =====================================================================
        // 24. Spatial Audio (Gap G)
        //     SetSpatial and ResetSpatial were not exercised directly.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetSpatial() with a follow-target pref: forces spatialBlend to 1f (3D)
        // and positions the AudioPlayer transform at the follow target's position.
        public void SetSpatial_WithFollowTarget_SetsSpatialBlendTo3D()
        {
            SetupStubSoundManager(); // needed for PlaybackPreference constructor

            var entity     = MakeEntity();
            var followTarget = new GameObject("FollowTarget");
            followTarget.transform.position = new Vector3(5f, 0f, 0f);

            var pref = new PlaybackPreference(entity, followTarget.transform);
            InvokeMethod(_player, "SetSpatial", pref);

            Assert.AreEqual(AudioConstant.SpatialBlend_3D,
                _go.GetComponent<AudioSource>().spatialBlend, 0.001f,
                "spatialBlend must be forced to 3D when follow target is set");
            Assert.AreEqual(new Vector3(5f, 0f, 0f), _player.transform.position,
                "Player transform must be moved to the follow target's position");

            UnityEngine.Object.DestroyImmediate(followTarget);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // ResetSpatial() restores AudioSource spatial properties to their defaults.
        // No SoundManager needed — pure AudioSource property reset.
        public void ResetSpatial_RestoresDefaultSpatialValues()
        {
            var audioSource = _go.GetComponent<AudioSource>();

            // Set non-default values
            audioSource.spatialBlend = 1f;
            audioSource.panStereo    = 0.5f;
            _player.transform.position = new Vector3(10f, 0f, 0f);

            InvokeMethod(_player, "ResetSpatial");

            Assert.AreEqual(AudioConstant.SpatialBlend_2D,  audioSource.spatialBlend,  0.001f,
                "spatialBlend must reset to 2D (0)");
            Assert.AreEqual(Vector3.zero, _player.transform.position,
                "transform.position must reset to zero");
            Assert.AreEqual(AudioConstant.DefaultPanStereo, audioSource.panStereo,     0.001f,
                "panStereo must reset to DefaultPanStereo");
            Assert.AreEqual(AudioConstant.DefaultDoppler,   audioSource.dopplerLevel,  0.001f,
                "dopplerLevel must reset to DefaultDoppler");
        }

        // =====================================================================
        // 25. Recycling Details (Gap H)
        //     Recycle(), SetInstanceWrapper/GetInstanceWrapper not directly tested.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Recycle() resets: ID, all four events, _decorators, _instanceWrapper,
        // OnPlaybackHandover. NullMixerPool is required (Recycle calls ReturnPlayer).
        public void Recycle_ClearsIDEventsDecoratorsWrapper()
        {
            SetupStubSoundManager();

            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), default(PlaybackPreference), new NullMixerPool());
            Assert.IsTrue(_player.IsActive, "Precondition: player must be active");

            // Subscribe to all four events
            _player.OnEnd(_ => { });
            _player.OnStart(_ => { });
            _player.OnUpdate(_ => { });
            _player.OnPause(_ => { });

            // Create a decorator
            InvokeInterfaceMethod(_player, typeof(IMusicDecoratable), "AsBGM");
            Assert.IsTrue(_player.IsBGM, "Precondition: IsBGM must be true");

            // Set an instance wrapper via reflection (SetInstanceWrapper is internal)
            var wrapper = new AudioPlayerInstanceWrapper(_player);
            InvokeMethod(_player, "SetInstanceWrapper", wrapper);

            // Set a handover delegate
            _player.OnPlaybackHandover = (id, w, p, e, v, pitch) => { };

            _player.Recycle();

            Assert.IsFalse(_player.ID.IsValid(),        "ID must be invalid after Recycle");
            Assert.IsNull(GetField<AudioPlayer, Action<SoundID>>(_player, "_onEnd"),                  "_onEnd must be null");
            Assert.IsNull(GetField<AudioPlayer, Action<IAudioPlayer>>(_player, "_onStart"),           "_onStart must be null");
            Assert.IsNull(GetField<AudioPlayer, Action<IAudioPlayer>>(_player, "_onUpdate"),          "_onUpdate must be null");
            Assert.IsNull(GetField<AudioPlayer, Action<IAudioPlayer>>(_player, "_onPaused"),          "_onPaused must be null");
            Assert.IsNull(GetField<AudioPlayer, List<AudioPlayerDecorator>>(_player, "_decorators"),  "_decorators must be null");
            Assert.IsNull(_player.OnPlaybackHandover,                                    "OnPlaybackHandover must be null");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // SetInstanceWrapper / GetInstanceWrapper round-trip:
        // GetInstanceWrapper returns the same object that was passed to SetInstanceWrapper.
        public void SetInstanceWrapper_GetInstanceWrapper_RoundTrip()
        {
            var wrapper = new AudioPlayerInstanceWrapper(_player);

            // SetInstanceWrapper and GetInstanceWrapper are internal — access via reflection.
            InvokeMethod(_player, "SetInstanceWrapper", wrapper);
            var retrieved = (IAudioPlayer)InvokeMethod(_player, "GetInstanceWrapper");

            Assert.IsNotNull(retrieved, "GetInstanceWrapper must return a non-null IAudioPlayer");
            Assert.AreSame(wrapper, retrieved,
                "GetInstanceWrapper must return the same wrapper instance that was set");
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // Recycle() completes without exception when NullMixerPool handles ReturnPlayer.
        // After recycling, ID is invalid (smoke test for the MixerPool.ReturnPlayer call).
        public void Recycle_CallsMixerPoolReturnPlayer()
        {
            SetupStubSoundManager();

            var entity = MakeEntity();
            _player.SetPlaybackData(new SoundID(entity), new PlaybackPreference(entity), new NullMixerPool());
            Assert.IsTrue(_player.IsActive, "Precondition: player must be active");

            Assert.DoesNotThrow(() => _player.Recycle(),
                "Recycle() must not throw when NullMixerPool handles ReturnPlayer");

            Assert.IsFalse(_player.ID.IsValid(),
                "ID must be invalid after Recycle");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        // =====================================================================
        // 26. Miscellaneous (Gap I)
        //     PlayingPosition, Update follow-target tracking, SetVelocity fluency.
        // =====================================================================

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // PlayingPosition returns _pref.Position. When pref was constructed with an explicit
        // Vector3, Position returns that exact value.
        public void PlayingPosition_ReturnsPrefPosition()
        {
            SetupStubSoundManager(); // needed for PlaybackPreference(entity, position) ctor

            var entity = MakeEntityWithClip();
            var position = new Vector3(3f, 4f, 5f);
            var pref = new PlaybackPreference(entity, position);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());

            Assert.AreEqual(position, _player.PlayingPosition,
                "PlayingPosition must equal the position passed to PlaybackPreference");

            DestroyEntityWithClip(entity);
        }

        [UnityTest]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // When a follow target is set, AudioPlayer.Update() moves the player's transform
        // to the target's position every frame.
        public IEnumerator Update_WithFollowTarget_MovesTransformPosition()
        {
            SetupStubSoundManager();

            var entity = MakeEntityWithClip(BroAudioType.SFX, sampleLength: 44100 * 5);
            var followTarget = new GameObject("FollowTarget");
            followTarget.transform.position = new Vector3(5f, 0f, 0f);

            var pref = new PlaybackPreference(entity, followTarget.transform);
            _player.SetPlaybackData(new SoundID(entity), pref, new NullMixerPool());
            _player.Play();
            yield return null; // let Play start and first Update run

            Assert.AreEqual(new Vector3(5f, 0f, 0f), _player.transform.position,
                "Player transform must be at the initial follow-target position");

            followTarget.transform.position = new Vector3(10f, 0f, 0f);
            yield return null; // let Update() move the transform

            Assert.AreEqual(new Vector3(10f, 0f, 0f), _player.transform.position,
                "Player transform must track the follow-target's new position after Update()");

            _player.Stop(FadeData.Immediate, StopMode.Stop, null);
            yield return null;

            UnityEngine.Object.DestroyImmediate(followTarget);
            DestroyEntityWithClip(entity);
        }

        [Test]
        // CHARACTERIZATION TEST — captures current behavior, not ideal behavior.
        // IAudioPlayer.SetVelocity is an explicit interface implementation that returns 'this'
        // (fluent pattern). Verified via GetInterfaceMap since it is not directly callable.
        public void SetVelocity_ReturnsSelf_Fluent()
        {
            SetupStubSoundManager();
            var entity = MakeEntity();
            SetField(entity, "MulticlipsPlayMode", MulticlipsPlayMode.Velocity);
            _player.SetPlaybackData(new SoundID(entity), new PlaybackPreference(entity), null);

            var result = InvokeInterfaceMethod(_player, typeof(IAudioPlayer), "SetVelocity", 100);

            Assert.AreSame(_player, result,
                "IAudioPlayer.SetVelocity must return 'this' (fluent pattern)");

            UnityEngine.Object.DestroyImmediate(entity);
        }

        // ── test doubles ─────────────────────────────────────────────────────

        private class NullMixerPool : IAudioMixerPool
        {
            AudioMixerGroup IAudioMixerPool.GetTrack(AudioTrackType _) => null;
            void IAudioMixerPool.ReturnTrack(AudioTrackType _, AudioMixerGroup __) { }
            void IAudioMixerPool.ReturnPlayer(AudioPlayer _) { }
        }
    }
}
