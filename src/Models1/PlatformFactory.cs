using System;

namespace Sausa
{
    /// <summary>
    /// Default implementation of IPlatformFactory that creates platform-specific implementations
    /// </summary>
    public class PlatformFactory : IPlatformFactory
    {
        /// <summary>
        /// Creates and returns the appropriate platform implementation for the current OS
        /// </summary>
        /// <returns>Platform implementation for the current OS</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when the current OS is not supported</exception>
        public IOSPlatform CreatePlatform()
        {
            if (IsWindows())
            {
                return new Sausa.Native.Windows();
            }
            else if (IsMacOS())
            {
                return new Sausa.Native.MacOS();
            }
            else if (IsLinux())
            {
                return new Sausa.Native.Linux();
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
