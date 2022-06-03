using DK.Code;
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
        private ModifierFlags _flags;
        private FieldAccessMethod _accessMethod;
        private FileContext _fileContext;
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
            _flags = fileField.Flags;
            _accessMethod = fileField.AccessMethod;
            _fileContext = fileField.FileContext;
            _offset = fileField.Offset;
            _constExp = fileField.ConstantExpression;
            _constValue = fileField.ConstantValue;
            _span = fileField.DefinitionSpan;
        }

        private ProjectField(ProjectClass class_, string name, DataType dataType, bool readOnly, Privacy readPrivacy, Privacy writePrivacy,
            ModifierFlags flags, FieldAccessMethod accessMethod, FileContext fileContext, uint offset, ConstTerm constExp, ConstValue constValue, Span span)
        {
            _class = class_;
            _name = name;
            _dataType = dataType;
            _readOnly = readOnly;
            _readPrivacy = readPrivacy;
            _writePrivacy = writePrivacy;
            _flags = flags;
            _accessMethod = accessMethod;
            _fileContext = fileContext;
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
        public FileContext FileContext => _fileContext;
        public ModifierFlags Flags => _flags;
        public string Name => _name;
        public uint Offset => _offset;
        public bool ReadOnly => _readOnly;
        public Privacy ReadPrivacy => _readPrivacy;
        public Privacy WritePrivacy => _writePrivacy;

        public BsonObject ToBson(BsonFile bson)
        {
            var obj = new BsonObject(bson);

            obj.SetString("Name", _name);
            obj.SetDataType("DataType", _dataType);
            obj.SetBoolean("ReadOnly", _readOnly);
            obj.SetEnum("ReadPrivacy", _readPrivacy);
            obj.SetEnum("WritePrivacy", _writePrivacy);
            obj.SetUInt32("Flags", (uint)_flags);
            obj.SetEnum("AccessMethod", _accessMethod);
            obj.SetEnum("FileContext", _fileContext);
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
            var flags = (ModifierFlags)obj.GetUInt32("Flags");
            var accessMethod = obj.GetEnum<FieldAccessMethod>("AccessMethod");
            var fileContext = obj.GetEnum<FileContext>("FileContext");
            var offset = obj.GetUInt32("Offset");
            node = obj.GetProperty("ConstantExpression", throwIfMissing: false);
            var constExp = node != null ? ConstTerm.FromBson(node) : null;
            node = obj.GetProperty("ConstantValue", throwIfMissing: false);
            var constValue = node != null ? ConstValue.FromBson(node) : null;
            var span = obj.GetSpan("DefinitionSpan");

            return new ProjectField(class_, name, dataType, readOnly, readPrivacy, writePrivacy, flags, accessMethod, fileContext, offset, constExp, constValue, span);
        }

        public void ClearConstant()
        {
            _constValue = null;
        }

        public void ResolveConstant(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList)
        {
            var myId = $"{_class.FullClassName}.{_name}";
            if (circularDependencyCheckList.Contains(myId)) throw new CircularConstantDependencyException(this);

            if (_constExp == null) throw new InvalidOperationException("Constant has no expression.");

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
