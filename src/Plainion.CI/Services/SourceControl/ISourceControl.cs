using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plainion.CI.Services.SourceControl
{
    interface ISourceControl
    {
        Task<IEnumerable<Change>> GetPendingChangesAsync( string workspaceRoot );
        
        void Revert( string workspaceRoot, string file );

        void DiffToPrevious( string workspaceRoot, string file, string diffTool );
    }
}
