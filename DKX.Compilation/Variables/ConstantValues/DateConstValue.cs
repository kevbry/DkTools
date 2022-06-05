using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Variables.ConstantValues
{
    class DateConstValue : ConstValue
    {
        private DkDate _date;

        public DateConstValue(DkDate date, Span span)
            : base(span)
        {
            _date = date;
        }

        public DateConstValue(BsonObject obj, Span span)
            : base(span)
        {
            _date = new DkDate(obj.GetUInt16("Value"));
        }

        public override void SaveInner(BsonObject obj)
        {
            obj.SetUInt16("Value", _date.Value);
        }

        public override DataType DataType => DataType.Date;
        public override DkDate Date => _date;
        public override bool IsDate => true;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(CodeParser.StringToStringLiteral(_date.ToString()), DataType.Date, Expressions.OpPrec.None, Span, constant: this);
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            if (!rightValue.IsDate && !rightValue.IsNumber)
            {
                reportOrNull?.Report(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsDate)
            {
                return op.GetCompareResult(((decimal)_date.Value).CompareTo((decimal)rightValue.Date.Value));
            }
            else
            {
                return op.GetCompareResult(((decimal)_date.Value).CompareTo(rightValue.Number));
            }
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            var span = Span.Envelope(rightValue.Span);
            if (!rightValue.IsDate && rightValue.IsNumber)
            {
                reportOrNull?.Report(span, ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsDate)
            {
                var result = op.GetMathResult((decimal)_date.Value, (decimal)rightValue.Date.Value, reportOrNull, span);
                if (result < DkDate.MinValue.Value || result > DkDate.MaxValue.Value)
                {
                    reportOrNull?.Report(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new DateConstValue(new DkDate((ushort)result), span);
            }
            else
            {
                var result = op.GetMathResult((decimal)_date.Value, rightValue.Number, reportOrNull, span);
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
