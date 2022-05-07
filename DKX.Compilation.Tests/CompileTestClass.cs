using DK.AppEnvironment;
using DK.Implementation.Virtual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    class CompileTestClass
    {
        private DkAppContext _app;
        private VirtualFileSystem _fs;
        private TestLogger _log;
        private TestAppConfigSource _config;

        public CompileTestClass()
        {
            _fs = new VirtualFileSystem();
            _log = new TestLogger();
            _config = new TestAppConfigSource();

            _app = new DkAppContext(_fs, _log, _config);
            _app.LoadAppSettings();
        }

        protected DkAppContext App => _app;

        class TestAppConfigSource : IAppConfigSource
        {
            public string GetDefaultAppName() => "CoreBanking";

            public IEnumerable<string> GetAllAppNames() => new string[] { "CoreBanking" };

            public string GetAppPath(string appName, WbdkAppPath path)
            {
                switch (path)
                {
                    case WbdkAppPath.SourcePaths: return @"x:\src";
                    case WbdkAppPath.IncludePaths: return @"x:\src\include;x:\platform\include";
                    case WbdkAppPath.LibPaths: return @"x:\src\lib";
                    case WbdkAppPath.ExecutablePaths: return @"x:\bin";
                    case WbdkAppPath.ObjectPath: return @"x:\obj";
                    case WbdkAppPath.DiagPath: return @"x:\tmp";
                    case WbdkAppPath.ListingPath: return @"x:\tmp";
                    case WbdkAppPath.DataPath: return @"x:\tmp";
                    case WbdkAppPath.LogPath: return @"x:\tmp";
                    default: return null;
                }
            }

            public IEnumerable<string> GetAppPathMulti(string appName, WbdkAppPath path) => GetAppPath(appName, path).Split(';');

            public string GetAppConfig(string appName, WbdkAppConfig config)
            {
                switch (config)
                {
                    case WbdkAppConfig.DB1ServerName:
                    case WbdkAppConfig.DB2ServerName:
                    case WbdkAppConfig.DB3ServerName:
                    case WbdkAppConfig.DB4ServerName:
                        return "localhost";
                    case WbdkAppConfig.DB1SocketNumber:
                    case WbdkAppConfig.DB2SocketNumber:
                    case WbdkAppConfig.DB3SocketNumber:
                    case WbdkAppConfig.DB4SocketNumber:
                        return "5001";
                    default:
                        return null;
                }
            }

            public bool TryUpdateDefaultApp(string appName) => false;

            public WbdkPlatformInfo GetWbdkPlatformInfo() => new WbdkPlatformInfo
            {
                PlatformPath = @"x:\platform",
                Version = new Version(10, 0),
                VersionText = "10.0"
            };

            public Version PlatformVersion => new Version(10, 0);

            public int DefaultSamPort => 5001;
        }

        public void SetupCompileFiles()
        {
            _fs.CreateDirectory(@"x:\src");
            _fs.CreateDirectory(@"x:\src\include");
            _fs.CreateDirectory(@"x:\src\lib");
            _fs.CreateDirectory(@"x:\src\bin");
            _fs.CreateDirectory(@"x:\src\obj");
            _fs.CreateDirectory(@"x:\src\tmp");
            _fs.CreateDirectory(@"x:\platform");
            _fs.CreateDirectory(@"x:\platform\include");

            SetupFile(@"x:\platform\include\stdlib.i", "stdlib.i.txt");
            SetupFile(@"x:\src\dict", "dict.txt");
            SetupFile(@"x:\src\age.f", "age.f.txt");
        }

        private void SetupFile(string pathName, string testFileName)
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);
            var exeDir = System.IO.Path.GetDirectoryName(uri.AbsolutePath);
            var content = System.IO.File.ReadAllText($"{exeDir}\\TestSource\\{testFileName}");
            _fs.WriteFileText(pathName, content);
        }
    }
}
