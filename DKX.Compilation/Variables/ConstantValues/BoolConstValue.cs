using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Variables.ConstantValues
{
    class BoolConstValue : ConstValue
    {
        private bool _value;

        public BoolConstValue(bool value, Span span)
            : base(span)
        {
            _value = value;
        }

        public BoolConstValue(BsonObject obj, Span span)
            : base(span)
        {
            _value = obj.GetBoolean("Value");
        }

        public override void SaveInner(BsonObject obj)
        {
            obj.SetBoolean("Value", _value);
        }

        public override bool Bool => default;
        public override char Char => default;
        public override DataType DataType => DataType.Bool;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => false;
        public override bool IsNumber => false;
        public override bool IsString => false;
        public override decimal Number => default;
        public override string String => default;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(_value ? "1" : "0", DataType.Bool, Expressions.OpPrec.None, Span, constant: this);
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            if (!rightValue.IsBool)
            {
                reportOrNull?.Report(Span + rightValue.Span, ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            switch (op)
            {
                case Operator.Equal:
                    return _value == rightValue.Bool;
                case Operator.NotEqual:
                    return _value != rightValue.Bool;
                default:
                    reportOrNull?.Report(Span + rightValue.Span, ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
                    return null;
            }
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            reportOrNull?.Report(Span + rightValue.Span, ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
            return null;
        }
    }
}
