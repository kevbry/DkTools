using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

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

        public override async Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            return await ToWbdkCodeAsync(reading: true, report);
        }

        public override async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            return await ToWbdkCodeAsync(reading: false, report);
        }

        private async Task<CodeFragment> ToWbdkCodeAsync(bool reading, ISourceCodeReporter report)
        {
            CodeFragment leftFrag, rightFrag;
            OpPrec prec;

            switch (_op)
            {
                case Operator.Increment:
                case Operator.Decrement:
                    leftFrag = await _left.ToWbdkCode_ReadAsync(report);
                    if (leftFrag.ReadOnly) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, _op.GetText());
                    if (!leftFrag.DataType.IsSuitableForIncDec) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    return new CodeFragment($"{leftFrag.Protect(OpPrec.IncDec)} {_op.GetText()} 1", leftFrag.DataType, OpPrec.IncDec, Span, readOnly: false);

                case Operator.Add:
                case Operator.Subtract:
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.Modulus:
                    if (!reading) throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo, _op.GetText());
                    leftFrag = await _left.ToWbdkCode_ReadAsync(report);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    rightFrag = await _right.ToWbdkCode_ReadAsync(report);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(rightFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    await ConversionValidator.CheckConversionAsync(leftFrag.DataType, rightFrag, report);
                    prec = _op.GetPrecedence();
                    return new CodeFragment($"{leftFrag.Protect(prec)} {_op.GetText()} {rightFrag.Protect(prec)}", leftFrag.DataType, prec, Span, readOnly: true);

                case Operator.Assign:
                    rightFrag = await _right.ToWbdkCode_ReadAsync(report);
                    leftFrag = await _left.ToWbdkCode_WriteAsync(rightFrag, report);
                    return leftFrag.Protect(OpPrec.Assign);

                case Operator.AssignAdd:
                case Operator.AssignSubtract:
                case Operator.AssignMultiply:
                case Operator.AssignDivide:
                case Operator.AssignModulus:
                    leftFrag = await _left.ToWbdkCode_ReadAsync(report);
                    if (leftFrag.ReadOnly) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, _op.GetText());
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    rightFrag = await _right.ToWbdkCode_ReadAsync(report);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(rightFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    await ConversionValidator.CheckConversionAsync(leftFrag.DataType, rightFrag, report);
                    prec = _op.GetPrecedence();
                    return new CodeFragment($"{leftFrag.Protect(prec)} {_op.GetText()} {rightFrag.Protect(prec)}", leftFrag.DataType, prec, Span, readOnly: true);

                default:
                    throw new NotImplementedException();
            }
        }

        public override async Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull)
        {
            ConstantValue left, right;
            switch (_op)
            {
                case Operator.Add:
                case Operator.Subtract:
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.Modulus:
                    left = await _left.GetConstantOrNullAsync(reportOrNull);
                    right = await _right.GetConstantOrNullAsync(reportOrNull);
                    if (left != null && right != null) return await left.GetMathResultOrNullAsync(_op, right, reportOrNull);
                    return null;

                case Operator.And:
                    left = await _left.GetConstantOrNullAsync(reportOrNull);
                    right = await _right.GetConstantOrNullAsync(reportOrNull);
                    if (left != null && !left.IsBool) await reportOrNull?.ReportAsync(_left.Span, ErrorCode.ConditionMustBeBool);
                    if (right != null && !right.IsBool) await reportOrNull?.ReportAsync(_right.Span, ErrorCode.ConditionMustBeBool);
                    if (left != null && right != null) return new BoolConstantValue(left.Bool && right.Bool, Span);
                    return null;

                case Operator.Or:
                    left = await _left.GetConstantOrNullAsync(reportOrNull);
                    right = await _right.GetConstantOrNullAsync(reportOrNull);
                    if (left != null && !left.IsBool) await reportOrNull?.ReportAsync(_left.Span, ErrorCode.ConditionMustBeBool);
                    if (right != null && !right.IsBool) await reportOrNull?.ReportAsync(_right.Span, ErrorCode.ConditionMustBeBool);
                    if (left != null && right != null) return new BoolConstantValue(left.Bool || right.Bool, Span);
                    return null;

                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.LessThan:
                case Operator.LessEqual:
                case Operator.GreaterThan:
                case Operator.GreaterEqual:
                    left = await _left.GetConstantOrNullAsync(reportOrNull);
                    right = await _right.GetConstantOrNullAsync(reportOrNull);
                    if (left != null && right != null)
                    {
                        var result = await left.GetComparisonResultOrNullAsync(_op, right, reportOrNull);
                        if (result != null) return new BoolConstantValue(result.Value, Span);
                    }
                    return null;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
