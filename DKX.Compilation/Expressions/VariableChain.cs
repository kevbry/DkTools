using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class VariableChain : Chain
    {
        private Variable _variable;
        private Chain _thisExpressionOrNull;

        public VariableChain(Variable variable, Span span, Chain thisExpressionOrNull)
            : base(span)
        {
            switch (variable.AccessMethod)
            {
                case FieldAccessMethod.Object:
                case FieldAccessMethod.Constant:
                case FieldAccessMethod.Property:
                    throw new InvalidOperationException("A VariableChain cannot be used for properties, constants, or non-stack member variables.");
                    // Use FieldChain for these types.
            }

            _variable = variable ?? throw new ArgumentNullException(nameof(variable));

            if (!_variable.Local && !_variable.Flags.HasFlag(Scopes.ModifierFlags.Static))
            {
                if (thisExpressionOrNull == null) throw new ArgumentNullException(nameof(thisExpressionOrNull));
            }

            _thisExpressionOrNull = thisExpressionOrNull;
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => _variable.DataType;

        public override DataType InferredDataType => _variable.DataType;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            context.DependsOnFile(_variable.DefinitionSpan.PathName);

            if (_variable.Flags.HasFlag(Scopes.ModifierFlags.Static) || _variable.Local)
            {
                return new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span, readOnly: false);
            }
            else
            {
                if (_thisExpressionOrNull == null) throw new InvalidOperationException("No object reference expression exists for a non-static variable.");

                return Objects.ObjectAccess.GenerateMemberVariableGetter(
                    thisFragment: _thisExpressionOrNull.ToWbdkCode_Read(context),
                    varOffset: _variable.Offset,
                    varDataType: _variable.DataType,
                    span: Span);
            }
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            Conversions.ConversionValidator.CheckConversion(_variable.DataType, valueFragment, context.Report);

            if (_variable.Flags.HasFlag(Scopes.ModifierFlags.Static) || _variable.Local)
            {
                return new CodeFragment($"{_variable.WbdkName} = {valueFragment}", _variable.DataType, OpPrec.None, Span, readOnly: false);
            }
            else
            {
                if (_thisExpressionOrNull == null) throw new InvalidOperationException("No object reference expression exists for a non-static variable.");

                return Objects.ObjectAccess.GenerateMemberVariableSetter(
                    thisFragment: _thisExpressionOrNull.ToWbdkCode_Read(context),
                    varOffset: _variable.Offset,
                    varDataType: _variable.DataType,
                    span: Span,
                    valueFragment: valueFragment);
            }
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
