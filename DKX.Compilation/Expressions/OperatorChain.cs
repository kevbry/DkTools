using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Expressions
{
    class OperatorChain : Chain
    {
        private Operator _op;
        private Chain _left;
        private Chain _right;

        public OperatorChain(Operator op, Chain left, Chain right)
            : base(right != null ? left.Span.Envelope(right.Span) : left.Span)
        {
            _op = op;
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right;

            if (_right == null && !_op.IsUnaryPost()) throw new ArgumentNullException(nameof(right));
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType
        {
            get
            {
                if (_op.YieldsBoolean()) return DataType.Bool;
                return _left.DataType;
            }
        }

        public override DataType InferredDataType
        {
            get
            {
                if (_op.YieldsBoolean()) return DataType.Bool;
                return _left.InferredDataType;
            }
        }

        public override CodeFragment ToWbdkCode_Read(ISourceCodeReporter report)
        {
            return ToWbdkCode(reading: true, report);
        }

        public override CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            return ToWbdkCode(reading: false, report);
        }

        private CodeFragment ToWbdkCode(bool reading, ISourceCodeReporter report)
        {
            CodeFragment leftFrag, rightFrag;
            OpPrec prec;

            switch (_op)
            {
                case Operator.Increment:
                case Operator.Decrement:
                    leftFrag = _left.ToWbdkCode_Read(report);
                    if (leftFrag.ReadOnly) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, _op.GetText());
                    if (!leftFrag.DataType.IsSuitableForIncDec) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    return new CodeFragment($"{leftFrag.Protect(OpPrec.IncDec)} {_op.GetText()} 1", leftFrag.DataType, OpPrec.IncDec, Span, readOnly: false);

                case Operator.Add:
                case Operator.Subtract:
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.Modulus:
                    if (!reading) throw new CodeException(Span, ErrorCode.OperatorResultCannotBeWrittenTo, _op.GetText());
                    leftFrag = _left.ToWbdkCode_Read(report);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    rightFrag = _right.ToWbdkCode_Read(report);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(rightFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    ConversionValidator.CheckConversion(leftFrag.DataType, rightFrag, report);
                    prec = _op.GetPrecedence();
                    return new CodeFragment($"{leftFrag.Protect(prec)} {_op.GetText()} {rightFrag.Protect(prec)}", leftFrag.DataType, prec, Span, readOnly: true);

                case Operator.Assign:
                    rightFrag = _right.ToWbdkCode_Read(report);
                    ConversionValidator.CheckConversion(_left.DataType, rightFrag, report);
                    leftFrag = _left.ToWbdkCode_Write(rightFrag, report);
                    return new CodeFragment($"{leftFrag.Protect(OpPrec.Assign)} = {rightFrag.Protect(OpPrec.Assign)}", leftFrag.DataType, OpPrec.Assign, Span, readOnly: true);

                case Operator.AssignAdd:
                case Operator.AssignSubtract:
                case Operator.AssignMultiply:
                case Operator.AssignDivide:
                case Operator.AssignModulus:
                    leftFrag = _left.ToWbdkCode_Read(report);
                    if (leftFrag.ReadOnly) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, _op.GetText());
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    rightFrag = _right.ToWbdkCode_Read(report);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(rightFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    ConversionValidator.CheckConversion(leftFrag.DataType, rightFrag, report);
                    prec = _op.GetPrecedence();
                    return new CodeFragment($"{leftFrag.Protect(prec)} {_op.GetText()} {rightFrag.Protect(prec)}", leftFrag.DataType, prec, Span, readOnly: true);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
