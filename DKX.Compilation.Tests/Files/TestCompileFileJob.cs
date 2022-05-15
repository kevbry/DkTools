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
        private string _objPathName;

        public TestCompileFileJob(string dkxPathName, string objPathName)
        {
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
        }

        public string Description => $"Compile File: {_dkxPathName}";
        public string DkxPathName => _dkxPathName;
        public string ObjectPathName => _objPathName;

        public Task ExecuteAsync(CancellationToken cancel)
        {
            return Task.CompletedTask;
        }
    }
}
