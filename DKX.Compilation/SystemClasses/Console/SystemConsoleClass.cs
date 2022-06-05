using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Scopes;
using DKX.Compilation.Validation;
using DKX.Compilation.Variables;

namespace DKX.Compilation.SystemClasses.Console
{
    class SystemConsoleClass : SystemClass
    {
        public SystemConsoleClass()
            : base("Console")
        {
            AddMethod(new SystemMethod(this, "WriteLine", DataType.Void, new SystemArgument[]
            {
                new SystemArgument("text", DataType.String255, ArgumentPassType.ByReference)
            }, Generate_WriteLine));
        }

        private CodeFragment Generate_WriteLine(CodeGenerationContext context, Chain[] arguments, Span span, FlowTrace flow)
        {
            if (arguments.Length != 1) throw new CodeException(span, ErrorCode.InvalidNumberOfArguments);
            var argFrag = arguments[0].ToWbdkCode_Read(context, flow);
            ConversionValidator.CheckConversion(DataType.String255, argFrag, context.Report);
            return new CodeFragment($"puts({argFrag})", DataType.Void, OpPrec.None, span);
        }
    }
}
