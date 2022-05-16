using DK.Code;
using DKX.Compilation.CodeGeneration.Constants;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Expressions
{
    class OperatorChain : Chain
    {
        private Operator _op;
        private Chain _left;
        private Chain _right;

        public OperatorChain(Operator op, Chain left, Chain right, CodeSpan opSpan)
            : base(right != null ? left.Span.Envelope(right.Span) : left.Span.Envelope(opSpan))
        {
            _op = op;
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right;

            if (_right == null && !_op.IsUnaryPost()) throw new ArgumentNullException(nameof(right));
        }

        public override OpCodeFragment Execute(OpCodeGeneratorContext context)
        {
            OpCodeFragment leftFrag;

            switch (_op)
            {
                case Operator.Increment:
                    leftFrag = _left.ReadProvideVariable(context);
                    return leftFrag.Append(OpCodeFragment.Increment(Span, leftFrag.DataType, leftFrag.VarName));
                default:
                    throw new NotImplementedException();
            }
        }

        public override OpCodeFragment ReadProvideVariable(OpCodeGeneratorContext context)
        {
            OpCodeFragment leftFrag;

            switch (_op)
            {
                case Operator.Increment:
                    leftFrag = _left.ReadProvideVariable(context);
                    return leftFrag.Append(OpCodeFragment.Increment(Span, leftFrag.DataType, leftFrag.VarName));
                default:
                    throw new NotImplementedException();
            }
        }

        public override OpCodeFragment ReadToVariable(OpCodeGeneratorContext context, string varName, DataType? varDataType)
        {
            OpCodeFragment leftFrag;

            switch (_op)
            {
                case Operator.Increment:
                    leftFrag = _left.ReadProvideVariable(context);
                    leftFrag.Append(OpCodeFragment.Increment(Span, leftFrag.DataType, leftFrag.VarName));
                    if (varName != leftFrag.VarName) leftFrag.Append(OpCodeFragment.SetVarToVar(Span, varDataType, varName, leftFrag.VarName));
                    return leftFrag;
                default:
                    throw new NotImplementedException();
            }
        }

        public override ConstantValue ReadConstant(DataType constDataType)
        {
            switch (_op)
            {
                case Operator.Increment:
                    throw new OpCodeCannotBeConstantException();
                default:
                    throw new NotImplementedException();
            }
        }

        public override void Report(ISourceCodeReporter reporter)
        {
            _left.Report(reporter);
            _right?.Report(reporter);
        }
    }
}
