namespace Sausa
{
    /// <summary>
    /// Interface for file system operations that require platform-specific implementations
    /// </summary>
    public interface IFileSystem
    {

        /// <summary>
        /// Opens the specified path in the system's file manager
        /// </summary>
        /// <param name="path">Path to open</param>
        /// <param name="select">Whether to select the item in the file manager</param>
        void OpenInFileManager(string path, bool select);

        /// <summary>
        /// Opens the specified file with the system's default editor
        /// </summary>
        /// <param name="file">File to open</param>
        void OpenWithDefaultEditor(string file);

        /// <summary>
        /// Locates the Git executable on the system
        /// </summary>
        /// <returns>Path to the Git executable, or empty string if not found</returns>
        //   string FindGitExecutable();

    }
}
