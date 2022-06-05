using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Variables.ConstantValues
{
    class TimeConstValue : ConstValue
    {
        private DkTime _time;

        public TimeConstValue(DkTime time, Span span)
            : base(span)
        {
            _time = time;
        }

        public TimeConstValue(BsonObject obj, Span span)
            : base(span)
        {
            _time = new DkTime(obj.GetUInt16("Value"));
        }

        public override void SaveInner(BsonObject obj)
        {
            obj.SetUInt16("Value", _time.Ticks);
        }

        public override DataType DataType => DataType.Time;
        public override bool IsTime => true;
        public override DkTime Time => _time;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(CodeParser.StringToStringLiteral(_time.ToString()), DataType.Time, Expressions.OpPrec.None, Span, constant: this);
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            if (!rightValue.IsTime && !rightValue.IsNumber)
            {
                reportOrNull?.Report(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsTime)
            {
                return op.GetCompareResult(((decimal)_time.Ticks).CompareTo((decimal)rightValue.Time.Ticks));
            }
            else
            {
                return op.GetCompareResult(((decimal)_time.Ticks).CompareTo(rightValue.Number));
            }
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            var span = Span.Envelope(rightValue.Span);
            if (!rightValue.IsTime && rightValue.IsNumber)
            {
                reportOrNull?.Report(span, ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsTime)
            {
                var result = op.GetMathResult((decimal)_time.Ticks, (decimal)rightValue.Time.Ticks, reportOrNull, span);
                if (result < DkDate.MinValue.Value || result > DkDate.MaxValue.Value)
                {
                    reportOrNull?.Report(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new DateConstValue(new DkDate((ushort)result), span);
            }
            else
            {
                var result = op.GetMathResult((decimal)_time.Ticks, rightValue.Number, reportOrNull, span);
                if (result < DkDate.MinValue.Value || result > DkDate.MaxValue.Value)
                {
                    reportOrNull?.Report(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new DateConstValue(new DkDate((ushort)result), span);
            }
        }
    }
}
