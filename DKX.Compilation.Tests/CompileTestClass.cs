using DK;
using DK.AppEnvironment;
using DK.Implementation.Virtual;
using DK.Repository;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    class CompileTestClass
    {
        public static string DkxProjectPathName = "X:\\bin\\.dkx\\dkx.dat";

        protected DkAppContext CreateAppContext()
        {
            var app = CreateApp();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            return app;
        }

        protected DkAppContext CreateApp()
        {
            var fs = new VirtualFileSystem();
            var log = new TestLogger();
            var config = new TestAppConfigSource();

            return new DkAppContext(fs, log, config, new NoAppRepoFactory());
        }

        class TestAppConfigSource : IAppConfigSource
        {
            public string GetDefaultAppName() => "CoreBanking";

            public IEnumerable<string> GetAllAppNames() => new string[] { "CoreBanking" };

            public string GetAppPath(string appName, WbdkAppPath path)
            {
                switch (path)
                {
                    case WbdkAppPath.SourcePaths: return @"x:\src;x:\gen\.dkx";
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

        protected void SetupCompile(DkAppContext app)
        {
            app.FileSystem.CreateDirectory(@"x:\bin");
            app.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            app.FileSystem.CreateDirectory(@"x:\gen");
            app.FileSystem.CreateDirectory(@"x:\gen\.dkx");
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
        }

        protected void SetupCompileFiles(DkAppContext app)
        {
            SetupCompile(app);

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

        protected async Task<DkAppContext> SetupCompileSingle(string dkxPathName, string dkxSource, ReportItem? expectedError = null)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            app.FileSystem.WriteFileText(dkxPathName, dkxSource);

            var compiler = new Compiler(app);
            await compiler.CompileAsync(cancel: default);

            if (expectedError.HasValue) TestContext.Out.WriteLine($"Expected Error: {expectedError.ToString()}");

            TestContext.Out.WriteLine($"DKX Source:\n{dkxSource}");

            if (expectedError.HasValue)
            {
                Assert.IsTrue(compiler.HasErrors, "Compiler did not return any errors.");

                var errorText = expectedError.Value.ToString();
                Assert.IsTrue(compiler.ReportItems.Any(x => x.ToString().EqualsI(errorText)), "Compiler returned errors, but not the expected one.");
                return app;
            }
            else
            {
                Assert.IsFalse(compiler.HasErrors, "Compiler returned errors.");
            }

            //await DumpBsonFile(app, "X:\\bin\\.dkx\\dkx.dat");

            return app;
        }

        protected async Task SetCompileFileAsync(DkAppContext app, string dkxPathName, string dkxCode)
        {
            await app.FileSystem.WriteFileTextAsync(dkxPathName, dkxCode);
            TestContext.Out.WriteLine($"DKX Code: {dkxPathName}\n{dkxCode}");
        }

        protected async Task<DkAppContext> RunCompileAsync(DkAppContext app, ReportItem? expectedError = null, ErrorCode? expectedErrorCode = null)
        {
            var compiler = new Compiler(app);
            await compiler.CompileAsync(cancel: default);

            if (expectedError.HasValue) TestContext.Out.WriteLine($"Expected Error: {expectedError.ToString()}");

            if (expectedError.HasValue)
            {
                Assert.IsTrue(compiler.HasErrors, "Compiler did not return any errors.");

                var errorText = expectedError.Value.ToString();
                Assert.IsTrue(compiler.ReportItems.Any(x => x.ToString().EqualsI(errorText)), "Compiler returned errors, but not the expected one.");
                return app;
            }
            else if (expectedErrorCode.HasValue)
            {
                Assert.IsTrue(compiler.HasErrors, "Compiler did not return any errors.");
                Assert.IsTrue(compiler.ReportItems.Any(x => x.Code == expectedErrorCode.Value), "Compiler returned errors, but not the expected one.");
                return app;
            }
            else
            {
                Assert.IsFalse(compiler.HasErrors, "Compiler returned errors.");
            }

            return app;
        }

        protected async Task ValidateOutputAsync(DkAppContext app, string className, string wbdkPathName, string wbdkCode)
        {
            Assert.IsTrue(app.FileSystem.FileExists(wbdkPathName), $"WBDK file '{wbdkPathName}' was not created.");

            var actualWbdkCode = await app.FileSystem.ReadFileTextAsync(wbdkPathName);
            TestContext.Out.WriteLine($"WBDK Code Generated: {wbdkPathName}\n{actualWbdkCode}");

            var header = $"// {className}\n#define _LINK dkx.lib\n#include <dkx.i>\n#warndel 108\n";
            WbdkCodeValidator.Validate(header + wbdkCode, actualWbdkCode);
        }

        protected async Task DumpBsonFileAsync(DkAppContext app, string pathName)
        {
            Assert.IsTrue(app.FileSystem.FileExists(pathName));

            var bsonContent = await app.FileSystem.ReadFileBytesAsync(pathName);
            using (var memStream = new MemoryStream(bsonContent))
            {
                var bson = new BsonFile();
                bson.Read(memStream);
                var json = bson.ToJson(Formatting.Indented);
                TestContext.Out.WriteLine(json);
            }
        }

        protected async Task<string[]> GetFileDependenciesAsync(DkAppContext app, string dkxPathName)
        {
            Assert.IsTrue(app.FileSystem.FileExists(DkxProjectPathName));

            var bsonContent = await app.FileSystem.ReadFileBytesAsync(DkxProjectPathName);
            using (var memStream = new MemoryStream(bsonContent))
            {
                var bson = new BsonFile();
                bson.Read(memStream);
                var bsonFilesArray = bson.Root.GetProperty("Files") as BsonArray;
                foreach (var element in bsonFilesArray.Values)
                {
                    if (element is BsonObject obj)
                    {
                        if (obj.GetProperty("DkxPathName").ToString().EqualsI(dkxPathName))
                        {
                            return (obj.GetProperty("FileDependencies") as BsonArray).Values.Select(x => x.ToString()).ToArray();
                        }
                    }
                }
            }

            return DkxConst.EmptyStringArray;
        }
    }
}
