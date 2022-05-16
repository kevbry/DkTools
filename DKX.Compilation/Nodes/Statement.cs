using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;

namespace DKX.Compilation.Nodes
{
    abstract class Statement : Node
    {
        public abstract OpCodeFragment Execute(OpCodeGeneratorContext context);

        private CodeSpan _span;

        public Statement(Node parent, CodeSpan span)
            : base(parent)
        {
            _span = span;
        }

        public CodeSpan Span { get => _span; protected set => _span = value; }
    }
}
