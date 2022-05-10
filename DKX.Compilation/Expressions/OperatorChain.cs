using System;

namespace DKX.Compilation.Expressions
{
    class OperatorChain : Chain
    {
        private Operator _op;
        private Chain _left;
        private Chain _right;

        public OperatorChain(Operator op, Chain left, Chain right)
            : base(left.Span.Envelope(right.Span))
        {
            _op = op;
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public override string ToCode() => $"#{_op.GetOpCode()}({_left.ToCode()},{_right.ToCode()})";

        public override void Report(IReporter reporter)
        {
            _left.Report(reporter);
            _right.Report(reporter);
        }
    }
}
