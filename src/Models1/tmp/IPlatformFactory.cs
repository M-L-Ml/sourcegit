using System;
using Sausa;

namespace Models1.tmp
{
    /// <summary>
    /// Interface for platform factory that creates appropriate platform implementation
    /// based on the current operating system
    /// </summary>
    [Obsolete("Not used. TODO: make usable or delete")]
    public interface IPlatformFactory
    {
        /// <summary>
        /// Creates and returns the appropriate platform implementation for the current OS
        /// </summary>
        /// <returns>Platform implementation</returns>
        IOSPlatform CreatePlatform();
        
        /// <summary>
        /// Determines if the current operating system is Windows
        /// </summary>
        bool IsWindows();
        
        /// <summary>
        /// Determines if the current operating system is macOS
        /// </summary>
        bool IsMacOS();
        
        /// <summary>
        /// Determines if the current operating system is Linux
        /// </summary>
        bool IsLinux();
    }
}
