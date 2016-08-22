using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Mvvm;

namespace Plainion.CI.ViewModels
{
    [Export]
    class BuildLogViewModel : BindableBase
    {
        private bool? mySucceeded;

        public BuildLogViewModel()
        {
            Log = new ObservableCollection<string>();
        }

        public ObservableCollection<string> Log { get; private set; }

        public bool? Succeeded
        {
            get { return mySucceeded; }
            set { SetProperty(ref mySucceeded, value); }
        }
    }
}
