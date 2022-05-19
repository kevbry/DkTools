using DK.Code;
using System.Text;

namespace DKX.Compilation.CodeGeneration.OpCodes
{
    public class OpCodeGenerator
    {
        private StringBuilder _code = new StringBuilder();

        public override string ToString() => _code.ToString();

        private void WriteSpan(int parentOffset, CodeSpan span)
        {
            if (parentOffset < 0) return;

            var start = span.Start - parentOffset;
            var end = span.End - parentOffset;
            if (start < 0) start = 0;
            if (end < 0) end = 0;
            var len = end - start;

            if (len < 100)
            {
                _code.Append(':');
                _code.Append(start * 100 + len);
            }
            else
            {
                _code.Append(':');
                _code.Append(start);
                _code.Append(':');
                _code.Append(len);
            }
        }

        public void WriteOpCode(string opCodeName, int parentOffset, CodeSpan fullOpCodeSpan)
        {
            _code.Append(opCodeName);
            WriteSpan(parentOffset, fullOpCodeSpan);
        }

        public void WriteVariable(string variableName, int parentOffset, CodeSpan variableSpan)
        {
            _code.Append('$');
            _code.Append(variableName);
            WriteSpan(parentOffset, variableSpan);
        }

        public void WriteStringLiteral(string rawText, int parentOffset, CodeSpan literalSpan)
        {
            _code.Append(CodeParser.StringToStringLiteral(rawText));
            WriteSpan(parentOffset, literalSpan);
        }

        public void WriteNumberLiteral(string numberText, int parentOffset, CodeSpan literalSpan)
        {
            _code.Append(numberText);
            WriteSpan(parentOffset, literalSpan);
        }

        public void WriteBoolLiteral(bool value, int parentOffset, CodeSpan literalSpan)
        {
            _code.Append('!');
            _code.Append(value ? 'T' : 'F');
            WriteSpan(parentOffset, literalSpan);
        }

        public void WriteOpen() => _code.Append('(');
        public void WriteClose() => _code.Append(')');
        public void WriteDelim() => _code.Append(',');
    }
}
