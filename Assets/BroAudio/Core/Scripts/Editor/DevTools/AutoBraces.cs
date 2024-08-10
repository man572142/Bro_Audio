using System.IO;
using System;

namespace Ami.Extension.Reflection
{
    public struct AutoBraces : IDisposable
    {
        private StreamWriter _writer;
        private string _indent;

        public AutoBraces(StreamWriter writer, string text, ref string indent, string spaces = "    ")
        {
            _writer = writer;
            _indent = indent;
            if (string.IsNullOrEmpty(text))
            {
                _writer = null;
                _indent = null;
                return;
            }

            writer?.WriteLine(indent + text);
            writer?.WriteLine(indent + "{");
            indent += spaces;
        }

        public void Dispose()
        {
            _writer?.WriteLine(_indent + "}");
        }
    }

    public static class AutoBracesFactory
    {
        public static AutoBraces WriteBraces(this StreamWriter writer, string text, ref string indent)
        {
            return new AutoBraces(writer, text, ref indent);
        }
    }
}
