using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class DataTypeChain : Chain
    {
        private DataType _dataType;

        public DataTypeChain(DataType dataType, CodeSpan span)
            : base(span)
        {
            _dataType = dataType;
        }

        public override DataType DataType => _dataType;
        public override DataType InferredDataType => _dataType;
        public override bool IsEmptyCode => false;

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            return Task.FromResult(new CodeFragment("0", _dataType, OpPrec.None, Span, readOnly: true));
        }

        public override async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            await report.ReportAsync(valueFragment.SourceSpan, ErrorCode.StaticReferenceCannotBeModified);
            return new CodeFragment("0", _dataType, OpPrec.None, Span, readOnly: false);
        }
    }
}
