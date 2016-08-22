using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plainion.CI.Services.SourceControl
{
    interface ISourceControl
    {
        Task<IEnumerable<Change>> GetPendingChangesAsync( string workspaceRoot );

        void Commit( string workspaceRoot, IEnumerable<string> files, string comment, string name, string email );

        void Push( string workspaceRoot, string name, string password );
        
        void Revert( string workspaceRoot, string file );

        void DiffToPrevious( string workspaceRoot, string file, string diffTool );
    }
}
