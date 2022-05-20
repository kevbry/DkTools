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
        private int _codeOffset;
        private CodeGenerationContext _cgc;
        private CodeWriter _writer;
        private ISourceCodeReporter _reporter;

        /// <summary>
        /// Creates a new code generator object.
        /// </summary>
        /// <param name="code">The op code source that will be used to generate the code.</param>
        /// <param name="codeOffset">The starting position of the code body in the original DKX source. Used for generating spans on errors.</param>
        /// <param name="cgc">Context for generating code.</param>
        /// <param name="writer">Code writer</param>
        /// <param name="reporter">Error reporter</param>
        public CodeGenerator(string code, int codeOffset, CodeGenerationContext cgc, CodeWriter writer, ISourceCodeReporter reporter)
        {
            _ops = new OpCodeParser(code);
            _codeOffset = codeOffset;
            _cgc = cgc ?? throw new ArgumentNullException(nameof(cgc));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public bool EndOfCode => _ops.EndOfFile;

        public void GenerateBody()
        {
            GenerateBody(mustEndWithClose: false, _codeOffset);
        }

        private void GenerateBody(bool mustEndWithClose, int parentOffset)
        {
            var first = true;
            while (true)
            {
                if (mustEndWithClose)
                {
                    if (_ops.ReadClose(throwOnError: false)) break;
                }
                else
                {
                    if (_ops.EndOfFile) break;
                }

                if (first) first = false;
                else _ops.ReadDelim();

                GenerateStatement(parentOffset);
            }
        }

        private void GenerateStatement(int parentOffset)
        {
            if (_ops.EndOfFile) throw new InvalidOpCodeSourceException("Unexpected end of file where statement was expected.");

            var spanOffset = _ops.SpanOffset;
            _ops.SpanOffset = parentOffset;
            try
            {
                var resetPos = _ops.Position;
                var op = _ops.ReadOpCode(throwOnError: false);
                if (op != null)
                {
                    switch (op)
                    {
                        case OpCode.For:
                            Statement_For(_ops.Span);
                            return;
                        case OpCode.If:
                            Statement_If(_ops.Span);
                            return;
                        case OpCode.While:
                            Statement_While(_ops.Span);
                            return;
                        default:
                            _ops.Position = resetPos;
                            break;
                    }
                }

                var exp = GenerateFragment(parentOffset);
                _writer.Write(exp.ToString());
                if (!exp.Terminated) _writer.Write(';');
                _writer.WriteLine();
            }
            finally
            {
                _ops.SpanOffset = spanOffset;
            }
        }

        private CodeFragment GenerateFragment(int parentOffset)
        {
            var spanOffset = _ops.SpanOffset;
            _ops.SpanOffset = parentOffset;
            try
            {
                switch (_ops.Read())
                {
                    case OpCodeType.None:
                        throw new InvalidOpCodeSourceException("Unexpected end of opcode source.");
                    case OpCodeType.OpCode:
                        return GenerateOp(_ops.Text, _ops.Span);
                    case OpCodeType.Variable:
                        var text = _ops.Text;
                        if (_cgc.TryGetVariable(text, out var wbdkName, out var dataType))
                        {
                            return new CodeFragment(wbdkName, dataType, OpPrec.None, terminated: false, sourceSpan: _ops.Span, readOnly: false);
                        }
                        throw new InvalidOpCodeSourceException($"Unknown variable '{text}'.");
                    case OpCodeType.Number:
                        text = _ops.Text;
                        return new CodeFragment(text, GetDataTypeFromNumberText(text), OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                    case OpCodeType.String:
                        text = _ops.Text;
                        return new CodeFragment(CodeParser.StringToStringLiteral(text), GetDataTypeFromStringLiteral(text), OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                    case OpCodeType.Char:
                        return new CodeFragment(CodeParser.CharToCharLiteral(_ops.Text[0]), DataType.Char, OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                    case OpCodeType.Bool:
                        return new CodeFragment(_ops.Text == OpCodeParser.True ? "1" : "0", DataType.Bool, OpPrec.None, terminated: false, _ops.Span, readOnly: true);
                    default:
                        throw new InvalidOpCodeSourceException($"Unexpected token '{_ops.Text}' in opcode source.");
                }
            }
            finally
            {
                _ops.SpanOffset = spanOffset;
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

        #region Op Codes
        private CodeFragment GenerateOp(string opName, CodeSpan opSpan)
        {
            switch (opName)
            {
                case OpCode.Assign: return Op_Assign(opSpan);
                case OpCode.CompareEQ: return Op_Comparison(opSpan, Operator.Equal);
                case OpCode.CompareGE: return Op_Comparison(opSpan, Operator.GreaterEqual);
                case OpCode.CompareGT: return Op_Comparison(opSpan, Operator.GreaterThan);
                case OpCode.CompareLE: return Op_Comparison(opSpan, Operator.LessEqual);
                case OpCode.CompareLT: return Op_Comparison(opSpan, Operator.LessThan);
                case OpCode.CompareNE: return Op_Comparison(opSpan, Operator.NotEqual);
                case OpCode.Decrement: return Op_Increment(opSpan, decrement: true);
                case OpCode.Increment: return Op_Increment(opSpan, decrement: false);
                default: throw new InvalidOpCodeSourceException($"Invalid op code '{opName}'.");
            }
        }

        private CodeFragment Op_Increment(CodeSpan opSpan, bool decrement)
        {
            _ops.ReadOpen();

            var leftFrag = GenerateFragment(opSpan.Start);
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

            var leftFrag = GenerateFragment(opSpan.Start);
            if (leftFrag.IsEmpty || leftFrag.ReadOnly) _reporter.ReportItem(opSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, '=');

            _ops.ReadDelim();

            var rightFrag = GenerateFragment(opSpan.Start);
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

        private CodeFragment Op_Comparison(CodeSpan opSpan, Operator op)
        {
            _ops.ReadOpen();

            var leftFrag = GenerateFragment(opSpan.Start);
            if (leftFrag.IsEmpty) _reporter.ReportItem(opSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, op.GetText());

            _ops.ReadDelim();

            var rightFrag = GenerateFragment(opSpan.Start);
            if (leftFrag.IsEmpty) _reporter.ReportItem(opSpan, ErrorCode.OperatorExpectsValueOnRight, op.GetText());

            _ops.ReadClose();

            var prec = op.GetPrecedence();
            return new CodeFragment(
                text: string.Concat(leftFrag.Protect(prec), " ", op.GetText(), " ", rightFrag.Protect(prec)),
                dataType: DataType.Bool,
                precedence: prec,
                terminated: false,
                sourceSpan: leftFrag.SourceSpan.Envelope(rightFrag.SourceSpan),
                readOnly: true);
        }
        #endregion

        #region Statements
        private void Statement_If(CodeSpan keywordSpan)
        {
            _ops.ReadOpen();

            var first = true;
            while (true)
            {
                if (first == false)
                {
                    if (_ops.ReadClose(throwOnError: false)) return;
                    _ops.ReadDelim();
                }
                if (first == false && _ops.ReadDelim(throwOnError: false))
                {
                    // 'else' with no condition

                    _writer.WriteLine("else");

                    _ops.ReadOpen();
                    using (_writer.Indent())
                    {
                        GenerateBody(mustEndWithClose: true, keywordSpan.Start);
                    }
                    _writer.WriteLine();

                    _ops.ReadClose();   // Must end after the 'else' with no condition.
                    return;
                }
                else
                {
                    // 'if' or 'else if'

                    if (first) _writer.Write("if ");
                    else _writer.Write("else if ");

                    var condition = GenerateFragment(keywordSpan.Start);
                    if (condition.DataType.BaseType != BaseType.Bool) _reporter.ReportItem(condition.SourceSpan, ErrorCode.ConditionMustBeBool);

                    _writer.Write(condition.ToString());
                    _writer.WriteLine();
                    _ops.ReadDelim();

                    _ops.ReadOpen();
                    using (_writer.Indent())
                    {
                        GenerateBody(mustEndWithClose: true, keywordSpan.Start);
                    }
                    _writer.WriteLine();
                }

                first = false;
            }
        }

        private void Statement_While(CodeSpan keywordSpan)
        {
            _ops.ReadOpen();

            var condition = GenerateFragment(keywordSpan.Start);
            if (condition.DataType.BaseType != BaseType.Bool) _reporter.ReportItem(condition.SourceSpan, ErrorCode.ConditionMustBeBool);

            _ops.ReadDelim();
            _ops.ReadOpen();

            _writer.Write("while ");
            _writer.Write(condition);
            _writer.WriteLine();
            using (_writer.Indent())
            {
                GenerateBody(mustEndWithClose: true, keywordSpan.Start);
            }
            _writer.WriteLine();

            _ops.ReadClose();
        }

        private void Statement_For(CodeSpan keywordSpan)
        {
            _ops.ReadOpen();

            // Initializer
            _ops.ReadOpen();
            GenerateBody(mustEndWithClose: true, keywordSpan.Start);
            _ops.ReadDelim();

            _writer.Write("for (; ");

            // Condition
            var condition = GenerateFragment(keywordSpan.Start);
            if (condition.DataType.BaseType != BaseType.Bool) _reporter.ReportItem(condition.SourceSpan, ErrorCode.ConditionMustBeBool);
            _writer.Write(condition);
            _writer.Write("; ");
            _ops.ReadDelim();

            // Iteration
            if (!_ops.ReadDelim(throwOnError: false))
            {
                var iteration = GenerateFragment(keywordSpan.Start);
                _writer.Write(iteration);
                _ops.ReadDelim();
            }
            _writer.Write(')');
            _writer.WriteLine();

            // Body
            _ops.ReadOpen();
            using (_writer.Indent())
            {
                GenerateBody(mustEndWithClose: true, keywordSpan.Start);
            }
            _writer.WriteLine();

            _ops.ReadClose();
        }
        #endregion
    }
}
