using DK.AppEnvironment;

namespace DK.Repository
{
    public class AppRepoFactory : IAppRepoFactory
    {
        public IAppRepo CreateAppRepo(DkAppSettings appSettings) => new AppRepo(appSettings);
    }
}
