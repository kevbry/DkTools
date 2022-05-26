using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class ThisChain : Chain
    {
        private DataType _dataType;

        public ThisChain(DataType dataType, CodeSpan span)
            : base(span)
        {
            if (dataType.BaseType != BaseType.Class) throw new ArgumentException("Data type must be a class.");
            _dataType = dataType;
        }

        public override DataType DataType => _dataType;
        public override DataType InferredDataType => _dataType;
        public override bool IsEmptyCode => false;

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            return Task.FromResult(new CodeFragment(DkxConst.This, _dataType, OpPrec.None, Span, readOnly: true));
        }

        public override async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            await report.ReportAsync(valueFragment.SourceSpan, ErrorCode.ThisCannotBeModified);
            return new CodeFragment(DkxConst.This, _dataType, OpPrec.None, Span, readOnly: true);
        }
    }
}
