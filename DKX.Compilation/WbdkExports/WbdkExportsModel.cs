using System;

namespace DKX.Compilation.WbdkExports
{
    public class WbdkExportsModel
    {
        public string SourceFile { get; set; }
        public DateTime TimeStamp { get; set; }
        public WbdkExport[] Exports { get; set; }
        public string[] DependentFiles { get; set; }
        public WbdkExportTableDependency[] TableDependencies { get; set; }
    }

    public class WbdkExport
    {
        public string ClassName { get; set; }
        public string Name { get; set; }
        public WbdkExportArgument[] Arguments { get; set; }
        public string ReturnDataType { get; set; }
    }

    public class WbdkExportArgument
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool Ref { get; set; }
        public bool Out { get; set; }
    }

    public class WbdkExportTableDependency
    {
        public static readonly WbdkExportTableDependency[] EmptyArray = new WbdkExportTableDependency[0];

        public string TableName { get; set; }
        public string Hash { get; set; }
    }
}
