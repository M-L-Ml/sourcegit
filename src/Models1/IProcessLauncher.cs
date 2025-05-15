namespace Sausa
{
    /// <summary>
    /// Interface for launching external processes and applications
    /// </summary>
    public interface IProcessLauncher
    {
        /// <summary>
        /// Opens the system default web browser with the specified URL
        /// </summary>
        /// <param name="url">URL to open</param>
        void OpenBrowser(string url);
        
        /// <summary>
        /// Opens a terminal in the specified working directory
        /// </summary>
        /// <param name="workdir">Working directory to open the terminal in</param>
        void OpenTerminal(string workdir);
    }
}
