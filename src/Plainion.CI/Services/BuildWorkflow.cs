﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plainion.CI;
using Plainion.CI.Services.SourceControl;

namespace Plainion.CI.Services
{
    internal class BuildWorkflow
    {
        private ISourceControl mySourceControl;
        private BuildDefinition myDefinition;
        private BuildRequest myRequest;

        public BuildWorkflow( ISourceControl sourceControl, BuildDefinition definition, BuildRequest request )
        {
            mySourceControl = sourceControl;
            myDefinition = definition;
            myRequest = request;
        }

        internal Task<bool> ExecuteAsync( IProgress<string> progress )
        {
            // clone thread save copy of the relevant paramters;
            myDefinition = Objects.Clone( myDefinition );
            myRequest = Objects.Clone( myRequest );

            var toolsHome = Path.GetDirectoryName( GetType().Assembly.Location );
            var workflowFsx = Path.Combine( toolsHome, "bits", "Workflow.fsx" );

            return Task<bool>.Run( () =>
                Try( "Workflow", Run( workflowFsx, "default" ), progress )
                && ( !myDefinition.CreatePackage || Try( "create pacakge", Run( myDefinition.CreatePackageScript, myDefinition.CreatePackageArguments ), progress ) )
                && ( !myDefinition.DeployPackage || Try( "deploy pacakge", Run( myDefinition.DeployPackageScript, myDefinition.DeployPackageArguments ), progress ) )
            );
        }

        private bool Try( string activity, Func<IProgress<string>, bool> action, IProgress<string> progress )
        {
            try
            {
                var success = action( progress );

                if( success )
                {
                    progress.Report( "--- " + activity.ToUpper() + " SUCCEEDED ---" );
                }
                else
                {
                    progress.Report( "--- " + activity.ToUpper() + " FAILED ---" );
                }

                return success;
            }
            catch( Exception ex )
            {
                progress.Report( "ERROR: " + ex.Message );
                progress.Report( "--- " + activity.ToUpper() + " FAILED ---" );
                return false;
            }
        }

        private Func<IProgress<string>, bool> Run( string script, string args )
        {
            return Run( script, null, args.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ) );
        }

        private Func<IProgress<string>, bool> Run( string script, params string[] args )
        {
            return Run( script, null, args );
        }

        private Func<IProgress<string>, bool> Run( string script, string target, params string[] args )
        {
            return p =>
            {
                var executor = GetExecutor( script, p );
                return executor.Execute( script, target, args );
            };
        }

        private AbstractScriptExecutor GetExecutor( string script, IProgress<string> progress )
        {
            var fakeScriptExecutor = new FakeScriptExecutor( myDefinition, progress );

            return fakeScriptExecutor.CanExecute( script )
                ? ( AbstractScriptExecutor )fakeScriptExecutor
                : ( AbstractScriptExecutor )new MsBuildScriptExecutor( myDefinition, progress );
        }
    }
}
