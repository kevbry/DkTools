using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Jobs
{
    public interface ICompileJob
    {
        string Description { get; }

        Task ExecuteAsync(CancellationToken cancel);
    }
}
