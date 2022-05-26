using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class VariableChain : Chain
    {
        private Variable _variable;
        private Chain _thisExpressionOrNull;

        public VariableChain(Variable variable, CodeSpan span, Chain thisExpressionOrNull)
            : base(span)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));

            if (!_variable.Local && !_variable.Static)
            {
                if (thisExpressionOrNull == null) throw new ArgumentNullException(nameof(thisExpressionOrNull));
            }
            _thisExpressionOrNull = thisExpressionOrNull;
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => _variable.DataType;

        public override DataType InferredDataType => _variable.DataType;

        public override async Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            if (_variable.Static || _variable.Local)
            {
                return new CodeFragment(_variable.WbdkName, _variable.DataType, OpPrec.None, Span, readOnly: false);
            }
            else
            {
                if (_thisExpressionOrNull == null) throw new InvalidOperationException("No object reference expression exists for a non-static variable.");

                return Objects.ObjectAccess.GenerateMemberVariableGetter(
                    thisFragment: await _thisExpressionOrNull.ToWbdkCode_ReadAsync(report),
                    varOffset: _variable.Offset,
                    varDataType: _variable.DataType,
                    span: Span);
            }
        }

        public override async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            await Conversions.ConversionValidator.CheckConversionAsync(_variable.DataType, valueFragment, report);

            if (_variable.Static || _variable.Local)
            {
                return new CodeFragment($"{_variable.WbdkName} = {valueFragment}", _variable.DataType, OpPrec.None, Span, readOnly: false);
            }
            else
            {
                if (_thisExpressionOrNull == null) throw new InvalidOperationException("No object reference expression exists for a non-static variable.");

                return Objects.ObjectAccess.GenerateMemberVariableSetter(
                    thisFragment: await _thisExpressionOrNull.ToWbdkCode_ReadAsync(report),
                    varOffset: _variable.Offset,
                    varDataType: _variable.DataType,
                    span: Span,
                    valueFragment: valueFragment);
            }
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);
    }
}
