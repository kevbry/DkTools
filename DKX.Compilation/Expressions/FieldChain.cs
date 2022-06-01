using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Objects;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class FieldChain : Chain
    {
        private Chain _thisChain;
        private string _memberName;
        private IField _field;

        public FieldChain(Chain thisChain, DkxToken nameToken, IField field)
            : base(nameToken.Span)
        {
            _thisChain = thisChain ?? throw new ArgumentNullException(nameof(thisChain));
            _memberName = nameToken.Text;
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public override DataType DataType => _field.DataType;
        public override DataType InferredDataType => _field.DataType;
        public override bool IsEmptyCode => false;

        public override string ToString() => $"{{FieldChain: {_field.Name}}}";

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            context.DependsOnFile(_field.Class.DkxPathName);

            switch (_field.AccessMethod)
            {
                case FieldAccessMethod.Variable:
                    return new CodeFragment(_field.Name, _field.DataType, OpPrec.None, Span, readOnly: _field.ReadOnly);
                case FieldAccessMethod.Object:
                    var thisFrag = _thisChain.ToWbdkCode_Read(context);
                    return ObjectAccess.GenerateMemberVariableGetter(thisFrag, _field.Offset, _field.DataType, Span);
                case FieldAccessMethod.Property:
                    thisFrag = _thisChain.ToWbdkCode_Read(context);
                    return new CodeFragment($"{_field.Class.WbdkClassName}.{DkxConst.Properties.GetterPrefix}{_field.Name}({thisFrag})", _field.DataType, OpPrec.None, Span, readOnly: true);
                case FieldAccessMethod.Constant:
                    var value = _field.ConstantValue;
                    if (value == null)
                    {
                        context.Report.Report(Span, ErrorCode.ConstantNotResolved);
                        value = _field.DataType.CreateDefaultConstValue(Span);
                    }
                    return value.ToWbdkCode();
                default:
                    throw new InvalidFieldAccessMethodException();
            }
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            context.DependsOnFile(_field.Class.DkxPathName);

            switch (_field.AccessMethod)
            {
                case FieldAccessMethod.Variable:
                    return new CodeFragment($"{_field.Name} = {valueFragment.Protect(OpPrec.Assign)}", _field.DataType, OpPrec.Assign, Span, readOnly: false);
                case FieldAccessMethod.Object:
                    var thisFrag = _thisChain.ToWbdkCode_Read(context);
                    return ObjectAccess.GenerateMemberVariableSetter(thisFrag, _field.Offset, _field.DataType, Span, valueFragment);
                case FieldAccessMethod.Property:
                    thisFrag = _thisChain.ToWbdkCode_Read(context);
                    return new CodeFragment($"{_field.Class.WbdkClassName}.{DkxConst.Properties.SetterPrefix}{_field.Name}({thisFrag}, {valueFragment})", _field.DataType, OpPrec.None, Span, readOnly: true);
                case FieldAccessMethod.Constant:
                    context.Report.Report(Span, ErrorCode.ExpressionCannotBeWrittenTo);
                    return CodeFragment.Empty;
                default:
                    throw new InvalidFieldAccessMethodException();
            }
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report)
        {
            if (_field.AccessMethod == FieldAccessMethod.Constant)
            {
                var value = _field.ConstantValue;
                if (value != null) return new ConstValueTerm(value, Span);
                else return new ConstFieldTerm(_field.Class.FullClassName, _field.Name, _field.DataType, Span);
            }

            return null;
        }
    }
}
