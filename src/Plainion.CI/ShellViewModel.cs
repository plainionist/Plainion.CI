using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using Plainion.CI.Model;
using Plainion.CI.Services;
using Plainion.CI.Services.SourceControl;
using Plainion.CI.ViewModels;
using Plainion.Windows;
using Plainion.Windows.Controls;

namespace Plainion.CI
{
    [Export]
    class ShellViewModel : BindableBase, IPartImportsSatisfiedNotification
    {
        private BuildService myBuildService;
        private int mySelectedTab;
        private bool myIsBusy;

        [ImportingConstructor]
        public ShellViewModel( BuildService buildService, ISourceControl sourceControl )
        {
            myBuildService = buildService;

            GoCommand = new DelegateCommand( OnGo, CanGo );

            var args = Environment.GetCommandLineArgs();
            if( args.Length > 1 )
            {
                myBuildService.InitializeBuildDefinition( args[ 1 ] );
            }
            else
            {
                myBuildService.InitializeBuildDefinition( null );
            }
        }

        [Import]
        public CheckInViewModel CheckInViewModel { get; private set; }

        [Import]
        public BuildDefinitionViewModel BuildDefinitionViewModel { get; private set; }

        [Import]
        public BuildLogViewModel BuildLogViewModel { get; private set; }

        public void OnImportsSatisfied()
        {
            SelectedTab = 0;
        }

        public int SelectedTab
        {
            get { return mySelectedTab; }
            set
            {
                if( SetProperty( ref mySelectedTab, value ) )
                {
                    // force sync into view model before switching tab where view might get destroyed
                    TextBoxBinding.ForceSourceUpdate();
                }
            }
        }

        public DelegateCommand GoCommand { get; private set; }

        private bool CanGo() { return !myIsBusy; }

        private void OnGo()
        {
            BuildLogViewModel.Clear();
            myIsBusy = true;
            BuildLogViewModel.Succeeded = null;
            GoCommand.RaiseCanExecuteChanged();

            SelectedTab = 2;

            var progress = new Progress<string>( p => BuildLogViewModel.Append( p ) );

            var request = new BuildRequest
            {
                CheckInComment = CheckInViewModel.CheckInComment,
                Files = CheckInViewModel.Files
                    .Where( e => e.IsChecked )
                    .Select( e => e.File )
                    .ToList(),
            };

            myBuildService.ExecuteAsync( request, progress )
                .RethrowExceptionsInUIThread()
                .ContinueWith( t =>
                    {
                        BuildLogViewModel.Succeeded = t.Result;

                        myIsBusy = false;
                        GoCommand.RaiseCanExecuteChanged();

                        CheckInViewModel.RefreshPendingChanges();
                    }, TaskScheduler.FromCurrentSynchronizationContext() );
        }
    }
}
