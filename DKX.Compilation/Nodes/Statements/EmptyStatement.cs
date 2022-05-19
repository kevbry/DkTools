using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;

namespace DKX.Compilation.Nodes.Statements
{
    class EmptyStatement : Statement
    {
        public EmptyStatement(Node parent, CodeSpan span)
            : base(parent, span)
        { }

        public override bool IsEmptyCode => true;

        public override void ToCode(OpCodeGenerator code, int parentOffset) { }
    }
}
