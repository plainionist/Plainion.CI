using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Plainion.CI.Services.SourceControl
{
    /// <summary>
    /// Thread-safe and re-entrent
    /// </summary>
    [Export(typeof(ISourceControl))]
    class GitService : ISourceControl
    {
        public Task<IReadOnlyCollection<Change>> GetPendingChangesAsync(string workspaceRoot)
        {
            return Task<IReadOnlyCollection<Change>>.Run(() =>
            {
                using(var repo = new Repository(workspaceRoot))
                {
                    return (IReadOnlyCollection<Change>)repo.RetrieveStatus()
                        .Where(e => (e.State & FileStatus.Ignored) == 0)
                        .Select(e => new Change(e.FilePath, GetChangeType(e.State)))
                        .ToList();
                }
            });
        }

        private ChangeType GetChangeType(FileStatus fileStatus)
        {
            if(fileStatus == FileStatus.NewInWorkdir) return ChangeType.Untracked;
            if(fileStatus == FileStatus.DeletedFromWorkdir) return ChangeType.Missing;
            return ChangeType.Modified;
        }

        public void DiffToPrevious(string workspaceRoot, string file, string diffTool)
        {
            var headFile = GetHeadOf(workspaceRoot, file);

            var parts = Regex.Matches(diffTool, @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToList();

            var executable = parts.First().Trim('"');
            var args = string.Join(" ", parts.Skip(1))
                .Replace("%base", headFile)
                .Replace("%mine", Path.Combine(workspaceRoot, file));

            Process.Start(executable, args);
        }

        /// <summary>
        /// Returns path to a temp file which contains the HEAD version of the given file.
        /// The caller has to take care to delete the file.
        /// </summary>
        private string GetHeadOf(string workspaceRoot, string relativePath)
        {
            using(var repo = new Repository(workspaceRoot))
            {
                var log = repo.Commits.QueryBy(relativePath);
                if(log == null || !log.Any())
                {
                    // file not yet tracked -> ignore
                    return null;
                }

                var head = log.First();
                var treeEntry = head.Commit.Tree[relativePath];
                var blob = (Blob)treeEntry.Target;

                var file = Path.Combine(Path.GetTempPath(), Path.GetFileName(relativePath) + ".head");

                using(var reader = new StreamReader(blob.GetContentStream()))
                {
                    using(var writer = new StreamWriter(file))
                    {
                        while(!reader.EndOfStream)
                        {
                            writer.WriteLine(reader.ReadLine());
                        }
                    }
                }

                return file;
            }
        }

        public void Revert(string workspaceRoot, string file)
        {
            using(var repo = new Repository(workspaceRoot))
            {
                var options = new CheckoutOptions
                {
                    CheckoutModifiers = CheckoutModifiers.Force
                };
                repo.CheckoutPaths("HEAD", new[] { file }, options);
            }
        }

        public void Ignore(string workspaceRoot, string pattern)
        {
            File.AppendAllLines(Path.Combine(workspaceRoot, ".gitignore"), new[] { pattern.Replace('\\', '/') });
        }
    }
}
