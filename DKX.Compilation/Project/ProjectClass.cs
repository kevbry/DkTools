using DKX.Compilation.Project.Bson;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Project
{
    class ProjectClass : IClass
    {
        private string _className;
        private string _fullClassName;
        private string _namespaceName;
        private string _wbdkClassName;
        private string _dkxPathName;
        private Privacy _privacy;
        private ModifierFlags _flags;
        private uint _dataSize;
        private Span _nameSpan;
        private List<ProjectMethod> _methods;
        private List<ProjectField> _fields;
        private DateTime _scanTime = DateTime.MinValue;

        public ProjectClass(string className, string fullClassName, string namespaceName, string wbdkClassName, string dkxPathName, Span nameSpan)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
            _fullClassName = fullClassName ?? throw new ArgumentNullException(nameof(fullClassName));
            _namespaceName = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
            _wbdkClassName = wbdkClassName ?? throw new ArgumentNullException(nameof(wbdkClassName));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _nameSpan = nameSpan;
        }

        private ProjectClass(string className, string fullClassName, string namespaceName, string wbdkClassName, string dkxPathName,
            Privacy privacy, ModifierFlags flags, uint dataSize, DateTime scanTime, Span nameSpan)
        {
            _className = className;
            _fullClassName = fullClassName;
            _namespaceName = namespaceName;
            _wbdkClassName = wbdkClassName;
            _dkxPathName = dkxPathName;
            _privacy = privacy;
            _flags = flags;
            _dataSize = dataSize;
            _scanTime = scanTime;
            _nameSpan = nameSpan;
        }

        public string ClassName => _className;
        public uint DataSize => _dataSize;
        public string DkxPathName => _dkxPathName;
        IEnumerable<IField> IClass.Fields => _fields;
        public IEnumerable<ProjectField> Fields => _fields;
        public ModifierFlags Flags => _flags;
        public string FullClassName => _fullClassName;
        IEnumerable<IMethod> IClass.Methods => _methods;
        public IEnumerable<ProjectMethod> Methods => _methods;
        public string NamespaceName => _namespaceName;
        public Span NameSpan => _nameSpan;
        public Privacy Privacy => _privacy;
        public string WbdkClassName => _wbdkClassName;

        public override string ToString() => $"ProjectClass: {_fullClassName}";

        public void Update(CompilePhase phase, IClass fileClass)
        {
            _scanTime = DateTime.Now;

            _wbdkClassName = fileClass.WbdkClassName;
            _dkxPathName = fileClass.DkxPathName;
            _privacy = fileClass.Privacy;
            _flags = fileClass.Flags;
            _dataSize = fileClass.DataSize;
            _nameSpan = fileClass.NameSpan;
            _methods = fileClass.Methods.Select(x => new ProjectMethod(this, x)).ToList();
            _fields = fileClass.Fields.Select(x => new ProjectField(this, x)).ToList();

            if (phase == CompilePhase.ConstantResolution)
            {
                foreach (var field in _fields)
                {
                    if (field.AccessMethod == Variables.FieldAccessMethod.Constant)
                    {
                        if (field.ConstantExpression == null) throw new InvalidOperationException("A constant field was received with no expression.");
                    }
                }
            }
        }

        IEnumerable<IMethod> IClass.GetMethods(string name) => ((IEnumerable<IMethod>)_methods ?? IMethodHelper.EmptyArray).Where(x => x.Name == name);

        IEnumerable<IField> IClass.GetFields(string name) => ((IEnumerable<IField>)_fields ?? IFieldHelper.EmptyArray).Where(x => x.Name == name);

        public BsonObject ToBson(BsonFile bson)
        {
            var obj = new BsonObject(bson);

            obj.SetString("ClassName", _className);
            obj.SetString("FullClassName", _fullClassName);
            obj.SetString("NamespaceName", _namespaceName);
            obj.SetString("WbdkClassName", _wbdkClassName);
            obj.SetString("DkxPathName", _dkxPathName);
            obj.SetEnum("Privacy", _privacy);
            obj.SetUInt32("Flags", (uint)_flags);
            obj.SetUInt32("DataSize", _dataSize);
            obj.SetDateTime("ScanTime", _scanTime);
            obj.SetSpan("NameSpan", _nameSpan);
            obj.SetArray("Methods", _methods.Select(x => x.ToBson(bson)));
            obj.SetArray("Fields", _fields.Select(x => x.ToBson(bson)));

            return obj;
        }

        public static ProjectClass FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonTypeException();

            var className = obj.GetString("ClassName");
            var fullClassName = obj.GetString("FullClassName");
            var namespaceName = obj.GetString("NamespaceName");
            var wbdkClassName = obj.GetString("WbdkClassName");
            var dkxPathName = obj.GetString("DkxPathName");
            var privacy = obj.GetEnum<Privacy>("Privacy");
            var flags = (ModifierFlags)obj.GetUInt32("Flags");
            var dataSize = obj.GetUInt32("DataSize");
            var scanTime = obj.GetDateTime("ScanTime");
            var nameSpan = obj.GetSpan("NameSpan");

            var cls = new ProjectClass(className, fullClassName, namespaceName, wbdkClassName, dkxPathName, privacy, flags, dataSize, scanTime, nameSpan);

            foreach (var bsonMethod in obj.GetArray("Methods")) cls._methods.Add(ProjectMethod.FromBson(cls, bsonMethod));
            foreach (var bsonField in obj.GetArray("Fields")) cls._fields.Add(ProjectField.FromBson(cls, bsonField));

            return cls;
        }

        public void ClearAllConstants()
        {
            foreach (var field in _fields)
            {
                if (field.AccessMethod == Variables.FieldAccessMethod.Constant)
                {
                    field.ClearConstant();
                }
            }
        }

        public void ResolveAllConstants(ConstResolutionContext context)
        {
            foreach (var field in _fields)
            {
                if (field.AccessMethod == Variables.FieldAccessMethod.Constant)
                {
                    if (field.ConstantValue == null)
                    {
                        field.ResolveConstant(context, DkxConst.EmptyStringArray);
                    }
                }
            }
        }

        public int CountUnresolvedConstants()
        {
            var count = 0;

            foreach (var field in _fields)
            {
                if (field.AccessMethod == Variables.FieldAccessMethod.Constant)
                {
                    if (field.ConstantValue == null) count++;
                }
            }

            return count;
        }
    }
}
