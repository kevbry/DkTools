using DK.Code;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Expressions
{
    class ConstantChain : Chain
    {
        private Constant _constant;

        public ConstantChain(Constant constant, CodeSpan span)
            : base(span)
        {
            _constant = constant ?? throw new ArgumentNullException(nameof(constant));
        }

        public override string ToCode(int parentOffset)
        {
            return _constant.Value.ToCode(-1);
        }

        public override void Report(ISourceCodeReporter reporter)
        {
            _constant.Value.Report(reporter);
        }
    }
}
