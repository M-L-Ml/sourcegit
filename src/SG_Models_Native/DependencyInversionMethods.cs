using System;
using System.IO;

namespace Sausa.Native
{
    // This class contains helper methods needed for dependency inversion
    // in the Windows, Linux, and MacOS platform implementation classes
    internal static class DependencyInversionMethods
    {
        // Original file: src/SG_Models_Native/Windows.cs
        internal static string FindJetBrainsFleet()
        {
            var fleetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Fleet", "Fleet.exe");
            if (File.Exists(fleetPath))
                return fleetPath;
            return string.Empty;
        }
        
        // Original file: src/SG_Models_Native/Windows.cs
        internal static string FindZed()
        {
            var zedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Zed", "Zed.exe");
            if (File.Exists(zedPath))
                return zedPath;
            return string.Empty;
        }
    }
}
