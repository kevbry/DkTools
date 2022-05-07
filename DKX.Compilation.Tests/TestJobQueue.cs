using DKX.Compilation.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    class TestJobQueue : ICompileJobQueue
    {
        public List<ICompileJob> Jobs { get; set; } = new List<ICompileJob>();

        public Task EnqueueCompileJobAsync(ICompileJob job)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }
    }
}
