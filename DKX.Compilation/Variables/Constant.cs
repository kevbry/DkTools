using DK;
using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables
{
    class Constant : IField
    {
        private string _name;
        private DataType _dataType;
        private ConstantValue _value;
        private Privacy _privacy;

        public Constant(string name, DataType dataType, ConstantValue value, Privacy privacy)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            if (!_name.IsWord()) throw new ArgumentException("Constant name must be a single word.");

            _dataType = dataType;
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _privacy = privacy;
        }

        public string Name => _name;
        public DataType DataType => _dataType;
        public bool IsConstant => true;
        public bool ReadOnly => true;
        public Privacy ReadPrivacy => _privacy;
        public bool Static => true;
        public ConstantValue Value => _value;
        public Privacy WritePrivacy => _privacy;

        public Task<CodeFragment> ToWbdkCode_ReadAsync(CodeFragment parentFragment, CodeSpan fieldSpan, ISourceCodeReporter report)
        {
            return Task.FromResult(_value.ToWbdkCode());
        }

        public Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment parentFragment, CodeSpan fieldSpan, CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(fieldSpan, ErrorCode.ExpressionCannotBeWrittenTo);
        }
    }
}
