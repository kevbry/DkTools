using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class FieldChain : Chain
    {
        private Chain _leftChain;
        private string _memberName;
        private IField _field;

        public FieldChain(Chain leftChain, DkxToken nameToken, IField field)
            : base(nameToken.Span)
        {
            _leftChain = leftChain ?? throw new ArgumentNullException(nameof(leftChain));
            _memberName = nameToken.Text;
            _field = field;
        }

        public override DataType DataType => _field.DataType;
        public override DataType InferredDataType => _field.DataType;
        public override bool IsEmptyCode => false;

        public override async Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            var leftFrag = await _leftChain.ToWbdkCode_ReadAsync(report);
            return await _field.ToWbdkCode_ReadAsync(leftFrag, Span, report);
        }

        public override async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            var leftFrag = await _leftChain.ToWbdkCode_ReadAsync(report);
            return await _field.ToWbdkCode_WriteAsync(leftFrag, Span, valueFragment, report);
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);
    }
}
