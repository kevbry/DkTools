using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class ConstantChain : Chain
    {
        private Constant _constant;

        public ConstantChain(Constant constant, Span span)
            : base(span)
        {
            _constant = constant ?? throw new ArgumentNullException(nameof(constant));
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => _constant.DataType;

        public override DataType InferredDataType => _constant.DataType;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            var value = _constant.ConstantValue;
            if (value == null) throw new CodeException(Span, ErrorCode.ConstantNotResolved);
            return value.ToWbdkCode();
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report)
        {
            var value = _constant.ConstantValue;
            if (value != null) return new ConstValueTerm(value, Span);
            else return new ConstFieldTerm(_constant.Class.FullClassName, _constant.Name, _constant.DataType, Span);
        }
    }
}
