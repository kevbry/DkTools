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

        public override string ToCode()
        {
            if (_right != null) return $"{_op.GetOpCode()}({_left.ToCode()},{_right.ToCode()})";
            return $"{_op.GetOpCode()}({_left.ToCode()})";
        }

        public override void Report(IReporter reporter)
        {
            _left.Report(reporter);
            _right?.Report(reporter);
        }
    }
}
