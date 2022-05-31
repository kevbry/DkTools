using DKX.Compilation.Jobs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    class TestCompileFileJob : ICompileJob
    {
        private string _dkxPathName;

        public TestCompileFileJob(string dkxPathName)
        {
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
        }

        public string Description => $"Compile File: {_dkxPathName}";
        public string DkxPathName => _dkxPathName;

        public Task ExecuteAsync(CancellationToken cancel)
        {
            return Task.CompletedTask;
        }
    }
}
