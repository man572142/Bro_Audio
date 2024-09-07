#if BroAudio_DevOnly
using System.Reflection;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Ami.Extension.Reflection
{
#if BroAudio_DevOnly_GeneratedCodeSample
    public interface IAudioSourceModifierSample
    {
        /// <inheritdoc cref="AudioSource.volume"/>
        float volume { get; set; }
    }

    public class AudioSourceModifierSample : BroModifier<AudioSource>, IAudioSourceModifierSample
    {
        public AudioSourceModifierSample(AudioSource @base) : base(@base) {}

        private bool _hasVolumeResetAction = false;
        public float volume
        {
            get => Base.volume;
            set
            {
                AddResetAction(ref _hasVolumeResetAction, () => Base.volume = 1f);
                Base.volume = value;
            }
        }
    }

    public class EmptyAudioSourceSample : IAudioSourceModifierSample
    {
        public float volume { get => 1f; set { } }
    }
#endif

#if BroAudio_DevOnly

    public static class AudioSourceProxyGenerator
    {
        [MenuItem("Tools/BroAudio/Generate Audio Source Proxy")]
        public static void Generate()
        {
            ProxyModifierCodeGenerator.Parameters parameters = new ProxyModifierCodeGenerator.Parameters()
            {
                Namespace = "Ami.Extension",
                ScriptName = "AudioSourceProxy",
                Path = Path.GetFullPath("Assets/BroAudio/Core/Scripts/Player/AutoGeneratedCode"),
                Usings = new string[] { "UnityEngine", "UnityEngine.Audio" },
            };

            ProxyModifierCodeGenerator.GenerateModifierCode<AudioSource>(parameters);
        }
    }

    public static class ProxyModifierCodeGenerator
    {
        public const string Title = "// Auto-generated code";
        public const string GamepadSpeakerCondition = "UNITY_EDITOR || UNITY_PS4 || UNITY_PS5";
        public struct Parameters
        {
            public string Namespace;
            public string ScriptName;
            public string Path;
            public string[] Usings;
            public bool IncludeBaseTypeMembers;
        }

        public static void GenerateModifierCode<T>(Parameters parameters, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) where T : Component
        {
            Type type = typeof(T);
            MemberInfo[] members = type.GetMembers(bindingFlags);

            if (members == null || members.Length == 0)
            {
                Debug.LogError($"No valid members in {type}, BindingFlags:{bindingFlags}");
                return;
            }

            if (!Directory.Exists(parameters.Path))
            {
                Debug.LogError($"Path:{parameters.Path} not exist!");
                return;
            }

            var filteredMembers = members
                .Where(x => x.MemberType == MemberTypes.Property &&
                (!parameters.IncludeBaseTypeMembers && x.DeclaringType == typeof(T)) &&
                x is PropertyInfo property && property.CanWrite && x.GetCustomAttribute(typeof(ObsoleteAttribute)) == null);

            var defaultValueMap = GetDefaultValueMap<T>(filteredMembers);
            CreateModifierInterface<T>(parameters, filteredMembers);
            CreateModifierClass<T>(parameters, filteredMembers, defaultValueMap);
            CreateEmptyModifierClass<T>(parameters, filteredMembers, defaultValueMap);
            AssetDatabase.Refresh();
        }

        private static void CreateModifierInterface<T>(Parameters parameters, IEnumerable<MemberInfo> members) where T : Component
        {
            string interfaceName = "I" + parameters.ScriptName;
            parameters.ScriptName = interfaceName;
            using (var file = CodeWriter.Write(parameters, CodeWriter.Type.Interface))
            {
                foreach (var member in members)
                {
                    if (member is not PropertyInfo property)
                    {
                        continue;
                    }

                    if(TryGetConditionalCompilation(property, out string condition))
                    {
                        file.WriteConditional_If(condition);
                    }

                    string typeName = property.PropertyType.GetSimpleTypeName();
                    file.WriteLine(file.Indent + $"/// <inheritdoc cref=\"{typeof(T).Name}.{property.Name}\"/>");
                    file.WriteLine(file.Indent + $"{typeName} {property.Name} {{ get; set; }}");

                    if(condition != null)
                    {
                        file.WriteConditional_EndIf();
                    }
                    file.WriteLine();
                }
            }
        }

        private static void CreateModifierClass<T>(Parameters parameters, IEnumerable<MemberInfo> members, Dictionary<MemberInfo, string> defaultValueMap) where T : Component
        {
            string targetName = typeof(T).Name;
            string implementation = $" : BroModifier<{targetName}>, I" + parameters.ScriptName;
            using (var file = CodeWriter.Write(parameters, CodeWriter.Type.Class, implementation))
            {
                file.WriteLine(file.Indent + $"public {parameters.ScriptName}({targetName} @base) : base(@base) {{}}");
                file.WriteLine();
                foreach (var member in members)
                {
                    WriteGetterSetterBody(file, file.Indent, member, GetDefaultValue(member, defaultValueMap));
                }
            }

            static void WriteGetterSetterBody(CodeWriter file, string indent, MemberInfo member, string defaultValue = "default")
            {
                if (member is not PropertyInfo property)
                {
                    return;
                }

                if (TryGetConditionalCompilation(property, out string condition))
                {
                    file.WriteConditional_If(condition);
                }

                string typeName = property.PropertyType.GetSimpleTypeName();
                string varName = property.Name;
                string pascalVarName = varName.ToPascal();
                string varNameOfHasReset = $"_has{pascalVarName}ResetAction";

                file.WriteLine(indent + $"private bool {varNameOfHasReset} = false;");
                using (file.WriteBraces($"public {typeName} {varName}", ref indent))
                {
                    file.WriteLine(indent + $"get => Base.{varName};");
                    using (file.WriteBraces("set", ref indent))
                    {
                        file.WriteLine(indent + $"AddResetAction(ref {varNameOfHasReset}, () => Base.{varName} = {defaultValue});");
                        file.WriteLine(indent + $"Base.{varName} = value;");
                    }
                }

                if (condition != null)
                {
                    file.WriteConditional_EndIf();
                }
                file.WriteLine();
            }
        }

        private static void CreateEmptyModifierClass<T>(Parameters parameters, IEnumerable<MemberInfo> filteredMembers, Dictionary<MemberInfo, string> defaultValueMap) where T : Component
        {
            string interfaceName = "I" + parameters.ScriptName;
            parameters.ScriptName = "Empty" + parameters.ScriptName;
            using (var file = CodeWriter.Write(parameters, CodeWriter.Type.Class, $" : " + interfaceName))
            {
                foreach (var member in filteredMembers)
                {
                    if (member is not PropertyInfo property)
                    {
                        continue;
                    }

                    if (TryGetConditionalCompilation(property, out string condition))
                    {
                        file.WriteConditional_If(condition);
                    }

                    string typeName = property.PropertyType.GetSimpleTypeName();
                    string defaultValue = GetDefaultValue(member, defaultValueMap);
                    file.WriteLine(file.Indent + $"public {typeName} {property.Name} {{ get => {defaultValue}; set {{ }} }}");

                    if (condition != null)
                    {
                        file.WriteConditional_EndIf();
                    }
                    file.WriteLine();
                }
            }
        }

        private static Dictionary<MemberInfo, string> GetDefaultValueMap<T>(IEnumerable<MemberInfo> filteredMembers) where T : Component
        {
            var temp = new GameObject("Temp");
            temp.hideFlags = HideFlags.HideAndDontSave;

            T component = temp.AddComponent<T>();
            Dictionary<MemberInfo, string> defaultValueMap = new Dictionary<MemberInfo, string>();
            foreach (PropertyInfo property in filteredMembers)
            {
                object value = property.GetValue(component);
                if (value != default)
                {
                    string valueString = value.ToString();
                    if (property.PropertyType.IsEnum)
                    {
                        valueString = property.PropertyType.ToString() + "." + valueString;
                    }
                    else if (property.PropertyType == typeof(bool))
                    {
                        valueString = valueString.ToCamel();
                    }

                    defaultValueMap.Add(property, valueString);
                }
            }
            GameObject.DestroyImmediate(temp);
            return defaultValueMap;
        }

        private static string GetDefaultValue(MemberInfo member, Dictionary<MemberInfo, string> defaultValueMap)
        {
            if (defaultValueMap.TryGetValue(member, out string result))
            {
                return result;
            }
            return "default";
        }

        public static void WriteUsings(this StreamWriter writer, string[] usings)
        {
            if (usings != null)
            {
                foreach (var usage in usings)
                {
                    writer.WriteLine($"using {usage};");
                }
            }
        }

        private static bool TryGetConditionalCompilation(PropertyInfo property, out string condition)
        {
            condition = null;
            if (property.PropertyType == typeof(GamepadSpeakerOutputType))
            {
                condition = GamepadSpeakerCondition;
            }
            return condition != null;
        }

        private static string GetSimpleTypeName(this Type type) => type switch
        {
            Type f when f == typeof(float) => "float",
            Type i when i == typeof(int) => "int",
            Type b when b == typeof(bool) => "bool",
            Type l when l == typeof(long) => "long",
            Type d when d == typeof(double) => "double",
            Type c when c == typeof(char) => "char",
            Type s when s == typeof(string) => "string",
            Type o when o == typeof(object) => "object",
            _ => type.Name,
        };

        private static string ToPascal(this string str)
        {
            char[] array = str.ToCharArray();
            array[0] = array[0].ToUpper();
            return new string(array);
        }

        private static string ToCamel(this string str)
        {
            char[] array = str.ToCharArray();
            array[0] = array[0].ToLower();
            return new string(array);
        }
    } 
#endif
}
#endif