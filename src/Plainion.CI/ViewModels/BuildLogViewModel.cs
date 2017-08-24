using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows;
using Plainion.Windows.Mvvm;

namespace Plainion.CI.ViewModels
{
    [Export]
    class BuildLogViewModel : BindableBase
    {
        private StringBuilder myLog;
        private bool? mySucceeded;
        private bool myIsRefreshPending;

        public BuildLogViewModel()
        {
            myLog = new StringBuilder();
        }

        public string Log
        {
            get { return myLog.ToString(); }
        }

        public void Append(string line)
        {
            myLog.AppendLine(line);

            if (myIsRefreshPending)
            {
                return;
            }

            myIsRefreshPending = true;

            // keep UI responsive even if there is a lot of text logged
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnPropertyChanged(nameof(Log));
                myIsRefreshPending = false;
            }));
        }

        public void Clear()
        {
            myLog.Clear();
        }

        public bool? Succeeded
        {
            get { return mySucceeded; }
            set { SetProperty(ref mySucceeded, value); }
        }
    }
}
