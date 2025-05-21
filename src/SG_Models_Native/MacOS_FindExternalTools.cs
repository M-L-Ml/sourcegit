using System;
using System.IO;
using Sausa;
using SourceGit.Models;

namespace SourceGit.Native
{
    // Original file: src/SG_Models_Native/MacOS.cs
    internal partial class MacOS
    {
        // Original file: src/SG_Models_Native/MacOS.cs MacOS.FindExternalTools
        public ExternalToolsFinder FindExternalTools()
        {
            var finder = new ExternalToolsFinder2();

            // Define standard editor tools with their custom LocationFinder delegates
            ExternalToolInfo2[] editorTools =
            [
                new ExternalToolInfo2(
                    Name: "Visual Studio Code",
                    LocationFinder: () => "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code"
                ),
                new ExternalToolInfo2(
                    Name: "Visual Studio Code - Insiders",
                    LocationFinder: () => "/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code"
                ),
                new ExternalToolInfo2(
                    Name: "VSCodium",
                    LocationFinder: () => "/Applications/VSCodium.app/Contents/Resources/app/bin/codium"
                ),
                new ExternalToolInfo2(
                    Name: "Fleet",
                    LocationFinder: () => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Applications/Fleet.app/Contents/MacOS/Fleet"
                ),
                new ExternalToolInfo2(
                    Name: "Sublime Text",
                    LocationFinder: () => "/Applications/Sublime Text.app/Contents/SharedSupport/bin/subl"
                ),
                new ExternalToolInfo2(
                    Name: "Zed",
                    LocationFinder: () => File.Exists("/usr/local/bin/zed") ? "/usr/local/bin/zed" : "/Applications/Zed.app/Contents/MacOS/cli"
                )
            ];

            // Add all editor tools in a loop
            foreach (var tool in editorTools)
                finder.AddEditorTool(tool);

            finder.FindJetBrainsFromToolbox(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/JetBrains/Toolbox");

            return finder;
        }
    }
}
