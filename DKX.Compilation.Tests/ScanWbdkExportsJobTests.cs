using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Implementation.Virtual;
using DKX.Compilation.WbdkExports;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    [TestFixture]
    class ScanWbdkExportsJobTests : CompileTestClass
    {
        private static readonly Regex _hasWbdkExportsRegex = new Regex(@"\.(f|cc|nc|sc)$");

        [Test]
        public async Task AllExportsGenerated()
        {
            var app = CreateAppContext();

            var jobQueue = new TestJobQueue();

            app.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            TestContext.Out.WriteLine("Applicable files:");
            var applicablePathNames = new List<string>();
            foreach (var pathName in app.FileSystem.GetFilesInDirectoryRecursive(@"x:\src"))
            {
                if (_hasWbdkExportsRegex.IsMatch(pathName))
                {
                    TestContext.Out.WriteLine($"- {pathName}");
                    applicablePathNames.Add(pathName);
                }
            }

            Assert.AreEqual(applicablePathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in applicablePathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);
            }
        }

        [Test]
        public async Task OnlyModifiedFilesPickedUp()
        {
            var app = CreateAppContext();
            var fs = app.FileSystem as VirtualFileSystem;

            var jobQueue = new TestJobQueue();

            // Run once to update the files
            app.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            // Pick the files we're going to touch
            TestContext.Out.WriteLine("Touched files:");
            var applicablePathNames = new List<string>();
            var touchedPathNames = new List<string>();
            var index = 0;
            foreach (var pathName in app.FileSystem.GetFilesInDirectoryRecursive(@"x:\src"))
            {
                if (_hasWbdkExportsRegex.IsMatch(pathName))
                {
                    applicablePathNames.Add(pathName);
                    if ((index++ % 2) == 0)
                    {
                        TestContext.Out.WriteLine($"- {pathName}");
                        touchedPathNames.Add(pathName);
                    }
                }
            }

            foreach (var pathName in applicablePathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);

                app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                app.FileSystem.WriteFileText(scanJob.ExportsPathName, string.Empty);
            }

            // Touch the files we want to pick up.
            fs.DateOffset = TimeSpan.FromMinutes(1);
            foreach (var pathName in touchedPathNames) fs.TouchFile(pathName);

            // Run again now that select files have been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            Assert.AreEqual(touchedPathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in touchedPathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);
            }
        }

        [Test]
        public async Task IncludeDependencies()
        {
            var app = CreateAppContext();
            var fs = app.FileSystem as VirtualFileSystem;

            var jobQueue = new TestJobQueue();
            var exportsReaderFactory = new TestExportsFileReaderFactory();

            // Run once to update the files
            app.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", exportsReaderFactory);
            await job.ExecuteAsync(cancel: default);

            // Pick the files we're going to touch
            TestContext.Out.WriteLine("Touched files:");
            var applicablePathNames = new List<string>();
            var touchedPathNames = new List<string>();
            var index = 0;
            foreach (var pathName in app.FileSystem.GetFilesInDirectoryRecursive(@"x:\src"))
            {
                if (_hasWbdkExportsRegex.IsMatch(pathName))
                {
                    applicablePathNames.Add(pathName);
                    if ((index++ % 2) == 1)
                    {
                        TestContext.Out.WriteLine($"- {pathName}");
                        touchedPathNames.Add(pathName);
                    }
                }
            }

            TestContext.Out.WriteLine("Exports with include dependency:");
            foreach (var pathName in applicablePathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();

                app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                app.FileSystem.WriteFileText(scanJob.ExportsPathName, string.Empty);

                if (touchedPathNames.Any(x => x.EqualsI(pathName)))
                {
                    TestContext.Out.WriteLine($"- {scanJob.ExportsPathName}");
                    exportsReaderFactory.SetIncludeDependencies(scanJob.ExportsPathName, new string[] { @"x:\src\include\all.i" });
                }
            }

            // Touch the include files which will trigger the other exports to be rebuilt.
            fs.DateOffset = TimeSpan.FromMinutes(1);
            fs.TouchFile(@"x:\src\include\all.i");

            // Run again now that the include file has been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", exportsReaderFactory);
            await job.ExecuteAsync(cancel: default);

            Assert.AreEqual(touchedPathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in touchedPathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}{CompileConstants.WbdkExportsExtension}", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);
            }
        }

        [TestCase(@"x:\src\age.f", @"x:\bin\.dkx\age.f.exports")]
        [TestCase(@"x:\src\gateway\gateway.cc", @"x:\bin\.dkx\gateway\gateway.cc.exports")]
        public async Task DeleteFile(string sourceFile, string exportFile)
        {
            var app = CreateAppContext();
            var fs = app.FileSystem as VirtualFileSystem;

            var jobQueue = new TestJobQueue();

            fs.CreateDirectory(@"x:\bin\.dkx");

            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);
            foreach (var scanFileJob in jobQueue.Jobs.Cast<ScanWbdkExportFileJob>())
            {
                fs.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanFileJob.ExportsPathName));
                fs.WriteFileText(scanFileJob.ExportsPathName, "");
            }
            fs.DeleteFile(sourceFile);

            Assert.IsTrue(fs.FileExists(exportFile));

            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            Assert.IsFalse(fs.FileExists(exportFile));
        }
    }
}
