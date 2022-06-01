using DK;
using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Variables
{
    public class Variable : IField, IArgument
    {
        private IClass _class;
        private string _name;
        private string _wbdkName;
        private DataType _dataType;
        private FileContext _fileContext;
        private ArgumentPassType? _passType;
        private Chain _initializer;
        private FieldAccessMethod _accessMethod;
        private bool _static;
        private bool _local;
        private Privacy _privacy;
        private uint _offset;
        private Span _span;

        public static readonly Variable[] EmptyArray = new Variable[0];

        /// <summary>
        /// Creates a new Variable.
        /// </summary>
        /// <param name="name">Name of the variable.</param>
        /// <param name="wbdkName">For local variables, name of the variable as it will be defined in WBDK. For member variables, this includes the class name prefix.</param>
        /// <param name="dataType">Variable's data type.</param>
        /// <param name="fileContext">For member variables, the context it lives in. Not used for local variables.</param>
        /// <param name="passType">For arguments, the pass type. For local variables this must be null.</param>
        /// <param name="static_">For member variables, determines whether it is static. Static variables are defined as globals in WBDK.</param>
        /// <param name="local">Set to true for arguments and local variables.</param>
        /// <param name="privacy">For member variables, their privacy.</param>
        /// <param name="initializer">For member variables, their initializer. Not used for arguments and local variables.</param>
        internal Variable(
            IClass class_,
            string name,
            string wbdkName,
            DataType dataType,
            FileContext fileContext,
            ArgumentPassType? passType,
            FieldAccessMethod accessMethod,
            bool static_,
            bool local,
            Privacy privacy,
            Chain initializer,
            Span span)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));

            _name = name ?? throw new ArgumentNullException();
            if (!_name.IsWord()) throw new ArgumentException("Variable name must be a single word identifier.");

            _wbdkName = wbdkName ?? throw new ArgumentNullException(nameof(wbdkName));
            _dataType = dataType;
            _fileContext = fileContext;
            _passType = passType;
            _accessMethod = accessMethod;
            _static = static_;
            _local = local;
            _privacy = privacy;
            _initializer = initializer;
            _span = span;
        }

        internal Variable(IField field)
        {
            _class = field.Class;
            _name = field.Name;
            _wbdkName = field.Name;
            _dataType = field.DataType;
            _fileContext = field.FileContext;
            _accessMethod = field.AccessMethod;
            _static = field.Static;
            _local = field.AccessMethod == FieldAccessMethod.Variable;
            _privacy = field.ReadPrivacy;
            _offset = field.Offset;
            _span = field.DefinitionSpan;
        }

        public FieldAccessMethod AccessMethod => _accessMethod;
        public ArgumentPassType? ArgumentType => _passType;
        public IClass Class => _class;
        public ConstTerm ConstantExpression => null;
        public ConstValue ConstantValue => null;
        public DataType DataType => _dataType;
        public Span DefinitionSpan => _span;
        internal Chain Initializer { get => _initializer; set => _initializer = value ?? throw new ArgumentNullException(); }
        public FileContext FileContext => _fileContext;
        public bool IsArgument => _passType != null;
        public bool IsConstant => false;
        public bool Local => _local;
        public string Name => _name;
        public uint Offset { get => _offset; set => _offset = value; }
        ArgumentPassType IArgument.PassType => _passType ?? ArgumentPassType.ByValue;
        public Privacy Privacy => _privacy;
        public bool ReadOnly => false;
        public Privacy ReadPrivacy => _privacy;
        public bool Static => _static;
        public string WbdkName => _wbdkName;
        public Privacy WritePrivacy => _privacy;

        // TODO: remove
        //public Task<CodeFragment> ToWbdkCode_ReadAsync(CodeFragment parentFragment, CodeSpan fieldSpan, ISourceCodeReporter report)
        //{
        //    if (_static || _passType != null)
        //    {
        //        // Stored as a WBDK variable.
        //        return Task.FromResult(new CodeFragment(_wbdkName, _dataType, OpPrec.None, fieldSpan, readOnly: false));
        //    }
        //    else
        //    {
        //        // Member variable which needs to use DKX accessor functions.
        //        return Task.FromResult(Objects.ObjectAccess.GenerateMemberVariableGetter(parentFragment, _offset, _dataType, fieldSpan));
        //    }
        //}

        //public Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment parentFragment, CodeSpan fieldSpan, CodeFragment valueFragment, ISourceCodeReporter report)
        //{
        //    if (_static || _passType != null)
        //    {
        //        // Stored as a WBDK variable.
        //        return Task.FromResult(new CodeFragment(_wbdkName, _dataType, OpPrec.None, fieldSpan, readOnly: false));
        //    }
        //    else
        //    {
        //        // Member variable which needs to use DKX accessor functions.
        //        return Task.FromResult(Objects.ObjectAccess.GenerateMemberVariableSetter(parentFragment, _offset, _dataType, fieldSpan, valueFragment));
        //    }
        //}
    }
}
