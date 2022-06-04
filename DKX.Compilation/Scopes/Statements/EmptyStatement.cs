using DKX.Compilation.CodeGeneration;

namespace DKX.Compilation.Scopes.Statements
{
    class EmptyStatement : Statement
    {
        public EmptyStatement(Scope parentScope, Span span) : base(parentScope, span) { }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow) { }

        public override bool IsEmpty => true;
    }
}
