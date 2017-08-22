using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Plainion.CI.Services;
using Plainion.Windows.Mvvm;

namespace Plainion.CI.ViewModels
{
    [Export]
    class BuildDefinitionViewModel : BindableBase
    {
        private BuildService myBuildService;

        [ImportingConstructor]
        public BuildDefinitionViewModel(BuildService buildService)
        {
            myBuildService = buildService;

            Configurations = new[] { "Debug", "Release" };
            Platforms = new[] { "Any CPU", "x86", "x64" };

            buildService.BuildDefinitionChanged += OnBuildDefinitionChanged;
            OnBuildDefinitionChanged();
        }

        private void OnBuildDefinitionChanged()
        {
            if (BuildDefinition != null)
            {
                BuildDefinition.PropertyChanged -= BuildDefinition_PropertyChanged;
            }

            BuildDefinition = myBuildService.BuildDefinition;
            OnPropertyChanged(nameof(BuildDefinition));

            if (BuildDefinition != null)
            {
                BuildDefinition.PropertyChanged += BuildDefinition_PropertyChanged;
                OnRepositoryRootChanged();
            }
        }

        private void BuildDefinition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BuildDefinition.RepositoryRoot))
            {
                OnRepositoryRootChanged();
            }
        }

        private void OnRepositoryRootChanged()
        {
            if (!string.IsNullOrEmpty(BuildDefinition.RepositoryRoot) && Directory.Exists(BuildDefinition.RepositoryRoot))
            {
                var solutionPath = Directory.GetFiles(BuildDefinition.RepositoryRoot, "*.sln", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
                if (solutionPath != null)
                {
                    BuildDefinition.Solution = Path.GetFileName(solutionPath);
                }
            }
        }

        public BuildDefinition BuildDefinition { get; private set; }

        public IEnumerable<string> Configurations { get; private set; }

        public IEnumerable<string> Platforms { get; private set; }
    }
}
