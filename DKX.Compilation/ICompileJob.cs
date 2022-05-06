using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation
{
    interface ICompileJob
    {
        string Description { get; }

        Task ExecuteAsync(CancellationToken cancel);
    }
}
