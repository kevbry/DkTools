using DK.AppEnvironment;

namespace DKX.Compilation.ReportItems
{
    interface ISourceCodeReporterFactory
    {
        ISourceCodeReporter CreateSourceCodeReporter(DkAppContext app, string sourcePathName);
    }
}
