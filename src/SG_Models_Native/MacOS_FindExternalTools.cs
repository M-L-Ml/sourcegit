using System;
using System.IO;

namespace SourceGit.Native
{
    // Original file: src/SG_Models_Native/MacOS.cs
    internal partial class MacOS
    {
        // Original file: src/SG_Models_Native/MacOS.cs MacOS.FindExternalTools
        public Sausa.ExternalToolsFinder FindExternalTools()
        {
            var finder = new Sausa.ExternalToolsFinder();
            
            // Add standard editor tools using ExternalToolInfo2 objects
            finder.AddEditorTool(new Sausa.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Visual Studio Code", 
                LocationFinder = () => "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code" 
            });
            
            finder.AddEditorTool(new Sausa.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Visual Studio Code - Insiders", 
                LocationFinder = () => "/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code" 
            });
            
            finder.AddEditorTool(new Sausa.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "VSCodium", 
                LocationFinder = () => "/Applications/VSCodium.app/Contents/Resources/app/bin/codium" 
            });
            
            finder.AddEditorTool(new Sausa.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Fleet", 
                LocationFinder = () => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Applications/Fleet.app/Contents/MacOS/Fleet" 
            });
            
            finder.AddEditorTool(new Sausa.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Sublime Text", 
                LocationFinder = () => "/Applications/Sublime Text.app/Contents/SharedSupport/bin/subl" 
            });
            
            finder.AddEditorTool(new Sausa.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Zed", 
                LocationFinder = () => File.Exists("/usr/local/bin/zed") ? "/usr/local/bin/zed" : "/Applications/Zed.app/Contents/MacOS/cli" 
            });
            
            // Implementation for finding JetBrains tools
            // Note: The FindJetBrainsFromToolbox method is part of our implementation
            // but is handled by the adapter class for now
            
            return finder;
        }
    }
}
