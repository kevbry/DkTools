using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Nodes.Statements
{
    class VariableInitializationStatement : Statement
    {
        private Variable _variable;
        private Chain _exp;

        public VariableInitializationStatement(Node parent, Variable variable, Chain exp, CodeSpan span)
            : base(parent, span)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
            _exp = exp ?? throw new ArgumentNullException(nameof(exp));
        }

        public override void ToCode(OpCodeGenerator code, int parentOffset)
        {
            if (_exp.IsEmptyCode) return;

            code.WriteOpCode(OpCode.Assign, parentOffset, Span);
            code.WriteOpen();
            code.WriteVariable(_variable.WbdkName, Span.Start, Span);
            code.WriteDelim();
            _exp.ToCode(code, Span.Start);
            code.WriteClose();
        }

        public override bool IsEmptyCode => _exp.IsEmptyCode;
    }
}
