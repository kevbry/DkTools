using DK.AppEnvironment;
using DK.Implementation.Virtual;
using DK.Repository;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DKX.Compilation.Tests
{
    class CompileTestClass
    {
        protected DkAppContext CreateAppContext()
        {
            var fs = new VirtualFileSystem();
            var log = new TestLogger();
            var config = new TestAppConfigSource();

            var app = new DkAppContext(fs, log, config, new NoAppRepoFactory());
            SetupCompileFiles(app);
            app.LoadAppSettings();
            return app;
        }

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

        private void SetupCompileFiles(DkAppContext app)
        {
            app.FileSystem.CreateDirectory(@"x:\bin");
            app.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            app.FileSystem.CreateDirectory(@"x:\platform");
            app.FileSystem.CreateDirectory(@"x:\platform\include");
            app.FileSystem.CreateDirectory(@"x:\src");
            app.FileSystem.CreateDirectory(@"x:\src\gateway");
            app.FileSystem.CreateDirectory(@"x:\src\include");
            app.FileSystem.CreateDirectory(@"x:\src\lib");
            app.FileSystem.CreateDirectory(@"x:\src\obj");
            app.FileSystem.CreateDirectory(@"x:\src\tmp");

            // DK source
            SetupFile(app, @"x:\platform\include\stdlib.i", "stdlib.i.txt");
            SetupFile(app, @"x:\src\dict", "dict.txt");
            SetupFile(app, @"x:\src\age.f", "age.f.txt");
            SetupFile(app, @"x:\src\trim.f", "trim.f.txt");
            SetupFile(app, @"x:\src\util.nc", "util.nc.txt");
            SetupFile(app, @"x:\src\gateway\gateway.cc", "gateway.cc.txt");
            SetupFile(app, @"x:\src\include\all.i", null);

            // DKX source
            SetupFile(app, @"x:\src\cust.dkx", "cust.dkx.txt");
            SetupFile(app, @"x:\src\info.dkx", "info.dkx.txt");
            SetupFile(app, @"x:\src\test.dkx", "test.dkx.txt");
            SetupFile(app, @"x:\src\util.dkx", "util.dkx.txt");
        }

        private void SetupFile(DkAppContext app, string pathName, string testFileName)
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);
            var exeDir = System.IO.Path.GetDirectoryName(uri.AbsolutePath);
            var content = testFileName != null ? System.IO.File.ReadAllText($"{exeDir}\\TestSource\\{testFileName}") : string.Empty;
            app.FileSystem.WriteFileText(pathName, content);
        }
    }
}
