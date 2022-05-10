using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation
{
    internal class CompileQueue : ICompileJobQueue
    {
        private DkAppContext _app;
        private string _name;
        private Queue<ICompileJob> _pendingJobs = new Queue<ICompileJob>();
        private List<Task> _runningJobs = new List<Task>();
        private SemLock _jobLock = new SemLock();
        private int _maxRunningJobs = Environment.ProcessorCount;
        private List<ReportItem> _reportItems = new List<ReportItem>();
        private bool _haltErrors = false;

        private const int ProcessQueueIdleSleepTimeMilliseconds = 10;

        public CompileQueue(DkAppContext app, string name)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public async Task ProcessQueueToCompletionAsync(CancellationToken cancel)
        {
            var jobsToRemove = new List<Task>();
            ICompileJob jobToRun;

            try
            {
                _app.Log.Debug("Compile queue '{0}' is starting.", _name);

                while (_haltErrors == false)
                {
                    cancel.ThrowIfCancellationRequested();

                    jobToRun = null;

                    await _jobLock.WaitAsync();
                    try
                    {
                        // Clear completed jobs
                        jobsToRemove.Clear();
                        foreach (var job in _runningJobs)
                        {
                            if (job.IsCompleted) jobsToRemove.Add(job);
                        }

                        if (jobsToRemove.Count > 0)
                        {
                            foreach (var job in jobsToRemove) _runningJobs.Remove(job);
                        }

                        // Check if it's time to start new jobs
                        if (_runningJobs.Count < _maxRunningJobs)
                        {
                            if (_pendingJobs.Count > 0)
                            {
                                jobToRun = _pendingJobs.Dequeue();
                            }
                            else if (_runningJobs.Count == 0)
                            {
                                // All work complete
                                _app.Log.Debug("All jobs complete in queue '{0}'.", _name);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        _jobLock.Release();
                    }

                    if (jobToRun != null)
                    {
                        Task task;
                        try
                        {
                            task = jobToRun.ExecuteAsync(cancel);

                            await _jobLock.LockAsync();
                            try
                            {
                                _runningJobs.Add(task);
                            }
                            finally
                            {
                                _jobLock.Release();
                            }
                        }
                        catch (Exception ex)
                        {
                            _reportItems.Add(new ReportItem(null, -1, -1, -1, -1, ErrorCode.DKX0001_CompileJobFailed, jobToRun.Description, ex));
                            _haltErrors = true;
                        }
                    }
                    else
                    {
                        // Nothing else to do except wait.
                        await Task.Delay(ProcessQueueIdleSleepTimeMilliseconds);
                    }
                }

                _app.Log.Debug("Compile queue '{0}' has finished normally.", _name);
            }
            catch (OperationCanceledException)
            {
                _app.Log.Debug("Compile queue '{0}' has been cancelled.", _name);
            }
            catch (Exception ex)
            {
                _app.Log.Error(ex, "Fatal error in compile queue '{0}'.", _name);
            }
        }

        public async Task EnqueueCompileJobAsync(ICompileJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            await _jobLock.WaitAsync();
            try
            {
                _pendingJobs.Enqueue(job);
            }
            finally
            {
                _jobLock.Release();
            }
        }

        public void AddReport(ReportItem reportItem)
        {
            _reportItems.Add(reportItem);
            if (reportItem.Severity == ErrorSeverity.Error) _haltErrors = true;
        }

        public void AddReports(IEnumerable<ReportItem> reportItems)
        {
            foreach (var item in reportItems)
            {
                if (item.Severity == ErrorSeverity.Error) _haltErrors = true;
                _reportItems.Add(item);
            }
        }

        public bool HasErrors => _haltErrors;

        public bool HasReportItems => _reportItems.Count > 0;

        public IEnumerable<ReportItem> ReportItems => _reportItems;
    }
}
