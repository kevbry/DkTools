using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Objects;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
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
            if (!field.Flags.IsStatic())
            {
                if (thisChain == null) throw new ArgumentNullException(nameof(thisChain));
            }

            _thisChain = thisChain;
            _memberName = nameToken.Text;
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public override DataType DataType => _field.DataType;
        public override DataType InferredDataType => _field.DataType;
        public override bool IsEmptyCode => false;

        public override string ToString() => $"{{FieldChain: {_field.Name}}}";

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            context.DependsOnFile(_field.Class.DkxPathName);

            if (context.IsOutsideClass(_field.Class) && _field.ReadPrivacy != Privacy.Public)
            {
                throw new CodeException(Span, ErrorCode.CannotAccessMemberDueToPrivacy, _field.Name, _field.ReadPrivacy.ToString().ToLower());
            }

            switch (_field.AccessMethod)
            {
                case FieldAccessMethod.Variable:
                    return new CodeFragment(_field.Name, _field.DataType, OpPrec.None, Span, reportable: true);
                case FieldAccessMethod.Object:
                    var thisFrag = _thisChain.ToWbdkCode_Read(context, flow);
                    if (thisFrag.IsUnownedObjectReference) thisFrag = ObjectAccess.GenerateReleaseDefer(context, thisFrag);
                    return ObjectAccess.GenerateMemberVariableGetter(thisFrag, _field.Offset, _field.DataType, Span);
                case FieldAccessMethod.Property:
                    if (_field.Flags.IsStatic())
                    {
                        return new CodeFragment($"{_field.Class.WbdkClassName}.{DkxConst.Properties.GetterPrefix}{_field.Name}()",
                            _field.DataType, OpPrec.None, Span, reportable: true,
                            flags: _field.DataType.IsClass ? CodeFragmentFlags.UnownedObjectReference : default);
                    }
                    else
                    {
                        thisFrag = _thisChain.ToWbdkCode_Read(context, flow);
                        if (thisFrag.DataType.IsClass && thisFrag.IsUnownedObjectReference) thisFrag = ObjectAccess.GenerateReleaseDefer(context, thisFrag);
                        return new CodeFragment($"{_field.Class.WbdkClassName}.{DkxConst.Properties.GetterPrefix}{_field.Name}({thisFrag})",
                            _field.DataType, OpPrec.None, Span, reportable: true,
                            flags: _field.DataType.IsClass ? CodeFragmentFlags.UnownedObjectReference : default);
                    }
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

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            context.DependsOnFile(_field.Class.DkxPathName);

            if (_field.Flags.IsReadOnly()) throw new CodeException(Span, ErrorCode.PropertyIsReadOnly, _field.Name);

            if (context.IsOutsideClass(_field.Class) && _field.WritePrivacy != Privacy.Public)
            {
                throw new CodeException(Span, ErrorCode.CannotAccessMemberDueToPrivacy, _field.Name, _field.WritePrivacy.ToString().ToLower());
            }

            switch (_field.AccessMethod)
            {
                case FieldAccessMethod.Variable:
                    return new CodeFragment($"{_field.Name} = {valueFragment.Protect(OpPrec.Assign)}", _field.DataType, OpPrec.Assign, Span, reportable: false);
                case FieldAccessMethod.Object:
                    var thisFrag = _thisChain.ToWbdkCode_Read(context, flow);

                    if (_field.DataType.IsClass)
                    {
                        var valueFlags = valueFragment.Flags;

                        valueFragment = ObjectAccess.GenerateSwapLink(
                            objFragment: thisFrag,
                            oldFragment: ObjectAccess.GenerateMemberVariableGetter(thisFrag, _field.Offset, _field.DataType, Span),
                            newFragment: valueFragment);

                        if (valueFlags.HasFlag(CodeFragmentFlags.UnownedObjectReference))
                        {
                            // We need to release the ref count on the value.
                            valueFragment = ObjectAccess.GenerateReleaseDefer(context, valueFragment);
                        }
                    }

                    return ObjectAccess.GenerateMemberVariableSetter(thisFrag, _field.Offset, _field.DataType, Span, valueFragment);
                case FieldAccessMethod.Property:
                    if (valueFragment.DataType.IsClass && !valueFragment.IsUnownedObjectReference) valueFragment = ObjectAccess.GenerateAddReference(valueFragment);
                    if (_field.Flags.IsStatic())
                    {
                        return new CodeFragment($"{_field.Class.WbdkClassName}.{DkxConst.Properties.SetterPrefix}{_field.Name}({valueFragment})",
                            _field.DataType, OpPrec.None, Span, reportable: false);
                    }
                    else
                    {
                        thisFrag = _thisChain.ToWbdkCode_Read(context, flow);
                        if (thisFrag.DataType.IsClass && thisFrag.IsUnownedObjectReference) thisFrag = ObjectAccess.GenerateReleaseDefer(context, thisFrag);
                        return new CodeFragment($"{_field.Class.WbdkClassName}.{DkxConst.Properties.SetterPrefix}{_field.Name}({thisFrag}, {valueFragment})",
                            _field.DataType, OpPrec.None, Span, reportable: false);
                    }
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
