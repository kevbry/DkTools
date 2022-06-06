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
            return new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span, reportable: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            ConversionValidator.CheckConversion(_variable.DataType, valueFragment, context.Report);

            if (_variable.DataType.IsClass)
            {
                if (valueFragment.IsUnownedObjectReference)
                {
                    // We need to assign the value to the variable without incrementing the ref count of the new value.
                    if (flow.IsVariableInitialized(_variable.WbdkName))
                    {
                        // This variable is already assigned, so we need to decrement the ref count of the old value.
                        valueFragment = ObjectAccess.GenerateSwapNoAddReference(oldFragment: ToWbdkCode_Read(context, flow), newFragment: valueFragment);
                    }
                    else
                    {
                        // Variable is new, so no need to decrement ref count of old value.
                        // No change to valueFragment
                    }
                }
                else
                {
                    // We need to increment the ref count of the new value.
                    if (flow.IsVariableInitialized(_variable.WbdkName))
                    {
                        // This variable is already assigned, so we need to decrement the ref count of the old value.
                        var oldFragment = new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span, reportable: true);
                        valueFragment = ObjectAccess.GenerateSwapReference(oldFragment, valueFragment);
                    }
                    else
                    {
                        // Variable is new, so no need to decrement ref count of old value.
                        valueFragment = ObjectAccess.GenerateAddReference(valueFragment);
                    }
                }
            }

            flow.OnVariableAssigned(_variable.WbdkName);
            return new CodeFragment($"{_variable.WbdkName} = {valueFragment.Protect(OpPrec.Assign)}", _variable.DataType, OpPrec.Assign, Span, reportable: false);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
