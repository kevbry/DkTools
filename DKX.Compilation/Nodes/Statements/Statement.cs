using DK.Code;

namespace DKX.Compilation.Nodes.Statements
{
    abstract class Statement : Node
    {
        private CodeSpan _span;

        public abstract string ToCode(int parentOffset);

        public Statement(Node parent, CodeSpan span)
            : base(parent)
        {
            _span = span;
            if (Parent is Statement parentStmt) parentStmt.Span = parentStmt.Span.Envelope(_span);
        }

        public CodeSpan Span
        {
            get => _span;
            protected set
            {
                _span = value;
                if (Parent is Statement parentStmt) parentStmt.Span = parentStmt.Span.Envelope(_span);
            }
        }
    }
}
