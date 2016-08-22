
namespace Plainion.CI.Services.SourceControl
{
    class Change
    {
        public Change( string path, ChangeType type )
        {
            Path = path;
            ChangeType = type;
        }

        public string Path { get; private set; }

        public ChangeType ChangeType { get; private set; }
    }
}
