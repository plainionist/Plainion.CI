using System.ComponentModel.Composition;
using System.Text;
using Plainion.Windows.Mvvm;

namespace Plainion.CI.ViewModels
{
    [Export]
    class BuildLogViewModel : BindableBase
    {
        private StringBuilder myLog;
        private bool? mySucceeded;

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
            OnPropertyChanged(nameof(Log));
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
