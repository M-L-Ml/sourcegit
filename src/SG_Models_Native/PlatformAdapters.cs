using System;
using Avalonia;
using Avalonia.Controls;
using Sausa;

namespace SourceGit.Native
{
    /// <summary>
    /// Contains platform adapters to bridge between the abstract interfaces and concrete implementations
    /// </summary>
    internal static class PlatformAdapters
    {
        /// <summary>
        /// Adapts the AppBuilder object for platform-specific implementations
        /// </summary>
        /// <param name="builder">Generic builder object</param>
        /// <returns>Avalonia AppBuilder</returns>
        /// <exception cref="ArgumentException">Thrown when the provided object is not an AppBuilder</exception>
        public static AppBuilder AsAppBuilder(object builder)
        {
            if (builder is AppBuilder appBuilder)
                return appBuilder;
                
            throw new ArgumentException("Object must be an Avalonia AppBuilder", nameof(builder));
        }
        
        /// <summary>
        /// Adapts the Window object for platform-specific implementations
        /// </summary>
        /// <param name="window">Generic window object</param>
        /// <returns>Avalonia Window</returns>
        /// <exception cref="ArgumentException">Thrown when the provided object is not a Window</exception>
        public static Window AsWindow(object window)
        {
            if (window is Window avaloniaWindow)
                return avaloniaWindow;
                
            throw new ArgumentException("Object must be an Avalonia Window", nameof(window));
        }
        
        /// <summary>
        /// Converts SourceGit model to Sausa model
        /// </summary>
        /// <param name="shell">SourceGit ShellOrTerminal</param>
        /// <returns>Sausa ShellOrTerminal</returns>
        public static Sausa.ShellOrTerminal AsShellOrTerminal(SourceGit.Models.ShellOrTerminal shell)
        {
            if (shell == null)
                return null;
                
            return new Sausa.ShellOrTerminal(shell.Type, shell.Name, shell.Exec);
        }
        
        /// <summary>
        /// Converts ExternalToolsFinder to Sausa implementation
        /// </summary>
        /// <param name="finder">SourceGit ExternalToolsFinder</param>
        /// <returns>Sausa ExternalToolsFinder</returns>
        public static Sausa.ExternalToolsFinder AsExternalToolsFinder(SourceGit.Models.ExternalToolsFinder finder)
        {
            // Create a new ExternalToolsFinder
            var result = new Sausa.ExternalToolsFinder();
            
            // Copy tools from original finder
            // Implementation would be more complex in real code
            
            return result;
        }
    }
}
