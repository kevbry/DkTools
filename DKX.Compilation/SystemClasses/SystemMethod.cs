using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.SystemClasses
{
    class SystemMethod : IMethod
    {
        private IClass _class;
        private string _name;
        private DataType _returnDataType;
        private SystemArgument[] _arguments;
        private WbdkCodeGeneratorCallback _wbdkCodeGenerator;

        public delegate CodeFragment WbdkCodeGeneratorCallback(CodeGenerationContext context, Chain[] arguments, Span span, FlowTrace flow);

        public SystemMethod(IClass class_, string name, DataType returnDataType, SystemArgument[] arguments, WbdkCodeGeneratorCallback wbdkCodeGenerator)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _returnDataType = returnDataType;
            _arguments = arguments;
            _wbdkCodeGenerator = wbdkCodeGenerator ?? throw new ArgumentNullException(nameof(wbdkCodeGenerator));
        }

        public MethodAccessType AccessType => MethodAccessType.System;
        public IArgument[] Arguments => _arguments ?? SystemArgument.EmptyArray;
        public IClass Class => _class;
        public Span DefinitionSpan => Span.Empty;
        public FileContext FileContext => FileContext.NeutralClass;
        public ModifierFlags Flags => ModifierFlags.Static;
        public string Name => _name;
        public Privacy Privacy => Privacy.Public;
        public DataType ReturnDataType => _returnDataType;
        public WbdkCodeGeneratorCallback WbdkCodeGenerator => _wbdkCodeGenerator;
        public string WbdkName => _name;
    }

    class SystemArgument : IArgument
    {
        public static readonly SystemArgument[] EmptyArray = new SystemArgument[0];

        private string _name;
        private DataType _dataType;
        private ArgumentPassType _passType;

        public SystemArgument(string name, DataType dataType, ArgumentPassType passType)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _dataType = dataType;
            _passType = passType;
        }

        public string Name => _name;

        public DataType DataType => _dataType;

        public ArgumentPassType PassType => _passType;
    }
}
