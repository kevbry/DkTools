using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstTerms;

namespace DKX.Compilation.Expressions
{
    class DataTypeChain : Chain
    {
        private DataType _dataType;

        public DataTypeChain(DataType dataType, Span span)
            : base(span)
        {
            _dataType = dataType;
        }

        public override DataType DataType => _dataType;
        public override DataType InferredDataType => _dataType;
        public override bool IsEmptyCode => false;
        public override bool IsStatic => true;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            return new CodeFragment("0", _dataType, OpPrec.None, Span, reportable: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            context.Report.Report(valueFragment.SourceSpan, ErrorCode.StaticReferenceCannotBeModified);
            return new CodeFragment("0", _dataType, OpPrec.None, Span, reportable: true);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
