using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Expressions
{
    class ConstructorChain : Chain
    {
        private DataType _dataType;
        private List<Chain> _argExpressions;
        private IClass _class;

        private ConstructorChain(DataType dataType, Span span, IClass class_)
            : base(span)
        {
            _dataType = dataType;
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
        }

        public static Chain Parse(
            Scope scope,
            DataType dataType,
            Span newKeywordSpan,
            Span dataTypeSpan,
            DkxTokenCollection argumentTokens)
        {
            if (dataType.BaseType != BaseType.Class) throw new CodeException(dataTypeSpan, ErrorCode.DataTypeCannotBeInstantiated, dataType.ToString());

            var class_ = scope.Resolver.GetClassByFullNameOrNull(dataType.ClassName);
            if (class_ == null)
            {
                scope.Report(dataTypeSpan, ErrorCode.ClassNotFound, dataType.ClassName);
                return new ErrorChain(innerChainOrNull: null, dataTypeSpan);
            }

            if (class_.Flags.IsStatic()) scope.Report(dataTypeSpan, ErrorCode.StaticClassCannotBeInstantiated, class_.FullClassName);

            var span = newKeywordSpan.Envelope(dataTypeSpan);
            if (argumentTokens != null && argumentTokens.Any()) span = span.Envelope(argumentTokens.Span);
            var ctor = new ConstructorChain(dataType, span, class_);

            var argExpressions = new List<Chain>();
            if (argumentTokens.Count != 0)
            {
                foreach (var argTokens in argumentTokens.SplitByType(DkxTokenType.Delimiter))
                {
                    var argStream = argTokens.ToStream();
                    var expression = ExpressionParser.ReadExpressionOrNull(scope, argStream);
                    if (expression == null)
                    {
                        scope.Report(dataTypeSpan, ErrorCode.ConstructorContainsEmptyArguments);
                    }
                    else if (!argStream.EndOfStream)
                    {
                        scope.Report(argStream.Read().Span, ErrorCode.SyntaxError);
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

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            context.DependsOnFile(_class.DkxPathName);

            return new CodeFragment(
                text: $"{DkxConst.DkxLib.dkx_new}({_class.DataSize})",
                dataType: _dataType,
                precedence: OpPrec.None,
                span: Span);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
