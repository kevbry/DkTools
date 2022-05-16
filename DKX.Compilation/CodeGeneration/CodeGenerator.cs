using DK;
using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerator
    {
        private CodeParser _code;
        private CodeGenerationContext _cgc;
        private CodeWriter _writer;
        private ISourceCodeReporter _reporter;

        public CodeGenerator(string opCodeSource, CodeGenerationContext cgc, CodeWriter writer, ISourceCodeReporter reporter)
        {
            _code = new CodeParser(opCodeSource);
            _cgc = cgc ?? throw new ArgumentNullException(nameof(cgc));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public bool EndOfCode => _code.EndOfFile;

        public void GenerateBody(bool mustEndWithClose)
        {
            while (!_code.EndOfFile)
            {
                var opSpan = ReadSpan();
                if (!_code.ReadWord()) throw new InvalidOpCodeSourceException("Expected op code to follow span.");

                // TODO: Allow for statements which take a different structure than op codes.

                GenerateOp(_code.Text, opSpan);
                _writer.Write(';');
                _writer.WriteLine();
            }
        }

        private CodeSpan ReadSpan()
        {
            if (!_code.ReadExact('[')) throw new InvalidOpCodeSourceException("Expected code span '['.");
            if (!_code.ReadNumber()) throw new InvalidOpCodeSourceException("Expected code span starting position.");
            if (!int.TryParse(_code.Text, out var start)) throw new InvalidOpCodeSourceException($"Invalid code span starting position '{_code.Text}'.");
            if (!_code.ReadExact(':')) throw new InvalidOpCodeSourceException("Expected code span ':'.");
            if (!_code.ReadNumber()) throw new InvalidOpCodeSourceException("Expected code span length.");
            if (!int.TryParse(_code.Text, out var length)) throw new InvalidOpCodeSourceException($"Invalid code span length '{_code.Text}'.");
            if (!_code.ReadExact(']')) throw new InvalidOpCodeSourceException("Expected code span ']'.");

            return new CodeSpan(start, start + length);
        }

        private string ReadVarName()
        {
            if (!_code.ReadWord()) throw new InvalidOpCodeSourceException("Expected variable name.");
            return _code.Text;
        }

        private string ReadNumber()
        {
            if (!_code.ReadNumber()) throw new InvalidOpCodeSourceException("Expected number.");
            return _code.Text;
        }

        private string ReadString()
        {
            if (!_code.ReadStringLiteral()) throw new InvalidOpCodeSourceException("Expected string.");
            return _code.Text;
        }

        private DataType GetDataTypeFromNumberText(string number)
        {
            var width = 0;
            var scale = 0;
            var gotDot = false;
            var signed = false;

            foreach (var ch in number)
            {
                if (ch.IsDigit())
                {
                    width++;
                    if (gotDot) scale++;
                }
                else if (ch == '.') gotDot = true;
                else if (ch == '-') signed = true;
            }

            if (width < 1) width = 1;
            return new DataType(signed ? BaseType.Numeric : BaseType.UNumeric, width: (byte)width, scale: (byte)scale);
        }

        private DataType GetDataTypeFromStringLiteral(string rawText)
        {
            var len = rawText.Length;
            if (len >= 255) return DataType.String255;
            else if (len < 1) len = 1;
            return new DataType(BaseType.String, width: (byte)len);
        }

        private void GenerateOp(string opName, CodeSpan opSpan)
        {
            switch (opName)
            {
                //case "asn":
                //    Op_Assign(opSpan);
                //    break;
                case "dec": Op_Increment(decrement: true); break;
                case "inc": Op_Increment(decrement: false); break;
                case "ret": Op_Return(withVariable: false); break;
                case "retv": Op_Return(withVariable: true); break;
                case "seti": Op_SetVarToIdentifier(); break;
                case "setn": Op_SetVarToNumber(); break;
                case "sets": Op_SetVarToString(); break;
                case "setv": Op_SetVarToVar(); break;
                default:
                    throw new InvalidOpCodeSourceException($"Invalid op code '{opName}'.");
            }
        }

        private void Op_Increment(bool decrement)
        {
            var varName = ReadVarName();

            _writer.Write(varName);
            _writer.Write(decrement ? " -= 1" : " += 1");
        }

        private void Op_Return(bool withVariable)
        {
            _writer.Write("return");
            if (withVariable)
            {
                _writer.Write(' ');
                _writer.Write(ReadVarName());
            }
        }

        private void Op_SetVarToVar()
        {
            _writer.Write(ReadVarName());
            _writer.Write(" = ");
            _writer.Write(ReadVarName());
        }

        private void Op_SetVarToIdentifier()
        {
            _writer.Write(ReadVarName());
            _writer.Write(" = ");
            _writer.Write(ReadVarName());
        }

        private void Op_SetVarToNumber()
        {
            _writer.Write(ReadVarName());
            _writer.Write(" = ");
            _writer.Write(ReadNumber());
        }

        private void Op_SetVarToString()
        {
            _writer.Write(ReadVarName());
            _writer.Write(" = ");
            _writer.Write(ReadString());
        }
    }
}
