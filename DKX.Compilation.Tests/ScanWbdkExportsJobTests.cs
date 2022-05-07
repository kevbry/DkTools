using DK;
using DK.AppEnvironment;
using DK.Code;
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
            SetupCompileFiles();

            var jobQueue = new TestJobQueue();

            FS.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(App, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            TestContext.Out.WriteLine("Applicable files:");
            var applicablePathNames = new List<string>();
            foreach (var pathName in FS.GetFilesInDirectoryRecursive(@"x:\src"))
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

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports.json", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);
            }
        }

        [Test]
        public async Task OnlyModifiedFilesPickedUp()
        {
            FS.DateOffset = TimeSpan.FromMinutes(-60);
            SetupCompileFiles();

            var jobQueue = new TestJobQueue();

            // Run once to update the files
            FS.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(App, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            // Pick the files we're going to touch
            TestContext.Out.WriteLine("Touched files:");
            var applicablePathNames = new List<string>();
            var touchedPathNames = new List<string>();
            var index = 0;
            foreach (var pathName in FS.GetFilesInDirectoryRecursive(@"x:\src"))
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

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports.json", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);

                FS.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                FS.WriteFileText(scanJob.ExportsPathName, string.Empty);
            }

            // Touch the files we want to pick up.
            FS.DateOffset = TimeSpan.Zero;
            foreach (var pathName in touchedPathNames) FS.TouchFile(pathName);

            // Run again now that select files have been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(App, jobQueue, @"x:\bin\.dkx", new TestExportsFileReaderFactory());
            await job.ExecuteAsync(cancel: default);

            Assert.AreEqual(touchedPathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in touchedPathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports.json", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);
            }
        }

        [Test]
        public async Task IncludeDependencies()
        {
            FS.DateOffset = TimeSpan.FromMinutes(-60);
            SetupCompileFiles();

            var jobQueue = new TestJobQueue();
            var exportsReaderFactory = new TestExportsFileReaderFactory();

            // Run once to update the files
            FS.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(App, jobQueue, @"x:\bin\.dkx", exportsReaderFactory);
            await job.ExecuteAsync(cancel: default);

            // Pick the files we're going to touch
            TestContext.Out.WriteLine("Touched files:");
            var applicablePathNames = new List<string>();
            var touchedPathNames = new List<string>();
            var index = 0;
            foreach (var pathName in FS.GetFilesInDirectoryRecursive(@"x:\src"))
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

            foreach (var pathName in applicablePathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports.json", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);

                FS.CreateDirectoryRecursive(PathUtil.GetDirectoryName(scanJob.ExportsPathName));
                FS.WriteFileText(scanJob.ExportsPathName, string.Empty);

                if (touchedPathNames.Any(x => x.EqualsI(pathName)))
                {
                    exportsReaderFactory.SetIncludeDependencies(scanJob.ExportsPathName, new string[] { @"x:\src\include\all.i" });
                }
            }

            // Touch the include files which will trigger the other exports to be rebuilt.
            FS.DateOffset = TimeSpan.Zero;
            FS.TouchFile(@"x:\src\include\all.i");

            // Run again now that the include file has been touched.
            jobQueue.Jobs.Clear();
            job = new ScanWbdkExportsJob(App, jobQueue, @"x:\bin\.dkx", exportsReaderFactory);
            await job.ExecuteAsync(cancel: default);

            Assert.AreEqual(touchedPathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in touchedPathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                Assert.IsTrue(pathName.StartsWith(@"x:\src\", StringComparison.OrdinalIgnoreCase));
                var relPathName = pathName.Substring(@"x:\src\".Length);

                Assert.AreEqual($"x:\\bin\\.dkx\\{relPathName}.exports.json", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContextHelper.GetFileContextFromFileName(pathName), scanJob.FileContext);
            }
        }
    }
}
