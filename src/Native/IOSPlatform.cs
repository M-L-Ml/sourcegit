namespace SourceGit.Native
{
    /// <summary>
    /// Abstracts OS-specific operations needed for tool discovery and launching.
    /// </summary>
    public interface IOSPlatform
    {
        string GetProgramFilesPath();
        string GetLocalAppDataPath();
        string GetEnvVariable(string name);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        // Add more as needed for tool discovery
    }
}
