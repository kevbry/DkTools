using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables.ConstantValues
{
    public abstract class ConstantValue
    {
        public abstract DataType DataType { get; }
        public abstract bool IsBool { get; }
        public abstract bool Bool { get; }
        public abstract bool IsNumber { get; }
        public abstract decimal Number { get; }
        public abstract bool IsString { get; }
        public abstract string String { get; }
        public abstract bool IsChar { get; }
        public abstract char Char { get; }
        public abstract bool IsNull { get; }
        public abstract Task<bool?> GetComparisonResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull);
        public abstract Task<ConstantValue> GetMathResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull);

        public abstract CodeFragment ToWbdkCode();

        private CodeSpan _span;

        public ConstantValue(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;
    }
}
