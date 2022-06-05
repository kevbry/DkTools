using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Objects;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Validation;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class VariableChain : Chain
    {
        private Variable _variable;

        public VariableChain(Variable variable, Span span)
            : base(span)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
            if (!variable.Local) throw new InvalidOperationException("VariableChain can only be used for local variables and arguments.");
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => _variable.DataType;

        public override DataType InferredDataType => _variable.DataType;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            if (!flow.IsVariableInitialized(_variable.WbdkName)) context.Report.Report(Span, ErrorCode.UseOfUninitializedVariable, _variable.Name);
            return new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            ConversionValidator.CheckConversion(_variable.DataType, valueFragment, context.Report);
            flow.OnVariableAssigned(_variable.WbdkName);

            if (_variable.DataType.IsClass) valueFragment = ObjectAccess.GenerateSwapReference(
                oldFragment: ToWbdkCode_Read(context, flow),
                newFragment: valueFragment);

            return new CodeFragment($"{_variable.WbdkName} = {valueFragment.Protect(OpPrec.Assign)}", _variable.DataType, OpPrec.Assign, Span);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
