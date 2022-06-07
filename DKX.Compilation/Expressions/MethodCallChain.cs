using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Objects;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.SystemClasses;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Linq;
using System.Text;

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
            if (method.Flags.HasFlag(ModifierFlags.Static))
            {
                if (thisChain != null) throw new ArgumentException($"{nameof(thisChain)} must be null for static methods.");
            }
            else
            {
                if (thisChain == null) throw new ArgumentNullException(nameof(thisChain));
            }

            _thisChain = thisChain;
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public override DataType DataType => _method.ReturnDataType;
        public override DataType InferredDataType => _method.ReturnDataType;
        public override bool IsEmptyCode => false;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            context.DependsOnFile(_method.Class.DkxPathName);

            if (_method.Flags.HasFlag(ModifierFlags.NotCallable)) throw new CodeException(Span, ErrorCode.MethodNotCallable);

            if (context.IsOutsideClass(_method.Class) && _method.Privacy != Privacy.Public)
            {
                throw new CodeException(Span, ErrorCode.CannotAccessMemberDueToPrivacy, _method.Name, _method.Privacy.ToString().ToLower());
            }

            if (_method.AccessType == MethodAccessType.System)
            {
                var cls = SystemClass.SystemClasses.Where(x => x.FullClassName == _method.Class.FullClassName).FirstOrDefault();
                if (cls == null) throw new InvalidOperationException($"System class '{_method.Class.FullClassName}' not found.");

                SystemMethod sysMethod;
                if (_thisChain == null)
                {
                    sysMethod = cls.GetStaticMethods(_method.Name).Where(m => m.ReturnDataType == _method.ReturnDataType && m.Arguments.IsMatch(_method.Arguments)).FirstOrDefault();
                }
                else
                {
                    sysMethod = cls.GetNonStaticMethods(_method.Name, _thisChain.DataType).Where(m => m.ReturnDataType == _method.ReturnDataType && m.Arguments.IsMatch(_method.Arguments)).FirstOrDefault();
                }
                if (sysMethod == null) throw new InvalidOperationException($"System method '{_method.Class.FullClassName}.{_method.Name}' not found.");

                return sysMethod.WbdkCodeGenerator(context, _thisChain, _args, Span, flow);
            }
            else
            {
                var sb = new StringBuilder();

                sb.Append(_method.Class.WbdkClassName);
                sb.Append('.');
                sb.Append(_method.WbdkName);
                sb.Append('(');
                var firstArg = true;
                if (!_method.Flags.HasFlag(ModifierFlags.Static))
                {
                    var thisFrag = _thisChain.ToWbdkCode_Read(context, flow);
                    if (thisFrag.IsUnownedObjectReference) thisFrag = ObjectAccess.GenerateReleaseDefer(context, thisFrag);
                    sb.Append(thisFrag);
                    firstArg = false;
                }
                foreach (var arg in _args)
                {
                    if (firstArg) firstArg = false;
                    else sb.Append(", ");
                    var argFrag = arg.ToWbdkCode_Read(context, flow);
                    if (argFrag.DataType.IsClass && !argFrag.IsUnownedObjectReference) argFrag = ObjectAccess.GenerateAddReference(argFrag);
                    sb.Append(argFrag);
                }
                sb.Append(')');

                return new CodeFragment(sb.ToString(), _method.ReturnDataType, OpPrec.None, Span, reportable: !_method.ReturnDataType.IsVoid,
                    flags: _method.ReturnDataType.IsClass ? CodeFragmentFlags.UnownedObjectReference : default);
            }
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
