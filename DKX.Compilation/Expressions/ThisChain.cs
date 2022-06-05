using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class ThisChain : Chain
    {
        private DataType _dataType;

        public ThisChain(DataType dataType, Span span)
            : base(span)
        {
            if (dataType.BaseType != BaseType.Class) throw new ArgumentException("Data type must be a class.");
            _dataType = dataType;
        }

        public override DataType DataType => _dataType;
        public override DataType InferredDataType => _dataType;
        public override bool IsEmptyCode => false;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            return new CodeFragment(DkxConst.This, _dataType, OpPrec.None, Span);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            context.Report.Report(valueFragment.SourceSpan, ErrorCode.ThisCannotBeModified);
            return new CodeFragment(DkxConst.This, _dataType, OpPrec.None, Span);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => null;
    }
}
