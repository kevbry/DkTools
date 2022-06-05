using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Variables.ConstantValues
{
    class NumberConstValue : ConstValue
    {
        private decimal _value;
        private DataType _dataType;

        public NumberConstValue(decimal value, DataType dataType, Span span)
            : base(span)
        {
            _value = value;
            _dataType = dataType;
        }

        public NumberConstValue(BsonObject obj, Span span)
            : base(span)
        {
            _value = obj.GetDecimal("Value");
            _dataType = obj.GetDataType("DataType");
        }

        public override void SaveInner(BsonObject obj)
        {
            obj.SetDecimal("Value", _value);
            obj.SetDataType("DataType", _dataType);
        }

        public override bool Bool => false;
        public override char Char => default;
        public override DataType DataType => _dataType;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => false;
        public override bool IsNumber => true;
        public override bool IsString => false;
        public override decimal Number => _value;
        public override string String => default;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(_value.ToString(), _dataType, Expressions.OpPrec.None, Span, constant: this);
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            var span = Span.Envelope(rightValue.Span);

            if (rightValue.IsNumber)
            {
                var result = op.GetMathResult(_value, rightValue.Number, reportOrNull, span);
                if (result < _dataType.MinNumber || result > _dataType.MaxNumber) reportOrNull?.Report(Span, ErrorCode.ConstantValueOutOfRange, _dataType);
                return new NumberConstValue(result, _dataType, span);
            }

            if (rightValue.IsChar)
            {
                var result = op.GetMathResult(_value, (decimal)rightValue.Char, reportOrNull, span);
                if (result < _dataType.MinNumber || result > _dataType.MaxNumber) reportOrNull?.Report(Span, ErrorCode.ConstantValueOutOfRange, _dataType);
                return new NumberConstValue(result, _dataType, span);
            }

            reportOrNull?.Report(span, ErrorCode.OperatorCannotBeUsedWithThisDataType, rightValue.DataType);
            return null;
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            if (rightValue.IsNumber)
            {
                return op.GetCompareResult(decimal.Compare(_value, rightValue.Number));
            }

            if (rightValue.IsChar)
            {
                return op.GetCompareResult(decimal.Compare(_value, (decimal)rightValue.Char));
            }

            reportOrNull?.Report(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType, rightValue.DataType);
            return null;
        }
    }
}
