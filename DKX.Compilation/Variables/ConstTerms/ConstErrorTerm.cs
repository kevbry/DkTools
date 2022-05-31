using DKX.Compilation.DataTypes;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Variables.ConstantValues;
using System.Collections.Generic;

namespace DKX.Compilation.Variables.ConstTerms
{
    class ConstErrorTerm : ConstTerm
    {
        public ConstErrorTerm(Span span) : base(span) { }

        public ConstErrorTerm(BsonObject bin, Span span) : base(span) { }

        protected override void SaveInner(BsonObject obj) { }

        public override DataType DataType => DataType.Int;

        internal override ConstValue ResolveConstantOrNull(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList) => null;
    }
}
