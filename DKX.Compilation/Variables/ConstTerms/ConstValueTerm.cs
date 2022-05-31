using DKX.Compilation.DataTypes;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Variables.ConstTerms
{
    class ConstValueTerm : ConstTerm
    {
        private ConstValue _value;

        public ConstValueTerm(ConstValue value, Span span)
            : base(span)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public ConstValueTerm(BsonObject obj, Span span)
            : base(span)
        {
            _value = ConstValue.FromBson(obj.GetProperty("Value"));
        }

        protected override void SaveInner(BsonObject obj)
        {
            obj.SetProperty("Value", _value.ToBson(obj.File));
        }

        public override DataType DataType => _value.DataType;

        internal override ConstValue ResolveConstantOrNull(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList) => _value;
    }
}
