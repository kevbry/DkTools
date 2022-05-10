using DKX.Compilation.Variables;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace DKX.Compilation.Files
{
    public class ObjectFileModel
    {
        public string SourcePathName { get; set; }

        public string DestinationPathName { get; set; }

        public string ClassName { get; set; }

        public ObjectFileDependency[] FileDependencies { get; set; }

        public ObjectTableDependency[] TableDependencies { get; set; }

        public ObjectMethod[] Methods { get; set; }

        public ObjectProperty[] Properties { get; set; }
    }

    public class ObjectMethod
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Privacy Privacy { get; set; }

        public string ReturnDataType { get; set; }

        public ObjectMethodArgument[] Arguments { get; set; }
    }

    public class ObjectProperty
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Privacy Privacy { get; set; }

        public string DataType { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class ObjectMethodArgument
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ArgumentPassType PassType { get; set; }
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

    class InvalidObjectFileException : Exception
    {
        public InvalidObjectFileException(string pathName) : base($"Object file '{pathName}' does not have a correct format.") { }
    }
}
