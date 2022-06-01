using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstTerms;
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

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            CodeFragment leftFrag, rightFrag;
            OpPrec prec;

            switch (_op)
            {
                case Operator.Increment:
                case Operator.Decrement:
                    leftFrag = _left.ToWbdkCode_Read(context);
                    if (leftFrag.ReadOnly) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, _op.GetText());
                    if (!leftFrag.DataType.IsSuitableForIncDec) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    return new CodeFragment($"{leftFrag.Protect(OpPrec.IncDec)} {_op.GetText()} 1", leftFrag.DataType, OpPrec.IncDec, Span, readOnly: false);

                case Operator.Add:
                case Operator.Subtract:
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.Modulus:
                    // Try to optimize away constants first
                    var leftConst = _left.ToConstTermOrNull(reportOrNull: null);
                    if (leftConst != null)
                    {
                        var rightConst = _right.ToConstTermOrNull(reportOrNull: null);
                        if (rightConst != null)
                        {
                            var term = new ConstMathTerm(_op, leftConst, rightConst, Span);
                            var report = new ReportItemCollector();
                            var constContext = new ConstResolutionContext(report, context.Project);
                            var constant = term.ResolveConstantOrNull(constContext, DkxConst.EmptyStringArray);
                            if (constant != null) return constant.ToWbdkCode();
                        }
                    }

                    leftFrag = _left.ToWbdkCode_Read(context);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    rightFrag = _right.ToWbdkCode_Read(context);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(rightFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    ConversionValidator.CheckConversion(leftFrag.DataType, rightFrag, context.Report);
                    prec = _op.GetPrecedence();
                    return new CodeFragment($"{leftFrag.Protect(prec)} {_op.GetText()} {rightFrag.Protect(prec)}", leftFrag.DataType, prec, Span, readOnly: true);

                case Operator.Assign:
                    rightFrag = _right.ToWbdkCode_Read(context);
                    leftFrag = _left.ToWbdkCode_Write(context, rightFrag);
                    return leftFrag.Protect(OpPrec.Assign);

                case Operator.AssignAdd:
                case Operator.AssignSubtract:
                case Operator.AssignMultiply:
                case Operator.AssignDivide:
                case Operator.AssignModulus:
                    leftFrag = _left.ToWbdkCode_Read(context);
                    if (leftFrag.ReadOnly) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorExpectsWriteableValueOnLeft, _op.GetText());
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(leftFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    rightFrag = _right.ToWbdkCode_Read(context);
                    if (!leftFrag.DataType.IsSuitableForNumericMath) throw new CodeException(rightFrag.SourceSpan, ErrorCode.OperatorCannotBeUsedWithThisDataType, _op.GetText());
                    ConversionValidator.CheckConversion(leftFrag.DataType, rightFrag, context.Report);
                    prec = _op.GetPrecedence();
                    return new CodeFragment($"{leftFrag.Protect(prec)} {_op.GetText()} {rightFrag.Protect(prec)}", leftFrag.DataType, prec, Span, readOnly: true);

                default:
                    throw new NotImplementedException();
            }
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            // Right-to-left operator associativity should take care of cascaded writes to operators.
            // For example { a = b = c; } should result in { b = c; } then { a = b; }
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo, _op.GetText());
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector reportOrNull)
        {
            ConstTerm left, right;
            switch (_op)
            {
                case Operator.Add:
                case Operator.Subtract:
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.Modulus:
                    left = _left.ToConstTermOrNull(reportOrNull);
                    right = _right.ToConstTermOrNull(reportOrNull);
                    if (left != null && right != null) return new ConstMathTerm(_op, left, right, Span);
                    return null;

                case Operator.And:
                case Operator.Or:
                    left = _left.ToConstTermOrNull(reportOrNull);
                    right = _right.ToConstTermOrNull(reportOrNull);
                    if (left != null && left.DataType.BaseType != BaseType.Bool) reportOrNull?.Report(_left.Span, ErrorCode.ExpressionMustBeBool);
                    if (right != null && right.DataType.BaseType != BaseType.Bool) reportOrNull?.Report(_right.Span, ErrorCode.ExpressionMustBeBool);
                    if (left != null && right != null) return new ConstLogicalTerm(_op, left, right, Span);
                    return null;

                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.LessThan:
                case Operator.LessEqual:
                case Operator.GreaterThan:
                case Operator.GreaterEqual:
                    left = _left.ToConstTermOrNull(reportOrNull);
                    right = _right.ToConstTermOrNull(reportOrNull);
                    if (left != null && right != null) return new ConstComparisonTerm(_op, left, right, Span);
                    return null;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
