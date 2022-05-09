using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Files
{
    public class ObjectFileModel
    {
        public string SourcePathName { get; set; }
        public string DestinationPathName { get; set; }
        public ObjectFileDependency[] FileDependencies { get; set; }
        public ObjectTableDependency[] TableDependencies { get; set; }
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
