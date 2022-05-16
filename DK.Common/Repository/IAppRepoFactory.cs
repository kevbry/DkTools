using DK.AppEnvironment;

namespace DK.Repository
{
    public interface IAppRepoFactory
    {
        IAppRepo CreateAppRepo(DkAppSettings appSettings);
    }
}
