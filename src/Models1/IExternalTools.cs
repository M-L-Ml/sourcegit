namespace Sausa
{
    /// <summary>
    /// Interface for discovering and managing external tools
    /// </summary>
    public interface IExternalTools
    {
        /// <summary>
        /// Locates the Git executable on the system
        /// </summary>
        /// <returns>Path to the Git executable, or empty string if not found</returns>
        string FindGitExecutable();
        
        /// <summary>
        /// Finds the specified shell or terminal executable
        /// </summary>
        /// <param name="shell">Shell or terminal to find</param>
        /// <returns>Path to the terminal executable, or empty string if not found</returns>
        string FindTerminal(ShellOrTerminal shell);
        
        /// <summary>
        /// Discovers all external tools available on the system
        /// </summary>
        /// <returns>Collection of found external tools</returns>
        ExternalToolsFinder FindExternalTools();
    }
}
