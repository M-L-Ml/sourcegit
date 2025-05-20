using System;
using Sausa;

namespace Models1.tmp
{
    /// <summary>
    /// Default implementation of IPlatformFactory that creates platform-specific implementations
    /// </summary>
    [Obsolete("Not used. TODO: make usable or delete")]
    public class PlatformFactory : IPlatformFactory
    {
        /// <summary>
        /// Creates and returns the appropriate platform implementation for the current OS
        /// </summary>
        /// <returns>Platform implementation for the current OS</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when the current OS is not supported</exception>
        public IOSPlatform CreatePlatform()
        {
            // Platform implementations are in the SG_Models_Native assembly
            // and should be dynamically loaded/instantiated at runtime
            // This factory uses reflection to avoid direct dependencies
            // which would create circular references
            Type platformType = null;
            
            if (IsWindows())
            {
                platformType = Type.GetType("Sausa.Native.Windows, SG_Models_Native");
            }
            else if (IsMacOS())
            {
                platformType = Type.GetType("Sausa.Native.MacOS, SG_Models_Native");
            }
            else if (IsLinux())
            {
                platformType = Type.GetType("Sausa.Native.Linux, SG_Models_Native");
            }
            
            if (platformType != null)
            {
                return (IOSPlatform)Activator.CreateInstance(platformType);
            }
            
            throw new PlatformNotSupportedException("Current platform is not supported by Sausa");
        }

        /// <summary>
        /// Determines if the current operating system is Windows
        /// </summary>
        public bool IsWindows() => OperatingSystem.IsWindows();

        /// <summary>
        /// Determines if the current operating system is macOS
        /// </summary>
        public bool IsMacOS() => OperatingSystem.IsMacOS();

        /// <summary>
        /// Determines if the current operating system is Linux
        /// </summary>
        public bool IsLinux() => OperatingSystem.IsLinux();
    }
}
