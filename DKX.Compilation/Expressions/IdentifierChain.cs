using DK;
using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.CodeGeneration.Constants;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Expressions
{
    class IdentifierChain : Chain
    {
        private string _name;

        public IdentifierChain(string name, CodeSpan span)
            : base(span)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
#if DEBUG
            if (!_name.IsWord()) throw new ArgumentException("Identifier name must be a single word.");
#endif
        }

        public override void Report(ISourceCodeReporter reporter) { }

        public override OpCodeFragment ReadToVariable(OpCodeGeneratorContext context, string varName, DataType? varDataType)
        {
            return OpCodeFragment.SetVarToIdentifier(Span, varDataType, varName, _name);
        }

        public override OpCodeFragment ReadProvideVariable(OpCodeGeneratorContext context)
        {
            return new OpCodeFragment(dataType: null, _name);
        }

        public override ConstantValue ReadConstant(DataType constDataType) => throw new OpCodeCannotBeConstantException();

        public override OpCodeFragment Execute(OpCodeGeneratorContext context) => throw new OpCodeCannotBeExecutedException();
    }
}
