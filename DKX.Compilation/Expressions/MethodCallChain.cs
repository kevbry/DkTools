using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class MethodCallChain : Chain
    {
        private Chain _leftChain;
        private Chain[] _args;
        private IMethod _method;

        public MethodCallChain(Chain leftChain, DkxToken nameToken, Chain[] args, CodeSpan argsSpan, IMethod method)
            : base(nameToken.Span.Envelope(argsSpan))
        {
            _leftChain = leftChain ?? throw new ArgumentNullException(nameof(leftChain));
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public override DataType DataType => _method.ReturnDataType;
        public override DataType InferredDataType => _method.ReturnDataType;
        public override bool IsEmptyCode => false;

        public override async Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            var leftFrag = await _leftChain.ToWbdkCode_ReadAsync(report);

            var args = new List<CodeFragment>();
            foreach (var arg in _args) args.Add(await arg.ToWbdkCode_ReadAsync(report));

            return await _method.ToWbdkCode_MethodCallAsync(leftFrag, args, Span);
        }

        public override Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);
    }
}
