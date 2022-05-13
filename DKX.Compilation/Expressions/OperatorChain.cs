using DKX.Compilation.CodeGeneration.OpCodes;
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

        public override string ToCode(int parentOffset)
        {
            if (_right != null)
            {
                return string.Concat(
                    OpCodeGenerator.GenerateOpCode(_op.GetOpCode(), parentOffset, Span),
                    "(",
                    _left.ToCode(Span.Start),
                    ",",
                    _right.ToCode(Span.Start),
                    ")");
            }
            else
            {
                return string.Concat(
                    OpCodeGenerator.GenerateOpCode(_op.GetOpCode(), parentOffset, Span),
                    "(",
                    _left.ToCode(Span.Start),
                    ")");
            }
        }

        public override void Report(ISourceCodeReporter reporter)
        {
            _left.Report(reporter);
            _right?.Report(reporter);
        }
    }
}
