using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Plainion.CI.Services.SourceControl
{
    class PendingChangesObserver
    {
        private ISourceControl mySourceControl;
        private Action<IEnumerable<Change>> myOnPendingChangesChanged;
        private FileSystemWatcher myPendingChangesWatcher;
        private string myWorkspaceRoot;
        private Task myWorkspaceReaderTask;
        private bool myWorkspaceChanged;
        private HashSet<Change> myCurrentPendingChanges;

        public PendingChangesObserver(ISourceControl sourceControl, Action<IEnumerable<Change>> onPendingChangesChanged)
        {
            Contract.RequiresNotNull(sourceControl, "sourceControl");
            Contract.RequiresNotNull(onPendingChangesChanged, "onPendingChangesChanged");

            mySourceControl = sourceControl;
            myOnPendingChangesChanged = onPendingChangesChanged;
            myCurrentPendingChanges = new HashSet<Change>();
        }

        public void Start(string workspaceRoot)
        {
            Contract.Invariant(myPendingChangesWatcher == null, "Pending changes watcher still running");

            myWorkspaceRoot = workspaceRoot;

            myPendingChangesWatcher = new FileSystemWatcher();

            myPendingChangesWatcher.Path = workspaceRoot;
            myPendingChangesWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName;
            myPendingChangesWatcher.Filter = "*";
            myPendingChangesWatcher.IncludeSubdirectories = true;

            myPendingChangesWatcher.Created += OnChanged;
            myPendingChangesWatcher.Changed += OnChanged;
            myPendingChangesWatcher.Deleted += OnChanged;
            myPendingChangesWatcher.Renamed += OnChanged;

            myPendingChangesWatcher.EnableRaisingEvents = true;

            // run initial analysis
            OnChanged(null, null);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Debug.WriteLine("Workspace change detected");

            if (myWorkspaceReaderTask != null)
            {
                myWorkspaceChanged = true;
                return;
            }

            myWorkspaceChanged = false;

            myWorkspaceReaderTask = mySourceControl.GetPendingChangesAsync(myWorkspaceRoot)
                .ContinueWith(t =>
                   {
                       // intentionally we ignore all exceptions here because if in parallel a checkin or push is running
                       // we can easily run into race-conditions
                       if (t.IsFaulted)
                       {
                           myWorkspaceChanged = true;
                       }
                       else
                       {
                           Application.Current.Dispatcher.BeginInvoke(new Action(() => PropagatePendingChnages(t.Result)));
                       }
                   })
                .ContinueWith(t =>
                   {
                       myWorkspaceReaderTask = null;

                       if (myWorkspaceChanged)
                       {
                           OnChanged(null, null);
                       }
                   });
        }

        private void PropagatePendingChnages(IReadOnlyCollection<Change> changes)
        {
            if (myCurrentPendingChanges.SetEquals(changes))
            {
                return;
            }

            Debug.WriteLine("PropagatePendingChnages");

            myCurrentPendingChanges = new HashSet<Change>(changes);

            myOnPendingChangesChanged(myCurrentPendingChanges);
        }

        public void Stop()
        {
            if (myPendingChangesWatcher != null)
            {
                myPendingChangesWatcher.Dispose();
            }
            myWorkspaceReaderTask = null;
        }
    }
}
