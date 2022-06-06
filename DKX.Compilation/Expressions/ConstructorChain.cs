using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Objects;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Expressions
{
    class ConstructorChain : Chain
    {
        private DataType _dataType;
        private IClass _class;
        private IMethod _method;
        private Chain[] _args;

        private ConstructorChain(DataType dataType, Span span, IClass class_, IMethod methodOrNull, Chain[] args)
            : base(span)
        {
            _dataType = dataType;
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
            _method = methodOrNull;
            _args = args;
        }

        public static Chain Parse(
            Scope scope,
            DataType dataType,
            Span dataTypeSpan,
            DkxToken argsToken)
        {
            if (dataType.BaseType != BaseType.Class) throw new CodeException(dataTypeSpan, ErrorCode.DataTypeCannotBeInstantiated, dataType.ToString());

            var class_ = scope.Resolver.GetClassByFullNameOrNull(dataType.ClassName);
            if (class_ == null) throw new CodeException(dataTypeSpan, ErrorCode.ClassNotFound, dataType.ClassName);

            if (class_.Flags.IsStatic()) throw new CodeException(dataTypeSpan, ErrorCode.StaticClassCannotBeInstantiated, class_.FullClassName);

            Chain[] args;
            Span ctorSpan;
            if (argsToken.IsBrackets)
            {
                args = ExpressionParser.SplitArgumentExpressions(scope, argsToken.Tokens, dataTypeSpan);
                ctorSpan = dataTypeSpan + argsToken.Span;
            }
            else
            {
                args = Chain.EmptyArray;
                ctorSpan = dataTypeSpan;
            }

            ConstructorChain ctor;
            var ctors = class_.GetMethods(class_.ClassName).ToList();
            if (ctors.Count == 0)
            {
                // Use default constructor
                ctor = new ConstructorChain(dataType, ctorSpan, class_, methodOrNull: null, args);
            }
            else
            {
                var method = ExpressionParser.FindBestMethodForArguments(class_.ClassName, ctors, args, dataTypeSpan, isConstructor: true);
                ctor = new ConstructorChain(dataType, ctorSpan, class_, method, args);
            }

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

            if (_method == null)
            {
                return ObjectAccess.GenerateNewObject(_class, Span);
            }
            else
            {
                if (context.IsOutsideClass(_method.Class) && _method.Privacy != Privacy.Public)
                {
                    throw new CodeException(Span, ErrorCode.CannotAccessConstructorDueToPrivacy, _method.Privacy.ToString().ToLower());
                }

                var sb = new StringBuilder();

                sb.Append(_class.WbdkClassName);
                sb.Append('.');
                sb.Append(_method.WbdkName);
                sb.Append('(');

                var firstArg = true;
                foreach (var arg in _args)
                {
                    if (firstArg) firstArg = false;
                    else sb.Append(", ");
                    sb.Append(arg.ToWbdkCode_Read(context, flow));
                }

                sb.Append(')');

                return new CodeFragment(
                    text: sb.ToString(),
                    dataType: _dataType,
                    precedence: OpPrec.None,
                    span: Span,
                    reportable: true,
                    flags: CodeFragmentFlags.UnownedObjectReference);
            }
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
