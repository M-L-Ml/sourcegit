using System;
using System.Collections.Generic;
using System.IO;

namespace Sausa
{
    // Original file: src/SG_Models_Native/ExternalTool.cs (partial)
    /// <summary>
    /// Utility for finding external tools on the system
    /// </summary>
    public abstract class ExternalToolsFinder
    {
        protected List<ExternalTool> _tools = new List<ExternalTool>();


        /// <summary>
        /// Adds an editor tool to the finder
        /// </summary>
        /// <param name="info">Information about the editor tool</param>
        protected void AddTool(ExternalToolInfo2 info, string? type)
        {
            var location = info.LocationFinder();
            if (!TryAdd(info, location, type))
            {
                throw new InvalidProgramException("External tool location not found.");
            }
        }
        protected void Founded_Add(ExternalTool externalTool)
        {
            _tools.Add(externalTool);
        }
        protected bool TryAdd(ExternalToolInfo toolInfo, string location, string? type = "external tool")
        {
            if (toolInfo == null)
                throw new ArgumentNullException(nameof(toolInfo));

            if (string.IsNullOrEmpty(location) || !File.Exists(location))
                return false;

            _tools.Add(new ExternalTool
            (
                info: toolInfo,

                location: location,
                type: type
            ));

            return true;
        }

        /// <summary>
        /// Finds JetBrains tools from the toolbox
        /// </summary>
        /// <param name="toolboxPathProvider">Function that returns the toolbox path</param>
        public abstract void FindJetBrainsFromToolbox(Func<string> toolboxPathProvider);
        //{
        //    var toolboxPath = toolboxPathProvider();
        //    if (string.IsNullOrEmpty(toolboxPath) || !Directory.Exists(toolboxPath))
        //        return;

        // }

        /// <summary>
        /// Converts the finder results to a list of external tools
        /// </summary>
        /// <returns>List of found external tools</returns>
        public IReadOnlyList<ExternalTool> ToList()
        {
            return _tools;
        }
    }
}
