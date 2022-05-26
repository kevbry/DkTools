using DK.Code;
using DKX.Compilation.CodeGeneration;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes.Statements
{
    class EmptyStatement : Statement
    {
        public EmptyStatement(Scope parentScope, CodeSpan span)
            : base(parentScope, span)
        { }

        internal override Task GenerateWbdkCodeAsync(CodeWriter cw) => Task.CompletedTask;

        public override bool IsEmpty => true;
    }
}
