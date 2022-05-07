using System.Threading.Tasks;

namespace DKX.Compilation.Jobs
{
    public interface ICompileJobQueue
    {
        Task EnqueueCompileJobAsync(ICompileJob job);
    }
}
