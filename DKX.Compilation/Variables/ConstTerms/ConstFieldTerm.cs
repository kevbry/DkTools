using DKX.Compilation.DataTypes;
using DKX.Compilation.Project;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Variables.ConstTerms
{
    class ConstFieldTerm : ConstTerm
    {
        private string _fullClassName;
        private string _fieldName;
        private DataType _dataType;

        public ConstFieldTerm(string fullClassName, string fieldName, DataType dataType, Span span)
            : base(span)
        {
            _fullClassName = fullClassName ?? throw new ArgumentNullException(nameof(fullClassName));
            _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _dataType = dataType;
        }

        public ConstFieldTerm(BsonObject obj, Span span)
            : base(span)
        {
            _fullClassName = obj.GetString("FullClassName");
            _fieldName = obj.GetString("FieldName");
            _dataType = obj.GetDataType("DataType");
        }

        protected override void SaveInner(BsonObject obj)
        {
            obj.SetString("FullClassName", _fullClassName);
            obj.SetString("FieldName", _fieldName);
            obj.SetDataType("DataType", _dataType);
        }

        public override DataType DataType => _dataType;

        internal override ConstValue ResolveConstantOrNull(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList)
        {
            var cls = context.Project.GetClassByFullNameOrNull(_fullClassName);
            if (cls == null) context.Report.Report(Span, ErrorCode.ClassNotFound, _fullClassName);

            var field = cls.GetFields(_fieldName).FirstOrDefault();
            if (field == null) context.Report.Report(Span, ErrorCode.FieldNotFound, _fieldName);

            var value = field.ConstantValue;
            if (value != null) return value;    // The referenced field has already been resolved

            var fieldId = $"{_fullClassName}.{_fieldName}";
            if (circularDependencyCheckList.Contains(fieldId)) throw new CircularConstantDependencyException(field);

            var reportItems = new ReportItemCollector();
            var subContext = new ConstResolutionContext(reportItems, context.Project);
            value = field.ConstantExpression.ResolveConstantOrNull(subContext, circularDependencyCheckList.Concat(new string[] { fieldId }));
            if (value != null)
            {
                reportItems.ReportTo(context.Report);
                return value;
            }

            return null;
        }
    }
}
