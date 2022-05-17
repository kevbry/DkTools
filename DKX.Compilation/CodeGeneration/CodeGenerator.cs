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
        private OpCodeParser _ops;
        private CodeGenerationContext _cgc;
        private CodeWriter _writer;
        private ISourceCodeReporter _reporter;

        public CodeGenerator(string code, CodeGenerationContext cgc, CodeWriter writer, ISourceCodeReporter reporter)
        {
            _ops = new OpCodeParser(code);
            _cgc = cgc ?? throw new ArgumentNullException(nameof(cgc));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public bool EndOfCode => _ops.EndOfFile;

        public void GenerateBody(bool mustEndWithClose)
        {
            do
            {
                if (!GenerateStatement()) break;
            }
            while (_ops.ReadDelim(throwOnError: false));
        }

        public bool GenerateStatement()
        {
            if (_ops.EndOfFile) return false;

            // TODO: to implement later
            //if (_ops.ReadStatement())
            //{
            //    switch (_ops.Text)
            //    {
            //        case "if":
            //            GenerateIfStatement();
            //            return true;
            //        default:
            //            throw new InvalidOpCodeSourceException($"Invalid statement type '{_ops.Text}'.");
            //    }
            //}

            var exp = GenerateFragment();
            _writer.Write(exp.ToString());
            if (!exp.Terminated) _writer.Write(';');
            _writer.WriteLine();
            return true;
        }

        public CodeFragment GenerateFragment()
        {
            switch (_ops.Read())
            {
                case OpCodeType.None:
                    throw new InvalidOpCodeSourceException("Unexpected end of opcode source.");
                case OpCodeType.OpCode:
                    return GenerateOp(_ops.Text, _ops.Span);
                case OpCodeType.Variable:
                    var text = _ops.Text;
                    if (_cgc.TryGetVariable(text, out var dataType))
                    {
                        return new CodeFragment(text, dataType, OpPrec.None, terminated: false, sourceSpan: _ops.Span, readOnly: false);
                    }
                    throw new InvalidOpCodeSourceException($"Unknown variable '{text}'.");
                case OpCodeType.Number:
                    text = _ops.Text;
                    return new CodeFragment(text, GetDataTypeFromNumberText(text), OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                case OpCodeType.String:
                    text = _ops.Text;
                    return new CodeFragment(CodeParser.StringToStringLiteral(text), GetDataTypeFromStringLiteral(text), OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                case OpCodeType.Char:
                    text = _ops.Text;
                    return new CodeFragment(CodeParser.CharToCharLiteral(text[0]), DataType.Char, OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                default:
                    throw new InvalidOpCodeSourceException($"Unexpected token '{_ops.Text}' in opcode source.");
            }
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

        private CodeFragment GenerateOp(string opName, CodeSpan opSpan)
        {
            switch (opName)
            {
                case "asn": return Op_Assign(opSpan);
                case "dec": return Op_Increment(opSpan, decrement: true);
                case "inc": return Op_Increment(opSpan, decrement: false);
                default: throw new InvalidOpCodeSourceException($"Invalid op code '{opName}'.");
            }
        }

        private CodeFragment Op_Increment(CodeSpan opSpan, bool decrement)
        {
            _ops.ReadOpen();

            var leftFrag = GenerateFragment();
            if (leftFrag.IsEmpty || leftFrag.ReadOnly) _reporter.ReportItem(opSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, decrement ? "--" : "++");
            else if (!leftFrag.DataType.IsSuitableForIncDec) _reporter.ReportItem(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, decrement ? "--" : "++");

            _ops.ReadClose();

            return new CodeFragment(
                text: string.Concat(leftFrag.Protect(OpPrec.AddSub), decrement ? " -= 1" : " += 1"),
                dataType: leftFrag.DataType,
                precedence: OpPrec.Assign,
                terminated: false,
                sourceSpan: leftFrag.SourceSpan.Envelope(opSpan),
                readOnly: false);
        }

        private CodeFragment Op_Assign(CodeSpan opSpan)
        {
            _ops.ReadOpen();

            var leftFrag = GenerateFragment();
            if (leftFrag.IsEmpty || leftFrag.ReadOnly) _reporter.ReportItem(opSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, '=');

            _ops.ReadDelim();

            var rightFrag = GenerateFragment();
            if (rightFrag.IsEmpty || !rightFrag.DataType.IsValue) _reporter.ReportItem(opSpan, ErrorCode.OperatorExpectsValueOnRight, '=');

            _ops.ReadClose();

            // TODO: Check data type conversions

            return new CodeFragment(
                text: string.Concat(leftFrag.Protect(OpPrec.Assign), " = ", rightFrag.Protect(OpPrec.Assign)),
                dataType: leftFrag.DataType,
                precedence: OpPrec.Assign,
                terminated: false,
                sourceSpan: leftFrag.SourceSpan.Envelope(rightFrag.SourceSpan),
                readOnly: false);

        }
    }
}
