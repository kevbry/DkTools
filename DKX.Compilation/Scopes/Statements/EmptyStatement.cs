using DK.Code;
using DKX.Compilation.CodeGeneration;

namespace DKX.Compilation.Scopes.Statements
{
    class EmptyStatement : Statement
    {
        public EmptyStatement(Scope parentScope, CodeSpan span)
            : base(parentScope, span)
        { }

        internal override void GenerateWbdkCode(CodeWriter cw) { }

        public override bool IsEmpty => true;
    }
}
