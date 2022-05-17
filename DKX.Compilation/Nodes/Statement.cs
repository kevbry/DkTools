using DK.Code;

namespace DKX.Compilation.Nodes
{
    abstract class Statement : Node
    {
        private CodeSpan _span;

        public abstract string ToCode(int offset);

        public Statement(Node parent, CodeSpan span)
            : base(parent)
        {
            _span = span;
        }

        public CodeSpan Span { get => _span; protected set => _span = value; }
    }
}
