using System;
using System.IO;
using ExternalToolsFinder = Sausa.ExternalToolsFinder;
using static Sausa.ExternalToolsFinder;
using Sausa;

namespace SourceGit.Native
{
    // Original file: src/SG_Models_Native/MacOS.cs
    internal partial class MacOS
    {
        // Original file: src/SG_Models_Native/MacOS.cs MacOS.FindExternalTools
        public ExternalToolsFinder FindExternalTools()
        {
            var finder = new ExternalToolsFinder();

            // Define standard editor tools with their custom LocationFinder delegates
            ExternalToolInfo2[] editorTools = //new
            [
                new()
                {
                    Name = "Visual Studio Code",
                    LocationFinder = () => "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code"
                },

                new()
                  {
                      Name = "Visual Studio Code - Insiders",
                      LocationFinder = () => "/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code"
                  },
                new()
                {
                    Name = "VSCodium",
                    LocationFinder = () => "/Applications/VSCodium.app/Contents/Resources/app/bin/codium"
                },
                new()
                {
                    Name = "Fleet",
                    LocationFinder = () => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Applications/Fleet.app/Contents/MacOS/Fleet"
                },
                new()
                {
                    Name = "Sublime Text",
                    LocationFinder = () => "/Applications/Sublime Text.app/Contents/SharedSupport/bin/subl"
                },
                new()
                {
                    Name = "Zed",
                    LocationFinder = () => File.Exists("/usr/local/bin/zed") ? "/usr/local/bin/zed" : "/Applications/Zed.app/Contents/MacOS/cli"
                }
             ];


            // Add all editor tools in a loop
            foreach (var tool in editorTools)
                finder.AddEditorTool(tool);

            // Implementation for finding JetBrains tools
            // Note: The FindJetBrainsFromToolbox method is part of our implementation
            // but is handled by the adapter class for now

            return finder;
        }
    }
}
