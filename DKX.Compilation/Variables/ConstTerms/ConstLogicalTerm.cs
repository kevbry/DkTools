using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Variables.ConstTerms
{
    /// <summary>
    /// Stores unresolved constants for logical operators ( && || )
    /// </summary>
    class ConstLogicalTerm : ConstTerm
    {
        private Operator _op;
        private ConstTerm _left;
        private ConstTerm _right;

        public ConstLogicalTerm(Operator op, ConstTerm left, ConstTerm right, Span span)
            : base(span)
        {
            _op = op;
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public ConstLogicalTerm(BsonObject obj, Span span)
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

            if (!leftValue.IsBool || !rightValue.IsBool)
            {
                context.Report.Report(Span, ErrorCode.ExpressionMustBeBool);
                return null;
            }

            switch (_op)
            {
                case Operator.And:
                    return new BoolConstValue(leftValue.Bool && rightValue.Bool, Span);
                case Operator.Or:
                    return new BoolConstValue(leftValue.Bool || rightValue.Bool, Span);
                default:
                    throw new InvalidOperatorException();
            }
        }
    }
}
