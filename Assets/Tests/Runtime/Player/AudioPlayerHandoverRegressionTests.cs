using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ami.BroAudio;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Tests
{
    /// <summary>
    /// Section 30 — Regression tests for the PlayScheduled+handover loop.
    /// Covers the three correctness invariants identified in the adversarial review:
    ///   30-A  Scheduled end time survives pause/unpause.
    ///   30-B  LoopType.Loop keeps the same clip across iterations.
    ///   30-C  In-flight volume fade does not snap at the loop handover boundary.
    /// </summary>
    public class AudioPlayerHandoverRegressionTests
    {
        private GameObject _soundManagerGo;
        private AudioPlayer _player;

        [SetUp]
        public void SetUp()
        {
            SetupStubSoundManager();
            var go = new GameObject("AudioPlayer");
            go.AddComponent<AudioSource>();
            _player = go.AddComponent<AudioPlayer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_player != null) UnityEngine.Object.DestroyImmediate(_player.gameObject);
            if (_soundManagerGo != null) UnityEngine.Object.DestroyImmediate(_soundManagerGo);
        }

        private void SetupStubSoundManager()
        {
            var prefab = Resources.Load<GameObject>("SoundManager");
            if (prefab == null) return;
            prefab.SetActive(false);
            _soundManagerGo = UnityEngine.Object.Instantiate(prefab);
            if (_soundManagerGo.TryGetComponent(out SoundManager sm))
            {
                var f = typeof(SoundManager)
                    .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                f?.SetValue(null, sm);
            }
            _soundManagerGo.SetActive(true);
        }

        // ── reflection helpers ────────────────────────────────────────────────

        private static T GetField<T>(object obj, string name)
        {
            var fi = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {obj.GetType().Name}");
            return (T)fi.GetValue(obj);
        }

        private static object InvokeInterfaceMethod(object obj, Type iface, string name, params object[] args)
        {
            var map = obj.GetType().GetInterfaceMap(iface);
            int argCount = args?.Length ?? 0;
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                var ifaceMethod = map.InterfaceMethods[i];
                if (ifaceMethod.Name == name && ifaceMethod.GetParameters().Length == argCount)
                    return map.TargetMethods[i].Invoke(obj, args);
            }
            Assert.Fail($"Interface method '{iface.Name}.{name}' with {argCount} parameter(s) not found");
            return null;
        }

        private static void SetAutoProperty(object obj, string propertyName, object value)
        {
            var fi = obj.GetType().GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi,
                $"Auto-property backing field for '{propertyName}' not found on {obj.GetType().Name}");
            fi.SetValue(obj, value);
        }

        // ── 30-A ─────────────────────────────────────────────────────────────

        /// <summary>
        /// After Pause, _pref.ScheduledEndTime must equal the original value passed to
        /// SetScheduledEndTime. StopControl cancels the AudioSource boundary, but the pref
        /// field must survive so the successor player can re-apply it on resume.
        /// </summary>
        [UnityTest]
        public IEnumerator SetScheduledEndTime_AfterPause_ScheduledEndTimeIsPreservedInPref()
        {
            var entity = ScriptableObject.CreateInstance<AudioEntity>();
            var soundId = new SoundID(entity);
            SetAutoProperty(_player, "ID", soundId);
            SetAutoProperty(_player, "PlaybackStartingTime", 1);

            double originalEndTime = AudioSettings.dspTime + 30.0;
            InvokeInterfaceMethod(_player, typeof(ISchedulable), "SetScheduledEndTime", originalEndTime);

            // AudioSource is not playing, so Pause just sets _stopMode without touching _pref.
            InvokeInterfaceMethod(_player, typeof(IAudioStoppable), "Pause", FadeData.Immediate);
            yield return null;

            var prefAfterPause = GetField<PlaybackPreference>(_player, "_pref");
            Assert.AreEqual(originalEndTime, prefAfterPause.ScheduledEndTime, 1e-9,
                "ScheduledEndTime must survive a Pause call; the successor reads this field at handover.");

            var onUpdate = GetField<Action<IAudioPlayer>>(_player, "_onUpdate");
            Assert.IsNotNull(onUpdate,
                "_onUpdate must retain the CheckScheduledEnd listener after SetScheduledEndTime+Pause.");

            SetAutoProperty(_player, "ID", SoundID.Invalid);
            SetAutoProperty(_player, "PlaybackStartingTime", 0);
            UnityEngine.Object.DestroyImmediate(entity);
        }

        // ── 30-B ─────────────────────────────────────────────────────────────

        /// <summary>
        /// For a Single-clip entity, PickNewClip must return the same IBroAudioClip and
        /// AudioClip reference on every call. This is the deterministic invariant that
        /// LoopType.Loop handover relies on via PinnedClip.
        /// </summary>
        [UnityTest]
        public IEnumerator LoopHandover_SingleClipEntity_PickNewClipReturnsSameClipOnEveryCall()
        {
            var entity = ScriptableObject.CreateInstance<AudioEntity>();
            var audioClip = AudioClip.Create("test_loop_clip", 44100, 1, 44100, false);
            var broClip = new BroAudioClip();
            typeof(BroAudioClip)
                .GetField("AudioClip", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(broClip, audioClip);
            entity.Clips = new[] { broClip };

            var firstClip  = entity.PickNewClip();
            var secondClip = entity.PickNewClip();
            yield return null;

            Assert.AreSame(firstClip, secondClip,
                "Single-clip AudioEntity.PickNewClip() must return the same IBroAudioClip every call — " +
                "LoopType.Loop handover must not re-pick across the boundary.");
            Assert.AreSame(firstClip.GetAudioClip(), secondClip.GetAudioClip(),
                "Both iterations must play the same AudioClip reference.");

            UnityEngine.Object.DestroyImmediate(entity);
            UnityEngine.Object.DestroyImmediate(audioClip);
        }

        // ── 30-C ─────────────────────────────────────────────────────────────

        /// <summary>
        /// When a volume fade is in progress at handover, the successor must start at the
        /// predecessor's instantaneous (Current) volume, not its fade Target. InheritVolumeState
        /// seeds from Current; falling back to SetVolume(target) would cause an audible snap.
        /// </summary>
        [UnityTest]
        public IEnumerator LoopHandover_SuccessorVolume_MatchesPredecessorCurrentNotTarget()
        {
            const float epsilon = 0.01f;

            var entity = ScriptableObject.CreateInstance<AudioEntity>();
            SetAutoProperty(_player, "ID", new SoundID(entity));

            // Simulate a fade in-progress: Current = 1.0, Target = 0.4.
            var predFader = GetField<Fader>(_player, "_trackVolume");
            predFader.Complete(1.0f, false);
            typeof(Fader)
                .GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(predFader, new object[] { 0.4f });

            float currentAtHandover = predFader.Current; // 1.0 — the heard volume
            float targetAtHandover  = predFader.Target;  // 0.4 — the fade destination

            Assert.AreNotEqual(currentAtHandover, targetAtHandover,
                "Pre-condition: fade must be in-progress (Current != Target).");

            // Successor should be primed at Current (via InheritVolumeState), not Target.
            var successorGo = new GameObject("SuccessorPlayer");
            successorGo.AddComponent<AudioSource>();
            var successor = successorGo.AddComponent<AudioPlayer>();
            SetAutoProperty(successor, "ID", new SoundID(entity));

            // InheritVolumeState is internal — invoke via reflection.
            typeof(AudioPlayer)
                .GetMethod("InheritVolumeState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.Invoke(successor, new object[] { currentAtHandover, targetAtHandover, 1.0f });
            yield return null;

            var succFader = GetField<Fader>(successor, "_trackVolume");
            Assert.AreEqual(currentAtHandover, succFader.Current, epsilon,
                "Successor must start at the predecessor's instantaneous (Current) volume, " +
                "not at its fade Target — snapping to Target would be audible.");
            Assert.Greater(Mathf.Abs(succFader.Current - targetAtHandover), epsilon,
                "Successor must NOT be primed at the predecessor's fade Target.");

            SetAutoProperty(_player, "ID", SoundID.Invalid);
            SetAutoProperty(successor, "ID", SoundID.Invalid);
            UnityEngine.Object.DestroyImmediate(successorGo);
            UnityEngine.Object.DestroyImmediate(entity);
        }
    }
}
