using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class MethodCallChain : Chain
    {
        private Chain _thisChain;
        private Chain[] _args;
        private IMethod _method;

        public MethodCallChain(Chain thisChain, DkxToken nameToken, Chain[] args, Span argsSpan, IMethod method)
            : base(nameToken.Span.Envelope(argsSpan))
        {
            _thisChain = thisChain ?? throw new ArgumentNullException(nameof(thisChain));
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public override DataType DataType => _method.ReturnDataType;
        public override DataType InferredDataType => _method.ReturnDataType;
        public override bool IsEmptyCode => false;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            var sb = new StringBuilder();
            sb.Append(_method.Class.WbdkClassName);
            sb.Append('.');
            sb.Append(_method.WbdkName);
            sb.Append('(');
            var firstArg = true;
            if (!_method.Static)
            {
                sb.Append(_thisChain.ToWbdkCode_Read(context));
                firstArg = false;
            }
            foreach (var arg in _args)
            {
                if (firstArg) firstArg = false;
                else sb.Append(", ");
                sb.Append(arg.ToWbdkCode_Read(context));
            }
            sb.Append(')');

            return new CodeFragment(sb.ToString(), _method.ReturnDataType, OpPrec.None, Span, readOnly: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
