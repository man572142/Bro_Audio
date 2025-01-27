using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ami.Extension;

namespace Ami.BroAudio.Testing
{
    [DisallowMultipleComponent]
    public class BroTestingButton : MonoBehaviour
    {
        public const string ClassColor = "4EC9B0";
        public const string StructColor = "86BB6F";
        public const string MethodColor = "DCDCAA";
        public const string None = "";

        public const string Volume = "vol";
        public const string Fade = "fade";
        public const string AudioType = "type";
        public const string ID = "id";
        public const string Pitch = "pitch";
        public const string Frequency = "frequency";
        public const string TransitionParam = "transition";
        public const string Time = "time";

        public readonly string BaseFormat = "BroAudio".SetColor(ClassColor);
        public readonly string MethodFormat = "." + "{0}".SetColor(MethodColor) + "({1})";
        public readonly string EffectFormat = "Effect".SetColor(StructColor);

        public const string Play = nameof(BroAudio.Play);
        public const string SetVolume = nameof(BroAudio.SetVolume);
        public const string SetPitch = nameof(BroAudio.SetPitch);
        public const string Stop = nameof(BroAudio.Stop);
        public const string Pause = nameof(BroAudioChainingMethod.Pause);
        public const string UnPause = nameof(BroAudioChainingMethod.UnPause);

        public const string SetScheduleStartTime = nameof(BroAudioChainingMethod.SetScheduledStartTime);
        public const string SetScheduleEndTime = nameof(BroAudioChainingMethod.SetScheduledEndTime);
        public const string SetDelay = nameof(BroAudioChainingMethod.SetDelay);

        public const string LowPass = nameof(Effect.LowPass);
        public const string ResetLowPass = nameof(Effect.ResetLowPass);
        public const string HighPass = nameof(Effect.HighPass);
        public const string ResetHighPass = nameof(Effect.ResetHighPass);
#if !UNITY_WEBGL
        public const string SetEffect = nameof(BroAudio.SetEffect);
        public const string Dominator = nameof(BroAudioChainingMethod.AsDominator); 
#endif

        public const string BGM = nameof(BroAudioChainingMethod.AsBGM);
        public const string LowPasOthers = "LowPassOthers";
        public const string HighPasOthers = "HighPasOthers";
        public const string Transition = "Transition";

        [SerializeField] bool _forceValidate = false;
        [SerializeField] Button _button;
        [SerializeField] Text _text;

        private void OnValidate()
        {
            if (!_button || !_text)
            {
                return;
            }
            _text.text = string.Empty;
            for (int i = 0; i < _button.onClick.GetPersistentEventCount(); i++)
            {
                if (i != 0)
                {
                    _text.text += "\n";
                }
                _text.text += GetAPIString(_button.onClick.GetPersistentMethodName(i));
            }
        }

        private string GetAPIString(string methodName) => methodName switch
        {
            nameof(BroTesting.Play) => GetMainMethodText(Play),
            nameof(BroTesting.PlayScheduled) => GetMainMethodText(Play) + GetPlayerAppendMethodString(SetScheduleStartTime, Time),
            nameof(BroTesting.SetVolume) => GetMainMethodText(SetVolume, Volume, Fade),
            nameof(BroTesting.SetAudioTypeVolume) => GetMainMethodText(SetVolume, AudioType, Volume, Fade),
            nameof(BroTesting.SetSoundIDVolume) => GetMainMethodText(SetVolume, ID, Volume, Fade),
            nameof(BroTesting.PlayerSetVolume) => GetPlayerAppendMethodString(SetVolume, Volume, Fade),

            nameof(BroTesting.SetPitch) => GetMainMethodText(SetPitch, Pitch, Fade),
            nameof(BroTesting.SetAudioTypePitch) => GetMainMethodText(SetPitch, Pitch, AudioType, Fade),
            nameof(BroTesting.PlayerSetPitch) => GetPlayerAppendMethodString(SetPitch, Pitch, Fade),

            nameof(BroTesting.SetScheduleStartTime) => GetPlayerAppendMethodString(SetScheduleStartTime, Time),
            nameof(BroTesting.SetScheduleEndTime) => GetPlayerAppendMethodString(SetScheduleEndTime, Time),
            nameof(BroTesting.SetDelay) => GetMainMethodText(Play) + GetPlayerAppendMethodString(SetDelay, Time),

            nameof(BroTesting.PlayerStop) => GetPlayerAppendMethodString(Stop, Fade),
            nameof(BroTesting.StopSoundID) => GetMainMethodText(Stop, ID, Fade),
            nameof(BroTesting.StopAudioType) => GetMainMethodText(Stop, AudioType, Fade),
            nameof(BroTesting.Pause) => GetPlayerAppendMethodString(Pause),
            nameof(BroTesting.UnPause) => GetPlayerAppendMethodString(UnPause),

#if !UNITY_WEBGL
            nameof(BroTesting.SetLowPassFilter) => GetMainMethodText(SetEffect, GetEffectFactoryText(LowPass, Frequency, Fade), AudioType),
            nameof(BroTesting.ResetLowPassFilter) => GetMainMethodText(SetEffect, GetEffectFactoryText(ResetLowPass, Fade), AudioType),
            nameof(BroTesting.SetHighPassFilter) => GetMainMethodText(SetEffect, GetEffectFactoryText(HighPass, Frequency, Fade), AudioType),
            nameof(BroTesting.ResetHighPassFilter) => GetMainMethodText(SetEffect, GetEffectFactoryText(ResetHighPass, Fade), AudioType),

            nameof(BroTesting.PlayerAsLowPassDominator) => GetPlayerAppendMethodString(Dominator) + GetPlayerAppendMethodString(LowPasOthers, Frequency, Fade),
            nameof(BroTesting.PlayerAsHighPassDominator) => GetPlayerAppendMethodString(Dominator) + GetPlayerAppendMethodString(HighPasOthers, Frequency, Fade),

#endif
            nameof(BroTesting.AppendAsBGM) => GetPlayerAppendMethodString(BGM) + GetPlayerAppendMethodString(Transition, TransitionParam, Fade),
            nameof(BroTesting.PlayAsBGM) => GetMainMethodText(Play) + GetPlayerAppendMethodString(BGM) + GetPlayerAppendMethodString(Transition, TransitionParam, Fade),
            _ => throw new System.NotImplementedException(),
        };

        private string GetMainMethodText(params string[] methodAndParameters)
        {
            return string.Format(BaseFormat + MethodFormat, GetMethodAndParameters(methodAndParameters));
        }

        private string GetEffectFactoryText(params string[] methodAndParameters)
        {
            return string.Format(EffectFormat + MethodFormat, GetMethodAndParameters(methodAndParameters));
        }

        private string[] GetMethodAndParameters(string[] parameters)
        {
            if (parameters.Length >= 1)
            {
                string[] result = new string[2];
                result[0] = parameters[0];
                result[1] = string.Empty;
                for (int i = 1; i < parameters.Length; i++)
                {
                    string comma = i == parameters.Length - 1 ? string.Empty : ",";
                    result[1] += parameters[i] + comma;
                }
                return result;
            }
            return null;
        }

        private string GetPlayerAppendMethodString(params string[] methodAndParameters)
        {
            return string.Format(MethodFormat, GetMethodAndParameters(methodAndParameters));
        }
    } 
}
