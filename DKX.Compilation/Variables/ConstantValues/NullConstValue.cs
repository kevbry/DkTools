using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;
using System.IO;

namespace DKX.Compilation.Variables.ConstantValues
{
    class NullConstValue : ConstValue
    {
        public NullConstValue(Span span) : base(span) { }

        public NullConstValue(BinaryReader bin, Span span) : base(span) { }

        public override void SaveInner(BsonObject obj) { }

        public override DataType DataType => DataType.Int;
        public override bool IsNull => true;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment("0", DataType.Int, OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            reportOrNull?.Report(Span + rightValue.Span, ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
            return null;
        }

        public override bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull)
        {
            reportOrNull?.Report(Span + rightValue.Span, ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
            return null;
        }
    }
}
