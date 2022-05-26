using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstantValues;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class ConstructorChain : Chain
    {
        private DataType _dataType;
        private List<Chain> _argExpressions;

        private ConstructorChain(DataType dataType, CodeSpan span)
            : base(span)
        {
            _dataType = dataType;
        }

        public static async Task<ConstructorChain> ParseAsync(
            Scope scope,
            DataType dataType,
            CodeSpan newKeywordSpan,
            CodeSpan dataTypeSpan,
            DkxTokenCollection argumentTokens,
            IResolver resolver)
        {
            if (!dataType.IsSuitableForNew) await scope.ReportAsync(dataTypeSpan, ErrorCode.DataTypeCannotBeInstantiated);

            var span = newKeywordSpan.Envelope(dataTypeSpan);
            if (argumentTokens != null && argumentTokens.Any()) span = span.Envelope(argumentTokens.Span);
            var ctor = new ConstructorChain(dataType, span);

            var argExpressions = new List<Chain>();
            if (argumentTokens.Count != 0)
            {
                foreach (var argTokens in argumentTokens.SplitByType(DkxTokenType.Delimiter))
                {
                    var argStream = argTokens.ToStream();
                    var expression = await ExpressionParser.TryReadExpressionAsync(scope, argStream, resolver);
                    if (expression == null)
                    {
                        await scope.ReportAsync(dataTypeSpan, ErrorCode.ConstructorContainsEmptyArguments);
                    }
                    else if (!argStream.EndOfStream)
                    {
                        await scope.ReportAsync(argStream.Read().Span, ErrorCode.SyntaxError);
                    }
                    else
                    {
                        argExpressions.Add(expression);
                    }
                }
            }
            ctor._argExpressions = argExpressions;

            return ctor;
        }

        public override DataType DataType => _dataType;
        public override DataType InferredDataType => _dataType;
        public override bool IsEmptyCode => false;

        public override Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            return Task.FromResult(new CodeFragment(
                text: $"{DkxConst.DkxLib.dkx_new}({_dataType.Class.DataSize})",
                dataType: _dataType,
                precedence: OpPrec.None,
                sourceSpan: Span,
                readOnly: true));
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);
    }
}
