using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.CodeGeneration
{
    class CodeWriter
    {
        private StringBuilder _code = new StringBuilder();
        private int _indent;
        private string _indentSequence = "\t";
        private bool _newLine;

        public string IndentSequence { get => _indentSequence; set => _indentSequence = value ?? throw new ArgumentNullException(); }
        public string Code => _code.ToString();

        public override string ToString() => _code.ToString();

        public void Write(string str)
        {
            CheckIndent();
            _code.Append(str);
        }

        public void Write(char ch)
        {
            CheckIndent();
            _code.Append(ch);
        }

        public void WriteLine(string str)
        {
            CheckIndent();
            _code.AppendLine(str);
            _newLine = true;
        }

        public void WriteLine()
        {
            _code.AppendLine();
            _newLine = true;
        }

        private void CheckIndent()
        {
            if (_newLine)
            {
                _newLine = false;
                for (int i = 0; i < _indent; i++)
                {
                    _code.Append(_indentSequence);
                }
            }
        }

        public IndentScope Indent(string startString = "{", string endString = "}") => new IndentScope(this, startString, endString);

        public class IndentScope : IDisposable
        {
            private CodeWriter _code;
            private string _endString;

            public IndentScope(CodeWriter code, string startString, string endString)
            {
                _code = code ?? throw new ArgumentNullException(nameof(code));
                _endString = endString;

                if (!string.IsNullOrEmpty(startString))
                {
                    _code.Write(startString);
                    _code._indent++;
                    _code.WriteLine();
                }
            }

            public void Dispose()
            {
                _code._indent--;
                if (!string.IsNullOrEmpty(_endString))
                {
                    _code.Write(_endString);
                }
            }
        }
    }
}
