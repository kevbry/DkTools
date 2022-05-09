using DK.Code;
using DKX.Compilation.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    class TestCompileFileJob : ICompileJob
    {
        private string _dkxPathName;
        private string _wbdkPathName;
        private string _objPathName;
        private FileContext _fileContext;

        public TestCompileFileJob(string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _wbdkPathName = wbdkPathName ?? throw new ArgumentNullException(nameof(wbdkPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _fileContext = fileContext;
        }

        public string Description => $"Compile File: {_dkxPathName}";
        public string DkxPathName => _dkxPathName;
        public FileContext FileContext => _fileContext;
        public string ObjectPathName => _objPathName;
        public string WbdkPathName => _wbdkPathName;

        public Task ExecuteAsync(CancellationToken cancel)
        {
            return Task.CompletedTask;
        }
    }
}
