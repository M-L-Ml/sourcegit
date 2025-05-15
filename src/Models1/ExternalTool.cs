using System;
using System.IO;

namespace Sausa
{
    // Original file: src/SG_Models_Native/ExternalTool.cs
    /// <summary>
    /// Represents an external tool that can be used by the application
    /// </summary>
    public class ExternalTool
    {
        /// <summary>
        /// Name of the external tool
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Path to the executable for the external tool
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Icon identifier for the external tool
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Type/category of the external tool
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether this external tool is valid
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Location) && File.Exists(Location);
    }
}
