using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using System;
using System.Linq;

namespace DKX.Compilation.Project
{
    class ProjectMethod : IMethod
    {
        private ProjectClass _class;
        private string _name;
        private string _wbdkName;
        private DataType _returnDataType;
        private ProjectArgument[] _arguments;
        private Privacy _privacy;
        private bool _static;
        private FileContext _fileContext;
        private Span _span;
        private MethodAccessType _accessType;

        public ProjectMethod(ProjectClass class_, IMethod fileMethod)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
            _name = fileMethod.Name;
            _wbdkName = fileMethod.WbdkName;
            _returnDataType = fileMethod.ReturnDataType;
            _arguments = fileMethod.Arguments.Select(x => new ProjectArgument(x)).ToArray();
            _privacy = fileMethod.Privacy;
            _static = fileMethod.Static;
            _fileContext = fileMethod.FileContext;
            _span = fileMethod.DefinitionSpan;
            _accessType = fileMethod.AccessType;
        }

        private ProjectMethod(ProjectClass class_, string name, string wbdkName, DataType returnDataType, Privacy privacy,
            bool static_, FileContext fileContext, Span span, MethodAccessType accessType, ProjectArgument[] args)
        {
            _class = class_;
            _name = name;
            _wbdkName = wbdkName;
            _returnDataType = returnDataType;
            _privacy = privacy;
            _static = static_;
            _fileContext = fileContext;
            _span = span;
            _accessType = accessType;
            _arguments = args;
        }

        public MethodAccessType AccessType => _accessType;
        public IArgument[] Arguments => _arguments;
        public ProjectClass Class => _class;
        IClass IMethod.Class => _class;
        public Span DefinitionSpan => _span;
        public FileContext FileContext => _fileContext;
        public string Name => _name;
        public Privacy Privacy => _privacy;
        public DataType ReturnDataType => _returnDataType;
        public bool Static => _static;
        public string WbdkName => _wbdkName;

        public BsonObject ToBson(BsonFile bson)
        {
            var obj = new BsonObject(bson);

            obj.SetString("Name", _name);
            obj.SetString("WbdkName", _wbdkName);
            obj.SetDataType("ReturnDataType", _returnDataType);
            obj.SetEnum("Privacy", _privacy);
            obj.SetBoolean("Static", _static);
            obj.SetEnum("FileContext", _fileContext);
            obj.SetSpan("DefinitionSpan", _span);
            obj.SetEnum("AccessType", _accessType);
            obj.SetArray("Arguments", _arguments.Select(x => x.ToBson(bson)));

            return obj;
        }

        public static ProjectMethod FromBson(ProjectClass class_, BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonDataException("Method node is not an object.");

            var name = obj.GetString("Name");
            var wbdkName = obj.GetString("WbdkName");
            var returnDataType = obj.GetDataType("ReturnDataType");
            var privacy = obj.GetEnum<Privacy>("Privacy");
            var static_ = obj.GetBoolean("Static");
            var fileContext = obj.GetEnum<FileContext>("FileContext");
            var span = obj.GetSpan("DefinitionSpan");
            var accessType = obj.GetEnum<MethodAccessType>("AccessType");
            var args = obj.GetArray("Arguments").Select(x => ProjectArgument.FromBson(x)).ToArray();

            return new ProjectMethod(class_, name, wbdkName, returnDataType, privacy, static_, fileContext, span, accessType, args);
        }
    }
}
