#if BroAudio_DevOnly
using System.IO;
using System;
using static Ami.Extension.Reflection.ProxyModifierCodeGenerator;

namespace Ami.Extension.Reflection
{
    public class CodeWriter : IDisposable
    {
        public enum Type { Class, Interface, Struct }

        private StreamWriter _file;
        private AutoBraces _namespace;
        private AutoBraces _codeBlock;

        public string Indent = string.Empty;

        public static implicit operator StreamWriter(CodeWriter writer) => writer._file;

        public CodeWriter(string directory, string codeName)
        {
            _file = File.CreateText(Path.Combine(directory, codeName + ".cs"));
        }

        public void Dispose()
        {
            _codeBlock.Dispose();
            _namespace.Dispose();
            _file?.Dispose();
        }

        public void Write(string[] usings, string @namespace, string codeName, Type scriptType, string implementation = "", bool isPartial = false)
        {
            _file.WriteLine(Title);
            _file.WriteUsings(usings);
            _file.WriteLine();
            string partial = isPartial ? "partial " : string.Empty;
            _namespace = _file.WriteBraces("namespace " + @namespace, ref Indent);
            _codeBlock = _file.WriteBraces("public " + partial + GetScriptType(scriptType) + " " + codeName + implementation, ref Indent);
        }

        public void WriteLine(string text = "")
        {
            _file.WriteLine(text);
        }

        public void WriteConditional_If(string condition)
        {
            _file.Write("#if " + condition);
            _file.WriteLine();
        }

        public void WriteConditional_EndIf()
        {
            _file.Write("#endif");
            _file.WriteLine();
        }

        public AutoBraces WriteBraces(string text, ref string indent)
        {
            return _file.WriteBraces(text, ref indent);
        }

        private string GetScriptType(Type scriptType) => scriptType switch
        {
            Type.Class => "class",
            Type.Interface => "interface",
            Type.Struct => "struct",
            _ => throw new NotImplementedException(),
        };

        public static CodeWriter Write(Parameters parameters, Type scriptType, string implementation = "", bool isPartial = false)
        {
            var code = new CodeWriter(parameters.Path, parameters.ScriptName);
            code.Write(parameters.Usings, parameters.Namespace, parameters.ScriptName, scriptType, implementation, isPartial);
            return code;
        }

        public static CodeWriter WritePartial(Parameters parameters, Type scriptType, string partialName, string implementation = "")
        {
            var code = new CodeWriter(parameters.Path, parameters.ScriptName + "." + partialName);
            code.Write(parameters.Usings, parameters.Namespace, parameters.ScriptName, scriptType, implementation, true);
            return code;
        }
    }
}
#endif