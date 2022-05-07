using DK;
using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.WbdkExports;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    [TestFixture]
    class ScanWbdkExportsJobTests : CompileTestClass
    {
        [Test]
        public async Task AllExportsGenerated()
        {
            SetupCompileFiles();

            var jobQueue = new TestJobQueue();

            App.FileSystem.CreateDirectory(@"x:\bin\.dkx");
            var job = new ScanWbdkExportsJob(App, jobQueue, @"x:\bin\.dkx");

            await job.ExecuteAsync(cancel: default);

            var applicablePathNames = new List<string>();
            foreach (var pathName in App.FileSystem.GetFilesInDirectory(@"x:\\src"))
            {
                if (pathName.EndsWith(".f", StringComparison.OrdinalIgnoreCase)) applicablePathNames.Add(pathName);
            }

            Assert.AreEqual(applicablePathNames.Count, jobQueue.Jobs.Count);

            foreach (var pathName in applicablePathNames)
            {
                var scanJob = jobQueue.Jobs.Cast<ScanWbdkExportFileJob>().Where(x => x.PathName.EqualsI(pathName)).FirstOrDefault();
                Assert.IsNotNull(scanJob);

                var name = PathUtil.GetFileName(pathName);
                Assert.AreEqual($"x:\\src\\{name}", scanJob.PathName.ToLower());
                Assert.AreEqual($"x:\\bin\\.dkx\\{name}.exports.json", scanJob.ExportsPathName.ToLower());
                Assert.AreEqual(FileContext.Function, scanJob.FileContext);
            }
        }

        // TODO: Classes
        // TODO: Files in deeper directories
        // TODO: Excludes untouched files
        // TODO: Include dependencies
    }
}
