using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.WbdkExports
{
    class WbdkExportsModel
    {
        public string SourceFile { get; set; }
        public DateTime TimeStamp { get; set; }
        public WbdkExport[] Exports { get; set; }
        public string[] DependentFiles { get; set; }
    }

    class WbdkExport
    {
        public string ClassName { get; set; }
        public string Name { get; set; }
        public string DkSignature { get; set; }
    }
}
