using System;
using System.Collections.Generic;
using System.IO;

namespace Sausa
{
    // Original file: src/SG_Models_Native/ExternalTool.cs (partial)
    /// <summary>
    /// Utility for finding external tools on the system
    /// </summary>
    public class ExternalToolsFinder
    {
        private List<ExternalTool> _tools = new List<ExternalTool>();

        /// <summary>
        /// Helper class for external tool information
        /// </summary>
        public class ExternalToolInfo2
        {
            /// <summary>
            /// Name of the tool
            /// </summary>
            public string Name { get; set; } = string.Empty;
            
            /// <summary>
            /// Function that returns the location of the tool
            /// </summary>
            public Func<string> LocationFinder { get; set; } = () => string.Empty;
        }

        /// <summary>
        /// Adds an editor tool to the finder
        /// </summary>
        /// <param name="info">Information about the editor tool</param>
        public void AddEditorTool(ExternalToolInfo2 info)
        {
            if (info == null) return;

            var location = info.LocationFinder();
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                _tools.Add(new ExternalTool
                {
                    Name = info.Name,
                    Location = location,
                    Type = "editor"
                });
            }
        }

        /// <summary>
        /// Finds JetBrains tools from the toolbox
        /// </summary>
        /// <param name="toolboxPathProvider">Function that returns the toolbox path</param>
        public void FindJetBrainsFromToolbox(Func<string> toolboxPathProvider)
        {
            var toolboxPath = toolboxPathProvider();
            if (string.IsNullOrEmpty(toolboxPath) || !Directory.Exists(toolboxPath))
                return;
            
            // This is a simplified implementation for now
            // In a real implementation, this would search for JetBrains tools in the toolbox directory
        }

        /// <summary>
        /// Converts the finder results to a list of external tools
        /// </summary>
        /// <returns>List of found external tools</returns>
        public List<ExternalTool> ToList()
        {
            return new List<ExternalTool>(_tools);
        }
    }
}
