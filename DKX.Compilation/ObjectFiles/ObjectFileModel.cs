using DK.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DKX.Compilation.ObjectFiles
{
    public class ObjectFileModel
    {
        public ObjectFileDependency[] FileDependencies { get; set; }

        public ObjectTableDependency[] TableDependencies { get; set; }

        public ObjectFileContext[] FileContexts { get; set; }
    }

    public class ObjectFileDependency
    {
        public string PathName { get; set; }
    }

    public class ObjectTableDependency
    {
        public string TableName { get; set; }

        public string Hash { get; set; }
    }

    public class ObjectFileContext
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public FileContext Context { get; set; }

        public static readonly ObjectFileContext[] EmptyArray = new ObjectFileContext[0];
    }
}
