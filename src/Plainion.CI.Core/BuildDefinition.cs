using System;
using System.IO;
using System.Runtime.Serialization;
using Plainion.Serialization;

namespace Plainion.CI
{
    [Serializable]
    [DataContract( Namespace = "http://github.com/ronin4net/plainion/GatedCheckIn", Name = "BuildDefinition" )]
    public class BuildDefinition : SerializableBindableBase
    {
        private string myRepositoryRoot;
        private string mySolution;
        private bool myRunTests;
        private bool myGenerateAPIDoc;
        private bool myCreatePackage;
        private bool myCheckIn;
        private bool myPush;
        private bool myDeploy;
        private bool myPublish;
        private string myConfiguration;
        private string myPlatform;
        private string myApiDocGenExecutable;
        private string myApiDocGenArguments;
        private string myTestRunnerExecutable;
        private string myTestAssemblyPattern;
        private string myPackagingScript;
        private string myCreatePackageArguments;
        private string myDeployPackageArguments;
        private string myPublishPackageArguments;
        private User myUser;
        private string myDiffTool;
        private string myNuGetExecutable;

        public BuildDefinition()
        {
            User = new User();
        }

        public string RepositoryRoot
        {
            get { return myRepositoryRoot; }
            set { SetProperty( ref myRepositoryRoot, value != null ? Path.GetFullPath( value ) : null ); }
        }

        [DataMember]
        public string Solution
        {
            get { return mySolution; }
            set { SetProperty( ref mySolution, value ); }
        }

        [DataMember]
        public bool RunTests
        {
            get { return myRunTests; }
            set { SetProperty( ref myRunTests, value ); }
        }

        [DataMember]
        public bool GenerateAPIDoc
        {
            get { return myGenerateAPIDoc; }
            set { SetProperty( ref myGenerateAPIDoc, value ); }
        }

        [DataMember]
        public bool CreatePackage
        {
            get { return myCreatePackage; }
            set { SetProperty( ref myCreatePackage, value ); }
        }

        [DataMember]
        public bool CheckIn
        {
            get { return myCheckIn; }
            set { SetProperty( ref myCheckIn, value ); }
        }

        [DataMember]
        public bool Push
        {
            get { return myPush; }
            set { SetProperty( ref myPush, value ); }
        }

        [DataMember]
        public bool DeployPackage
        {
            get { return myDeploy; }
            set { SetProperty( ref myDeploy, value ); }
        }

        [DataMember]
        public bool PublishPackage
        {
            get { return myPublish; }
            set { SetProperty( ref myPublish, value ); }
        }

        [DataMember]
        public string Configuration
        {
            get { return myConfiguration; }
            set { SetProperty( ref myConfiguration, value ); }
        }

        [DataMember]
        public string Platform
        {
            get { return myPlatform; }
            set { SetProperty( ref myPlatform, value ); }
        }

        [DataMember]
        public string ApiDocGenExecutable
        {
            get { return myApiDocGenExecutable; }
            set { SetProperty( ref myApiDocGenExecutable, value ); }
        }

        [DataMember]
        public string ApiDocGenArguments
        {
            get { return myApiDocGenArguments; }
            set { SetProperty( ref myApiDocGenArguments, value ); }
        }
        
        [DataMember]
        public string TestRunnerExecutable
        {
            get { return myTestRunnerExecutable; }
            set { SetProperty( ref myTestRunnerExecutable, value ); }
        }

        [DataMember]
        public string TestAssemblyPattern
        {
            get { return myTestAssemblyPattern; }
            set { SetProperty( ref myTestAssemblyPattern, value ); }
        }

        [DataMember(Name="CreatePackageScript")]
        public string PackagingScript
        {
            get { return myPackagingScript; }
            set { SetProperty( ref myPackagingScript, value ); }
        }

        [DataMember]
        public string CreatePackageArguments
        {
            get { return myCreatePackageArguments; }
            set { SetProperty( ref myCreatePackageArguments, value ); }
        }

        [DataMember]
        public string PublishPackageArguments
        {
            get { return myPublishPackageArguments; }
            set { SetProperty( ref myPublishPackageArguments, value ); }
        }

        [DataMember]
        public string DeployPackageArguments
        {
            get { return myDeployPackageArguments; }
            set { SetProperty( ref myDeployPackageArguments, value ); }
        }

        /// <summary/>
        /// <remarks>
        /// Saved in different file and therefore no DataMember.
        /// </remarks>
        public User User
        {
            get { return myUser; }
            set { SetProperty( ref myUser, value ); }
        }

        [DataMember]
        public string DiffTool
        {
            get { return myDiffTool; }
            set { SetProperty( ref myDiffTool, value ); }
        }

        [DataMember]
        public string NuGetExecutable
        {
            get { return myNuGetExecutable; }
            set { SetProperty( ref myNuGetExecutable, value ); }
        }
    
        public string GetOutputPath()
        {
            return Path.Combine( RepositoryRoot, "bin", "pCI" );
        }
    
        public string GetSolutionPath()
        {
            return Path.Combine( RepositoryRoot, Solution );
        }

        public string GetProjectName()
        {
            return Path.GetFileNameWithoutExtension( Solution );
        }
    }
}
