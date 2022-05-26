using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    public abstract class Chain
    {
        public abstract DataType DataType { get; }
        public abstract DataType InferredDataType { get; }
        public abstract Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report);
        public abstract Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report);
        public abstract bool IsEmptyCode { get; }
        public abstract Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull);

        public static readonly Chain[] EmptyArray = new Chain[0];

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;
    }
}
