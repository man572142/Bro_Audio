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

    public class AudioSourceModifierSample : IAudioSourceModifierSample, IDisposable
    {
        private AudioSource _source;
        public AudioSourceModifierSample(AudioSource source) => _source = source;

        private bool _isVolumeModified = false;
        public float volume
        {
            get => Base.volume;
            set
            {
                _isVolumeModified = true;
                _source.volume = value;
            }
        }

        // Other properties here

        public void Dispose()
        {
            if (_isVolumeModified) _source.volume = 1f;
            // Other properties here
        }
    }

    public class EmptyAudioSourceSample : IAudioSourceModifierSample
    {
        public float volume { get => 1f; set { } }
    }
#endif

    public static class ProxyModifierCodeGenerator
    {
        public const string Title = "// Auto-generated code";
        public const string GamepadSpeakerCondition = "(UNITY_EDITOR && UNITY_2021_3_OR_NEWER) || UNITY_PS4 || UNITY_PS5";
        public struct Parameters
        {
            public string Namespace;
            public string ScriptName;
            public string Path;
            public List<string> Usings;
            public List<Type> IncludedBaseTypes;
            public string[] MethodNameList;
            public Type[] DependencyComponentTypes;
            public string InterfaceImplementation;
            public string ClassImplementation;
        }

        public static void GenerateModifierCode<T>(Parameters parameters, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance, bool needEmpty = false, bool isEffectModifier = false) where T : Component
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

            var filteredProperties = members
                .Where(m => m.MemberType == MemberTypes.Property)
                .Select(m => m as PropertyInfo)
                .Where(p => p.CanWrite && !IsObsolete(p) && IsFromIncludedType(p))
                .ToList();

            var defaultValueMap = GetDefaultValueMap<T>(filteredProperties, parameters.DependencyComponentTypes);
            CreateModifierInterface<T>(parameters, filteredProperties, isEffectModifier);
            CreateModifierClass<T>(parameters, filteredProperties, defaultValueMap, isEffectModifier);
            if (needEmpty)
            {
                CreateEmptyModifierClass<T>(parameters, filteredProperties, defaultValueMap);
            }
            AssetDatabase.Refresh();
            
            bool IsFromIncludedType(MemberInfo m) => 
                (parameters.IncludedBaseTypes?.Contains(m.DeclaringType) ?? false) || m.DeclaringType == typeof(T);
            bool IsObsolete(MemberInfo m) => m.GetCustomAttribute(typeof(ObsoleteAttribute)) != null;
        }

        private static void CreateModifierInterface<T>(Parameters parameters, IEnumerable<MemberInfo> members, bool isEffectModifier) where T : Component
        {
            parameters.ScriptName = "I" + parameters.ScriptName;
            using (var file = CodeWriter.Write(parameters, CodeWriter.Type.Interface, parameters.InterfaceImplementation))
            {
                foreach (var member in members)
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
                    file.WriteLine(file.Indent + $"/// <inheritdoc cref=\"{typeof(T).Name}.{property.Name}\"/>");
                    file.WriteLine(file.Indent + $"{typeName} {property.Name} {{ get; set; }}");

                    if (condition != null)
                    {
                        file.WriteConditional_EndIf();
                    }
                    file.WriteLine();
                }

                if (parameters.MethodNameList != null)
                {
                    foreach (string methodName in parameters.MethodNameList)
                    {
                        var method = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                        if (method == null)
                        {
                            continue;
                        }

                        string returnType = method.ReturnType.GetSimpleTypeName();
                        method.GetParametersCode(out string parametersCode, out _);
                        file.WriteLine(file.Indent + $"/// <inheritdoc cref=\"{typeof(T).Name}.{methodName}\"/>");
                        file.WriteLine(file.Indent + $"{returnType} {methodName}({parametersCode});");
                        file.WriteLine();
                    }
                }
            }
        }

        private static void CreateModifierClass<T>(Parameters parameters, IEnumerable<MemberInfo> members, Dictionary<MemberInfo, string> defaultValueMap, bool isEffectModifier = false) where T : Component
        {
            string targetName = typeof(T).Name;
            string implementation = parameters.ClassImplementation + ", I" + parameters.ScriptName;
            if (!parameters.Usings.Contains(nameof(System)))
            {
                parameters.Usings.Add(nameof(System)); // for IDisposable
            }

            using (var file = CodeWriter.Write(parameters, CodeWriter.Type.Class, implementation, parameters.MethodNameList != null))
            {
                file.WriteLine(file.Indent + $"private {targetName} _source;");
                file.WriteLine(file.Indent + $"public {parameters.ScriptName}({targetName} source) => _source = source;");
                file.WriteLine();
                foreach (var member in members)
                {
                    WriteGetterSetterBody(file, file.Indent, member, GetDefaultValue(member, defaultValueMap));
                }

                if (isEffectModifier)
                {
                    WriteTransferValueToMethod(file, file.Indent, members);
                }
                else
                {
                    WriteResetDisposeMethod(file, file.Indent, members, defaultValueMap);
                }
            }


            if (parameters.MethodNameList != null)
            {
                using (var file = CodeWriter.WritePartial(parameters, CodeWriter.Type.Class, "Methods", implementation))
                {
                    foreach (string methodName in parameters.MethodNameList)
                    {
                        var method = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                        if (method == null)
                        {
                            continue;
                        }

                        string returnType = method.ReturnType.GetSimpleTypeName();
                        method.GetParametersCode(out string parametersCode, out string values);

                        file.WriteLine(file.Indent +
                                       $"public {returnType} {methodName}({parametersCode}) => _source.{methodName}({values});");
                        file.WriteLine();
                    }
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
                string varNameOfIsModified = $"_is{varName.ToPascal()}Modified";

                file.WriteLine(indent + $"private bool {varNameOfIsModified} = false;");
                using (file.WriteBraces($"public {typeName} {varName}", ref indent))
                {
                    file.WriteLine(indent + $"get => _source.{varName};");
                    using (file.WriteBraces("set", ref indent))
                    {
                        file.WriteLine(indent + $"{varNameOfIsModified} = true;");
                        file.WriteLine(indent + $"_source.{varName} = value;");
                    }
                }

                if (condition != null)
                {
                    file.WriteConditional_EndIf();
                }
                file.WriteLine();
            }

            static void WriteResetDisposeMethod(CodeWriter file, string indent, IEnumerable<MemberInfo> members, Dictionary<MemberInfo, string> defaultValueMap)
            {
                using (file.WriteBraces("public void Dispose()", ref indent))
                {
                    foreach (var member in members)
                    {
                        if (member is not PropertyInfo property)
                        {
                            continue;
                        }

                        if (TryGetConditionalCompilation(property, out string condition))
                        {
                            file.WriteConditional_If(condition);
                        }

                        string varName = property.Name;
                        string defaultValue = GetDefaultValue(member, defaultValueMap);

                        file.WriteLine(indent + $"if (_is{varName.ToPascal()}Modified) {{_source.{varName} = {defaultValue}; _is{varName.ToPascal()}Modified = false;}}");

                        if (condition != null)
                        {
                            file.WriteConditional_EndIf();
                        }
                    }
                }
            }

            static void WriteTransferValueToMethod(CodeWriter file, string indent, IEnumerable<MemberInfo> members)
            {
                var methodName = nameof(IAudioEffectModifier.TransferValueTo);
                using (file.WriteBraces($"public void {methodName}<T>(T target) where T : UnityEngine.Behaviour", ref indent))
                {
                    string targetName = typeof(T).Name;
                    file.WriteLine(indent + $"if (_source == null || !(target is {targetName} targetComponent)) return;");
                    file.WriteLine();

                    foreach (var member in members)
                    {
                        if (member is not PropertyInfo property)
                        {
                            continue;
                        }

                        string varName = property.Name;
                        string varNameOfIsModified = $"_is{varName.ToPascal()}Modified";

                        file.WriteLine(indent + $"if ({varNameOfIsModified}) targetComponent.{varName} = _source.{varName};");
                    }

                    file.WriteLine();
                    file.WriteLine(indent + "_source = targetComponent;");
                }
            }
        }

        private static void CreateEmptyModifierClass<T>(Parameters parameters, IEnumerable<MemberInfo> filteredMembers, Dictionary<MemberInfo, string> defaultValueMap) where T : Component
        {
            string interfaceName = "I" + parameters.ScriptName;
            parameters.ScriptName = "Empty" + parameters.ScriptName;
            using (var file = CodeWriter.Write(parameters, CodeWriter.Type.Class, interfaceName))
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
                }

                if (parameters.MethodNameList != null)
                {
                    foreach (string methodName in parameters.MethodNameList)
                    {
                        var method = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                        if (method == null)
                        {
                            continue;
                        }

                        string outContent = GetOutContent(method);
                        string content = method.ReturnType != typeof(void) ? $"return default;" : null;
                        string returnType = method.ReturnType.GetSimpleTypeName();
                        method.GetParametersCode(out string parametersCode, out _);
                        file.WriteLine(file.Indent + $"public {returnType} {methodName}({parametersCode}) {{ {outContent}{content} }}");
                        file.WriteLine();
                    }
                }


                string GetOutContent(MethodInfo method)
                {
                    string result = string.Empty;
                    var parameters = method.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        if(parameter.IsOut)
                        {
                            result += $"{parameter.Name} = default; ";
                        }
                    }
                    return result;
                }
            }
        }

        static Dictionary<MemberInfo, string> GetDefaultValueMap<T>(IEnumerable<MemberInfo> filteredMembers, Type[] dependencies = null) where T : Component
        {
            var temp = new GameObject("Temp");
            temp.hideFlags = HideFlags.HideAndDontSave;

            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    temp.AddComponent(dependency);
                }
            }

            T component = temp.AddComponent<T>();
            Dictionary<MemberInfo, string> defaultValueMap = new Dictionary<MemberInfo, string>();
            foreach (PropertyInfo property in filteredMembers)
            {
                object value = property?.GetValue(component);
                if (value != null)
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
                    else if (property.PropertyType == typeof(float))
                    {
                        valueString += "f";
                    }
                    else if (property.PropertyType == typeof(AnimationCurve) && value is AnimationCurve curve)
                    {
                        valueString = $"AnimationCurve.Constant(0f, 0f, {curve[0].value.ToString()}f)";
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
            return member.DeclaringType == null || member.DeclaringType.IsValueType ? "default" : "null";
        }

        public static void WriteUsings(this StreamWriter writer, IEnumerable<string> usings)
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
            Type v when v == typeof(void) => "void",
            Type f when f == typeof(float).MakeByRefType() => "float",
            Type i when i == typeof(int).MakeByRefType() => "int",
            Type b when b == typeof(bool).MakeByRefType() => "bool",
            Type l when l == typeof(long).MakeByRefType() => "long",
            Type d when d == typeof(double).MakeByRefType() => "double",
            Type c when c == typeof(char).MakeByRefType() => "char",
            Type s when s == typeof(string).MakeByRefType() => "string",
            _ => type.Name,
        };

        private static void GetParametersCode(this MethodInfo method, out string methodParameters, out string valueParameters)
        {
            methodParameters = string.Empty;
            valueParameters = string.Empty;
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length;i++)
            {
                if(i > 0)
                {
                    methodParameters += ", ";
                    valueParameters += ", ";
                }
                var para = parameters[i];
                string outString = para.IsOut ? "out " : string.Empty;
                methodParameters += $"{outString}{para.ParameterType.GetSimpleTypeName()} {para.Name}";
                valueParameters += outString + para.Name;
            }
        }

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
}
#endif