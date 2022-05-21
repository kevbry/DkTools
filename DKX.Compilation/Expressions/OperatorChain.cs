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

        public OperatorChain(Operator op, Chain left, Chain right)
            : base(right != null ? left.Span.Envelope(right.Span) : left.Span)
        {
            _op = op;
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right;

            if (_right == null && !_op.IsUnaryPost()) throw new ArgumentNullException(nameof(right));
        }

        public override void ToCode(OpCodeGenerator code, int parentOffset)
        {
            code.WriteOpCode(_op.GetOpCode(), parentOffset, Span);
            code.WriteOpen();
            _left.ToCode(code, Span.Start);
            if (_right != null)
            {
                code.WriteDelim();
                _right.ToCode(code, Span.Start);
            }
            code.WriteClose();
        }

        public override bool IsEmptyCode => false;

        public override void Report(ISourceCodeReporter reporter)
        {
            _left.Report(reporter);
            _right?.Report(reporter);
        }

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
    }
}
