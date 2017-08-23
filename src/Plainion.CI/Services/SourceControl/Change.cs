
using System;

namespace Plainion.CI.Services.SourceControl
{
    class Change : IEquatable<Change>
    {
        public Change(string path, ChangeType type)
        {
            Contract.RequiresNotNullNotEmpty(path, "path");

            Path = path;
            ChangeType = type;
        }

        public string Path { get; private set; }

        public ChangeType ChangeType { get; private set; }

        public bool Equals(Change other)
        {
            return ChangeType == other.ChangeType && Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Change;
            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }

        public override int GetHashCode()
        {
            return (Path.GetHashCode() * 251) + ChangeType.GetHashCode();
        }
    }
}
