using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Variables.ConstantValues
{
    class CharConstValue : ConstValue
    {
        private char _ch;

        public CharConstValue(char ch, Span span)
            : base(span)
        {
            _ch = ch;
        }

        public CharConstValue(BsonObject obj, Span span)
            : base(span)
        {
            _ch = (char)obj.GetUInt16("Value");
        }

        public override void SaveInner(BsonObject obj)
        {
            obj.SetUInt16("Value", _ch);
        }

        public override bool Bool => default;
        public override char Char => _ch;
        public override DataType DataType => DataType.Char;
        public override bool IsBool => false;
        public override bool IsChar => true;
        public override bool IsNull => false;
        public override bool IsNumber => false;
        public override bool IsString => false;
        public override decimal Number => (decimal)_ch;
        public override string String => default;

        internal override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(CodeParser.CharToCharLiteral(_ch), DataType.Char, OpPrec.None, Span, constant: this, reportable: true);
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            if (!rightValue.IsChar && !rightValue.IsNumber)
            {
                reportOrNull?.Report(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsChar)
            {
                return op.GetCompareResult(((decimal)_ch).CompareTo((decimal)rightValue.Char));
            }
            else
            {
                return op.GetCompareResult(((decimal)_ch).CompareTo(rightValue.Number));
            }
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            var span = Span.Envelope(rightValue.Span);
            if (!rightValue.IsChar && rightValue.IsNumber)
            {
                reportOrNull?.Report(span, ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsChar)
            {
                var result = op.GetMathResult((decimal)_ch, (decimal)rightValue.Char, reportOrNull, span);
                if (result < char.MinValue || result > char.MaxValue)
                {
                    reportOrNull?.Report(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new CharConstValue((char)result, span);
            }
            else
            {
                var result = op.GetMathResult((decimal)_ch, rightValue.Number, reportOrNull, span);
                if (result < char.MinValue || result > char.MaxValue)
                {
                    reportOrNull?.Report(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new CharConstValue((char)result, span);
            }
        }
    }
}
