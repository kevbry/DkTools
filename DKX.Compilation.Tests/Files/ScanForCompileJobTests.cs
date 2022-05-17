using DK;
using DK.Code;
using DK.Implementation.Virtual;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;
using DKX.Compilation.Tests.Schema;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    [TestFixture]
    class ScanForCompileJobTests : CompileTestClass
    {
        [Test]
        public async Task FirstCompile()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var fs = app.FileSystem as VirtualFileSystem;
            var jobQueue = new TestJobQueue();
            var compileFileJobFactory = new TestCompileFileJobFactory();
            var objectFileReaderFactory = new TestObjectFileReaderFactory();
            var tableHashProvider = new TestTableHashProvider();
            var scanJob = new ScanForCompileJob(
                app: app,
                workDir: @"x:\bin\.dkx",
                compileQueue: jobQueue,
                compileFileJobFactory: compileFileJobFactory,
                objectFileReaderFactory: objectFileReaderFactory,
                tableHashProvider: tableHashProvider);

            await scanJob.ExecuteAsync(cancel: default);

            Assert.AreEqual(4, jobQueue.Jobs.Count);
            var job = jobQueue.Jobs.Cast<TestCompileFileJob>().Where(x => x.DkxPathName.EqualsI(@"x:\src\cust.dkx")).FirstOrDefault();
            ValidateFileJob(job, @"x:\src\cust.dkx", @"x:\bin\.dkx\cust.dkxx");
            jobQueue.Jobs.Remove(job);

            job = jobQueue.Jobs.Cast<TestCompileFileJob>().Where(x => x.DkxPathName.EqualsI(@"x:\src\info.dkx")).FirstOrDefault();
            ValidateFileJob(job, @"x:\src\info.dkx", @"x:\bin\.dkx\info.dkxx");
            jobQueue.Jobs.Remove(job);

            job = jobQueue.Jobs.Cast<TestCompileFileJob>().Where(x => x.DkxPathName.EqualsI(@"x:\src\test.dkx")).FirstOrDefault();
            ValidateFileJob(job, @"x:\src\test.dkx", @"x:\bin\.dkx\test.dkxx");
            jobQueue.Jobs.Remove(job);

            job = jobQueue.Jobs.Cast<TestCompileFileJob>().Where(x => x.DkxPathName.EqualsI(@"x:\src\util.dkx")).FirstOrDefault();
            ValidateFileJob(job, @"x:\src\util.dkx", @"x:\bin\.dkx\util.dkxx");
            jobQueue.Jobs.Remove(job);

            Assert.AreEqual(0, jobQueue.Jobs.Count);
        }

        [Test]
        public async Task ChangedFile()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var fs = app.FileSystem as VirtualFileSystem;
            var jobQueue = new TestJobQueue();
            var compileFileJobFactory = new TestCompileFileJobFactory();
            var objectFileReaderFactory = new TestObjectFileReaderFactory();
            var tableHashProvider = new TestTableHashProvider();
            var scanJob = new ScanForCompileJob(
                app: app,
                workDir: @"x:\bin\.dkx",
                compileQueue: jobQueue,
                compileFileJobFactory: compileFileJobFactory,
                objectFileReaderFactory: objectFileReaderFactory,
                tableHashProvider: tableHashProvider);

            await scanJob.ExecuteAsync(cancel: default);

            // Create the object files so it looks like the compile succeeded.
            foreach (var job in jobQueue.Jobs.Cast<TestCompileFileJob>())
            {
                fs.WriteFileText(job.ObjectPathName, string.Empty);
            }

            // Pick the files we're going to touch
            fs.DateOffset = TimeSpan.FromMinutes(1);
            var index = 0;
            var touchedFiles = new List<TestCompileFileJob>();
            foreach (var job in jobQueue.Jobs.Cast<TestCompileFileJob>())
            {
                if ((index++) % 2 == 0) continue;
                fs.TouchFile(job.DkxPathName);
                touchedFiles.Add(job);
            }

            // Re-run the scan job
            jobQueue.Jobs.Clear();

            scanJob = new ScanForCompileJob(
                app: app,
                workDir: @"x:\bin\.dkx",
                compileQueue: jobQueue,
                compileFileJobFactory: compileFileJobFactory,
                objectFileReaderFactory: objectFileReaderFactory,
                tableHashProvider: tableHashProvider);
            await scanJob.ExecuteAsync(cancel: default);

            // Validate that only the touched files were compiled.
            Assert.AreEqual(touchedFiles.Count, jobQueue.Jobs.Count);
            foreach (var file in touchedFiles)
            {
                var job = jobQueue.Jobs.Cast<TestCompileFileJob>().Where(x => x.DkxPathName.EqualsI(file.DkxPathName)).FirstOrDefault();
                ValidateFileJob(job, file.DkxPathName, file.ObjectPathName);
                jobQueue.Jobs.Remove(job);
            }
        }

        [Test]
        public async Task DeleteFile()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var fs = app.FileSystem as VirtualFileSystem;
            var jobQueue = new TestJobQueue();
            var compileFileJobFactory = new TestCompileFileJobFactory();
            var objectFileReaderFactory = new TestObjectFileReaderFactory();
            var tableHashProvider = new TestTableHashProvider();
            var scanJob = new ScanForCompileJob(
                app: app,
                workDir: @"x:\bin\.dkx",
                compileQueue: jobQueue,
                compileFileJobFactory: compileFileJobFactory,
                objectFileReaderFactory: objectFileReaderFactory,
                tableHashProvider: tableHashProvider);

            await scanJob.ExecuteAsync(cancel: default);

            // Create the object files so it looks like the compile succeeded.
            foreach (var job in jobQueue.Jobs.Cast<TestCompileFileJob>())
            {
                fs.WriteFileText(job.ObjectPathName, string.Empty);
            }

            // Pick the files we're going to delete
            fs.DateOffset = TimeSpan.FromMinutes(1);
            var index = 0;
            var touchedFiles = new List<TestCompileFileJob>();
            var untouchedFiles = new List<TestCompileFileJob>();
            foreach (var job in jobQueue.Jobs.Cast<TestCompileFileJob>())
            {
                if ((index++) % 2 != 0)
                {
                    untouchedFiles.Add(job);
                }
                else
                {
                    fs.DeleteFile(job.DkxPathName);
                    touchedFiles.Add(job);
                }
            }

            // Re-run the scan job
            jobQueue.Jobs.Clear();

            scanJob = new ScanForCompileJob(
                app: app,
                workDir: @"x:\bin\.dkx",
                compileQueue: jobQueue,
                compileFileJobFactory: compileFileJobFactory,
                objectFileReaderFactory: objectFileReaderFactory,
                tableHashProvider: tableHashProvider);
            await scanJob.ExecuteAsync(cancel: default);

            // Validate that only the touched files were deleted.
            foreach (var file in touchedFiles)
            {
                Assert.IsFalse(fs.FileExists(file.ObjectPathName));
            }

            foreach (var file in untouchedFiles)
            {
                Assert.IsTrue(fs.FileExists(file.ObjectPathName));
                Assert.IsTrue(fs.FileExists(file.DkxPathName));
            }
        }

        private void ValidateFileJob(TestCompileFileJob job, string dkxPathName, string objPathName)
        {
            Assert.IsNotNull(job);
            Assert.AreEqual(dkxPathName.ToLower(), job.DkxPathName.ToLower());
            Assert.AreEqual(objPathName.ToLower(), job.ObjectPathName.ToLower());
        }
    }
}
