using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Scopes;
using System.Collections.Generic;

namespace DKX.Compilation.SystemClasses
{
    class SystemTypesClass : SystemClass
    {
        private static SystemTypesClass _instance;

        public SystemTypesClass()
            : base("Types")
        {
            _instance = this;
        }

        public static IEnumerable<SystemMethod> GetMethodsForDataType(DataType dataType, string name)
        {
            switch (name)
            {
                case DkxConst.SystemMethods.ToString_:
                    return GetToStringMethodsForDataType(dataType);
                default:
                    return SystemMethod.EmptyArray;
            }
        }

        public static IEnumerable<SystemMethod> GetToStringMethodsForDataType(DataType dataType)
        {
            switch (dataType.BaseType)
            {
                case BaseType.Short:
                case BaseType.UShort:
                case BaseType.Int:
                case BaseType.UInt:
                case BaseType.Numeric:
                case BaseType.UNumeric:
                case BaseType.Char:
                case BaseType.String:
                case BaseType.Date:
                case BaseType.Time:
                case BaseType.Enum:
                case BaseType.Bool:
                case BaseType.Class:
                    return new SystemMethod[]
                    {
                        new SystemMethod(_instance, DkxConst.SystemMethods.ToString_, dataType, SystemArgument.EmptyArray, onDataType: dataType, MakestringGenerator)
                    };

                default:
                    return SystemMethod.EmptyArray;
            }
        }

        private static CodeFragment MakestringGenerator(CodeGenerationContext context, Chain leftChain, Chain[] arguments, Span span, FlowTrace flow)
        {
            if (arguments.Length != 0) throw new CodeException(span, ErrorCode.InvalidNumberOfArguments);

            var frag = leftChain.ToWbdkCode_Read(context, flow);

            switch (frag.DataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"makestring({frag} ? \"true\" : \"false\")", DataType.String255, OpPrec.None, span, reportable: true);
                case BaseType.Class:
                    return new CodeFragment(frag.DataType.ClassName, DataType.String255, OpPrec.None, span, reportable: true);
                default:
                    return new CodeFragment($"makestring({frag})", DataType.String255, OpPrec.None, span, reportable: true);
            }
        }

        public override IEnumerable<SystemMethod> GetNonStaticMethods(string name, DataType dataType)
        {
            return GetMethodsForDataType(dataType, name);
        }
    }
}
