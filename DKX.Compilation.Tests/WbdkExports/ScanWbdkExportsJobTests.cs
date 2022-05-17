using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Implementation.Virtual;
using DKX.Compilation.Tests.Schema;
using DKX.Compilation.WbdkExports;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.WbdkExports
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
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory(), new TestTableHashProvider());
            await job.ExecuteAsync(cancel: default);

            await TestContext.Out.WriteLineAsync("Applicable files:");
            var applicablePathNames = new List<string>();
            foreach (var pathName in app.FileSystem.GetFilesInDirectoryRecursive(@"x:\src"))
            {
                if (_hasWbdkExportsRegex.IsMatch(pathName))
                {
                    await TestContext.Out.WriteLineAsync($"- {pathName}");
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

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.wbdkx", scanJob.ExportsPathName.ToLower());
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
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory(), new TestTableHashProvider());
            await job.ExecuteAsync(cancel: default);

            // Pick the files we're going to touch
            await TestContext.Out.WriteLineAsync("Touched files:");
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
                        await TestContext.Out.WriteLineAsync($"- {pathName}");
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

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.wbdkx", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);

                app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                app.FileSystem.WriteFileText(scanJob.ExportsPathName, string.Empty);
            }

            // Touch the files we want to pick up.
            fs.DateOffset = TimeSpan.FromMinutes(1);
            foreach (var pathName in touchedPathNames) fs.TouchFile(pathName);

            // Run again now that select files have been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory(), new TestTableHashProvider());
            await job.ExecuteAsync(cancel: default);

            Assert.AreEqual(touchedPathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in touchedPathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.wbdkx", scanJob.ExportsPathName.ToLower());
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
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", exportsReaderFactory, new TestTableHashProvider());
            await job.ExecuteAsync(cancel: default);

            // Pick the files we're going to touch
            await TestContext.Out.WriteLineAsync("Touched files:");
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
                        await TestContext.Out.WriteLineAsync($"- {pathName}");
                        touchedPathNames.Add(pathName);
                    }
                }
            }

            await TestContext.Out.WriteLineAsync("Exports with include dependency:");
            foreach (var pathName in applicablePathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();

                app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                app.FileSystem.WriteFileText(scanJob.ExportsPathName, string.Empty);

                if (touchedPathNames.Any(x => x.EqualsI(pathName)))
                {
                    await TestContext.Out.WriteLineAsync($"- {scanJob.ExportsPathName}");
                    exportsReaderFactory.SetIncludeDependencies(scanJob.ExportsPathName, new string[] { @"x:\src\include\all.i" });
                }
            }

            // Touch the include files which will trigger the other exports to be rebuilt.
            fs.DateOffset = TimeSpan.FromMinutes(1);
            fs.TouchFile(@"x:\src\include\all.i");

            // Run again now that the include file has been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", exportsReaderFactory, new TestTableHashProvider());
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

        [TestCase(@"x:\src\age.f", @"x:\bin\.dkx\age.f.wbdkx")]
        [TestCase(@"x:\src\gateway\gateway.cc", @"x:\bin\.dkx\gateway\gateway.cc.wbdkx")]
        public async Task DeleteFile(string sourceFile, string exportFile)
        {
            var app = CreateAppContext();
            var fs = app.FileSystem as VirtualFileSystem;

            var jobQueue = new TestJobQueue();

            fs.CreateDirectory(@"x:\bin\.dkx");

            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory(), new TestTableHashProvider());
            await job.ExecuteAsync(cancel: default);
            foreach (var scanFileJob in jobQueue.Jobs.Cast<ScanWbdkExportFileJob>())
            {
                fs.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanFileJob.ExportsPathName));
                fs.WriteFileText(scanFileJob.ExportsPathName, "");
            }
            fs.DeleteFile(sourceFile);

            Assert.IsTrue(fs.FileExists(exportFile));

            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory(), new TestTableHashProvider());
            await job.ExecuteAsync(cancel: default);

            Assert.IsFalse(fs.FileExists(exportFile));
        }

        [Test]
        public async Task TableDependencies()
        {
            var app = CreateAppContext();
            var fs = app.FileSystem as VirtualFileSystem;

            var jobQueue = new TestJobQueue();
            var exportsReaderFactory = new TestExportsFileReaderFactory();
            var tableHashProvider = new TestTableHashProvider();
            tableHashProvider.SetTableHash("cust", "hash1");

            // Run once to update the files
            app.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", exportsReaderFactory, tableHashProvider);
            await job.ExecuteAsync(cancel: default);
            var index = 0;
            var dependentFiles = new List<string>();
            foreach (ScanWbdkExportFileJob scanJob in jobQueue.Jobs)
            {
                app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                app.FileSystem.WriteFileText(scanJob.ExportsPathName, "");

                // Pick which files will be dependent on the table change.
                if ((index++) % 2 == 0) continue;
                dependentFiles.Add(scanJob.PathName);
                exportsReaderFactory.SetTableDependency(scanJob.ExportsPathName, "cust", tableHashProvider.GetTableHash("cust"));
            }

            await TestContext.Out.WriteLineAsync("Files to be dependent on table:");
            foreach (var df in dependentFiles) await TestContext.Out.WriteLineAsync($"- {df}");

            // Change the table signature
            tableHashProvider.SetTableHash("cust", "hash2");

            // Run again now that the include file has been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(app, jobQueue, @"x:\bin\.dkx", exportsReaderFactory, tableHashProvider);
            await job.ExecuteAsync(cancel: default);

            // Validate that the correct files were scanned
            Assert.AreEqual(dependentFiles.Count, jobQueue.Jobs.Count);
            foreach (var pathName in dependentFiles)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);
            }
        }
    }
}
