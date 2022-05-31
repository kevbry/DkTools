using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Variables.ConstantValues
{
    class StringConstValue : ConstValue
    {
        private string _text;
        private DataType _dataType;

        public StringConstValue(string text, DataType dataType, Span span)
            : base(span)
        {
            _text = text;
            _dataType = dataType;
        }

        public StringConstValue(BsonObject obj, Span span)
            : base(span)
        {
            _text = obj.GetString("Value");
            _dataType = obj.GetDataType("DataType");
        }

        public override void SaveInner(BsonObject obj)
        {
            obj.SetString("Value", _text);
            obj.SetDataType("DataType", _dataType);
        }

        public override bool Bool => default;
        public override char Char => default;
        public override DataType DataType => _dataType;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => false;
        public override bool IsNumber => false;
        public override bool IsString => true;
        public override decimal Number => default;
        public override string String => _text;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(CodeParser.StringToStringLiteral(_text), _dataType, Expressions.OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector report)
        {
            if (!rightValue.IsString)
            {
                report.Report(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            return op.GetCompareResult(string.Compare(_text, rightValue.String, ignoreCase: true));
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector report)
        {
            report.Report(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType);
            return null;
        }
    }
}
