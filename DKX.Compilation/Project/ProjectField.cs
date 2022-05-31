using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DKX.Compilation.Project
{
    class ProjectField : IField
    {
        private ProjectClass _class;
        private string _name;
        private DataType _dataType;
        private bool _readOnly;
        private Privacy _readPrivacy;
        private Privacy _writePrivacy;
        private bool _static;
        private FieldAccessMethod _accessMethod;
        private uint _offset;
        private ConstTerm _constExp;
        private ConstValue _constValue;
        private Span _span;

        public ProjectField(ProjectClass class_, IField fileField)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
            _name = fileField.Name;
            _dataType = fileField.DataType;
            _readOnly = fileField.ReadOnly;
            _readPrivacy = fileField.ReadPrivacy;
            _writePrivacy = fileField.WritePrivacy;
            _static = fileField.Static;
            _accessMethod = fileField.AccessMethod;
            _offset = fileField.Offset;
            _constExp = fileField.ConstantExpression;
            _constValue = fileField.ConstantValue;
            _span = fileField.DefinitionSpan;
        }

        private ProjectField(ProjectClass class_, string name, DataType dataType, bool readOnly, Privacy readPrivacy, Privacy writePrivacy,
            bool static_, FieldAccessMethod accessMethod, uint offset, ConstTerm constExp, ConstValue constValue, Span span)
        {
            _class = class_;
            _name = name;
            _dataType = dataType;
            _readOnly = readOnly;
            _readPrivacy = readPrivacy;
            _writePrivacy = writePrivacy;
            _static = static_;
            _accessMethod = accessMethod;
            _offset = offset;
            _constExp = constExp;
            _constValue = constValue;
            _span = span;
        }

        public FieldAccessMethod AccessMethod => _accessMethod;
        public ProjectClass Class => _class;
        IClass IField.Class => _class;
        public ConstTerm ConstantExpression => _constExp;
        public ConstValue ConstantValue => _constValue;
        public DataType DataType => _dataType;
        public Span DefinitionSpan => _span;
        public string Name => _name;
        public uint Offset => _offset;
        public bool ReadOnly => _readOnly;
        public Privacy ReadPrivacy => _readPrivacy;
        public bool Static => _static;
        public Privacy WritePrivacy => _writePrivacy;

        public BsonObject ToBson(BsonFile bson)
        {
            var obj = new BsonObject(bson);

            obj.SetString("Name", _name);
            obj.SetDataType("DataType", _dataType);
            obj.SetBoolean("ReadOnly", _readOnly);
            obj.SetEnum("ReadPrivacy", _readPrivacy);
            obj.SetEnum("WritePrivacy", _writePrivacy);
            obj.SetBoolean("Static", _static);
            obj.SetEnum("AccessMethod", _accessMethod);
            obj.SetUInt32("Offset", _offset);
            if (_constExp != null) obj["ConstantExpression"] = _constExp.ToBson(bson);
            if (_constValue != null) obj["ConstantValue"] = _constValue.ToBson(bson);
            obj.SetSpan("DefinitionSpan", _span);

            return obj;
        }

        public static ProjectField FromBson(ProjectClass class_, BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonDataException("Field node is not an object.");

            var name = obj.GetString("Name");
            var dataType = obj.GetDataType("DataType");
            var readOnly = obj.GetBoolean("ReadOnly");
            var readPrivacy = obj.GetEnum<Privacy>("ReadPrivacy");
            var writePrivacy = obj.GetEnum<Privacy>("WritePrivacy");
            var static_ = obj.GetBoolean("Static");
            var accessMethod = obj.GetEnum<FieldAccessMethod>("AccessMethod");
            var offset = obj.GetUInt32("Offset");
            node = obj.GetProperty("ConstantExpression", throwIfMissing: false);
            var constExp = node != null ? ConstTerm.FromBson(node) : null;
            node = obj.GetProperty("ConstantValue", throwIfMissing: false);
            var constValue = node != null ? ConstValue.FromBson(node) : null;
            var span = obj.GetSpan("DefinitionSpan");

            return new ProjectField(class_, name, dataType, readOnly, readPrivacy, writePrivacy, static_, accessMethod, offset, constExp, constValue, span);
        }

        public void ClearConstant()
        {
            _constValue = null;
        }

        public void ResolveConstant(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList)
        {
            var myId = $"{_class.FullClassName}.{_name}";
            if (circularDependencyCheckList.Contains(myId)) throw new CircularConstantDependencyException(this);

            var value = _constExp.ResolveConstantOrNull(context, circularDependencyCheckList.Concat(new string[] { myId }).ToList());
            if (value != null) _constValue = value;
        }
    }

    class CircularConstantDependencyException : CompilerException
    {
        private IField _field;

        public CircularConstantDependencyException(IField field)
        {
            _field = field;
        }

        public IField Field => _field;
    }
}
