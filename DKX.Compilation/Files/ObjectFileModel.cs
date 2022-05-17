using DK.Code;
using DKX.Compilation.Variables;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DKX.Compilation.Files
{
    public class ObjectFileModel
    {
        [TestApproach(TestApproach.Normal, IgnoreCase = true)]
        public string SourcePathName { get; set; }

        public string ClassName { get; set; }

        public ObjectFileDependency[] FileDependencies { get; set; }

        public ObjectTableDependency[] TableDependencies { get; set; }

        public ObjectMethod[] Methods { get; set; }

        public ObjectProperty[] Properties { get; set; }

        public ObjectMemberVariable[] MemberVariables { get; set; }

        public ObjectConstant[] Constants { get; set; }
    }

    public class ObjectMethod
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Privacy Privacy { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FileContext FileContext { get; set; }

        public string ReturnDataType { get; set; }

        public ObjectMethodArgument[] Arguments { get; set; }

        public ObjectBody Body { get; set; }
    }

    public class ObjectProperty
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public ObjectPropertyAccessor[] Getters { get; set; }

        public ObjectPropertyAccessor[] Setters { get; set; }
    }

    public class ObjectPropertyAccessor
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Privacy Privacy { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FileContext FileContext { get; set; }

        public ObjectBody Body { get; set; }
    }

    public class ObjectMethodArgument
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ArgumentPassType PassType { get; set; }
    }

    public class ObjectMemberVariable
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FileContext FileContext { get; set; }

        public string DataType { get; set; }
    }

    public class ObjectConstant
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public string Code { get; set; }

        public int CodeStartPosition { get; set; }
    }

    public class ObjectBody
    {
        public ObjectVariable[] Variables { get; set; }

        [TestApproach(TestApproach.OpCodeValidator)]
        public string Code { get; set; }

        public int StartPosition { get; set; }
    }

    public class ObjectVariable
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public string InitializerCode { get; set; }
    }

    public class ObjectFileDependency
    {
        public string PathName { get; set; }

        public static readonly ObjectFileDependency[] EmptyArray = new ObjectFileDependency[0];
    }

    public class ObjectTableDependency
    {
        public string TableName { get; set; }

        public string Hash { get; set; }

        public static readonly ObjectTableDependency[] EmptyArray = new ObjectTableDependency[0];
    }
}
