using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.CodeGeneration
{
    class GenerateCodeJobFactory : IGenerateCodeJobFactory
    {
        private DkAppContext _app;
        private ICompileJobQueue _compileQueue;
        private IObjectFileReaderFactory _objectFileReaderFactory;
        private ISourceCodeReporterFactory _reporterFactory;

        public GenerateCodeJobFactory(
            DkAppContext app,
            ICompileJobQueue compileQueue,
            IObjectFileReaderFactory objectFileReaderFactory,
            ISourceCodeReporterFactory reporterFactory)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _objectFileReaderFactory = objectFileReaderFactory ?? throw new ArgumentNullException(nameof(objectFileReaderFactory));
            _reporterFactory = reporterFactory ?? throw new ArgumentNullException(nameof(reporterFactory));
        }

        public ICompileJob CreateGenerateCodeJob(string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            var reporter = _reporterFactory.CreateSourceCodeReporter(_app, dkxPathName);

            return new GenerateCodeJob(_app, _compileQueue, dkxPathName, wbdkPathName, objPathName, fileContext,
                _objectFileReaderFactory.CreateObjectFileReader(objPathName), reporter);
        }
    }
}
