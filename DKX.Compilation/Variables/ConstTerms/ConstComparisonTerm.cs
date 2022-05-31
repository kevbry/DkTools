using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Variables.ConstTerms
{
    /// <summary>
    /// Stores unresolved constants expressions for comparison operators ( == != < <= > >= )
    /// </summary>
    class ConstComparisonTerm : ConstTerm
    {
        private Operator _op;
        private ConstTerm _left;
        private ConstTerm _right;

        public ConstComparisonTerm(Operator op, ConstTerm left, ConstTerm right, Span span)
            : base(span)
        {
            _op = op;
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public ConstComparisonTerm(BsonObject obj, Span span)
            : base(span)
        {
            _op = obj.GetEnum<Operator>("Operator");
            _left = ConstTerm.FromBson(obj.GetProperty("Left"));
            _right = ConstTerm.FromBson(obj.GetProperty("Right"));
        }

        protected override void SaveInner(BsonObject obj)
        {
            obj.SetEnum("Operator", _op);
            obj.SetProperty("Left", _left.ToBson(obj.File));
            obj.SetProperty("Right", _right.ToBson(obj.File));
        }

        public override DataType DataType => DataType.Bool;

        internal override ConstValue ResolveConstantOrNull(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList)
        {
            var leftValue = _left.ResolveConstantOrNull(context, circularDependencyCheckList);
            if (leftValue == null) return null;

            var rightValue = _right.ResolveConstantOrNull(context, circularDependencyCheckList);
            if (rightValue == null) return null;

            var reportCollector = new ReportItems.ReportItemCollector();
            var result = leftValue.GetComparisonResultOrNull(_op, rightValue, reportCollector);
            if (result != null)
            {
                reportCollector.ReportTo(context.Report);
                return new BoolConstValue(result.Value, Span);
            }

            return null;
        }
    }
}
