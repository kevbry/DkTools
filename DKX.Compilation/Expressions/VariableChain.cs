using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Expressions
{
    class VariableChain : Chain
    {
        private Variable _variable;

        public VariableChain(Variable variable, CodeSpan span)
            : base(span)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => _variable.DataType;

        public override DataType InferredDataType => _variable.DataType;

        public override CodeFragment ToWbdkCode_Read(ISourceCodeReporter report)
        {
            return new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span, readOnly: false);
        }

        public override CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            return new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span, readOnly: false);
        }
    }
}
