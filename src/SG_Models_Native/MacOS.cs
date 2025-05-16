using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Sausa;
using SourceGit.Models;

namespace Sausa.Native
{
    // Original file: src/SG_Models_Native/MacOS.cs
    [SupportedOSPlatform("macOS")]
    internal class MacOS : IOSPlatform, IApplicationSetup, IFileSystem, IExternalTools, IProcessLauncher, OS.IBackend
    {
        public void SetupApp(object builder)
        {
            var appBuilder = PlatformAdapters.AsAppBuilder(builder);
            SetupApp(appBuilder);
        }
        
        // Original file: src/SG_Models_Native/MacOS.cs MacOS.SetupApp
        public void SetupApp(AppBuilder builder)
        {
            builder.With(new MacOSPlatformOptions()
            {
                DisableDefaultApplicationMenuItems = true,
            });

            // Fix `PATH` env on macOS.
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                path = "/opt/homebrew/bin:/opt/homebrew/sbin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";
            else if (!path.Contains("/opt/homebrew/", StringComparison.Ordinal))
                path = "/opt/homebrew/bin:/opt/homebrew/sbin:" + path;

            var customPathFile = Path.Combine(OS.DataDir, "PATH");
            if (File.Exists(customPathFile))
            {
                var env = File.ReadAllText(customPathFile).Trim();
                if (!string.IsNullOrEmpty(env))
                    path = env;
            }

            Environment.SetEnvironmentVariable("PATH", path);
        }

        public void SetupWindow(object window)
        {
            var avWindow = PlatformAdapters.AsWindow(window);
            SetupWindow(avWindow);
        }
        
        // Original file: src/SG_Models_Native/MacOS.cs MacOS.SetupWindow
        public void SetupWindow(Window window)
        {
            window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.SystemChrome;
            window.ExtendClientAreaToDecorationsHint = true;
        }

        public string FindGitExecutable()
        {
            var gitPathVariants = new List<string>() {
                 "/usr/bin/git", "/usr/local/bin/git", "/opt/homebrew/bin/git", "/opt/homebrew/opt/git/bin/git"
            };
            foreach (var path in gitPathVariants)
                if (File.Exists(path))
                    return path;
            return string.Empty;
        }

        public string FindTerminal(Models.ShellOrTerminal shell)
        {
            switch (shell.Type)
            {
                case "mac-terminal":
                    return "Terminal";
                case "iterm2":
                    return "iTerm";
                case "warp":
                    return "Warp";
                case "ghostty":
                    return "Ghostty";
                case "kitty":
                    return "kitty";
            }

            return string.Empty;
        }

        public Models.ExternalToolsFinder FindExternalTools()
        {
            var finder = new Models.ExternalToolsFinder();
            
            // Add standard editor tools using ExternalToolInfo2 objects
            finder.AddEditorTool(new Models.ExternalToolInfo2 
            { 
                Name = "Visual Studio Code", 
                LocationFinder = () => "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code" 
            });
            
            finder.AddEditorTool(new Models.ExternalToolInfo2 
            { 
                Name = "Visual Studio Code - Insiders", 
                LocationFinder = () => "/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code" 
            });
            
            finder.AddEditorTool(new Models.ExternalToolInfo2 
            { 
                Name = "VSCodium", 
                LocationFinder = () => "/Applications/VSCodium.app/Contents/Resources/app/bin/codium" 
            });
            
            finder.AddEditorTool(new Models.ExternalToolInfo2 
            { 
                Name = "Fleet", 
                LocationFinder = () => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Applications/Fleet.app/Contents/MacOS/Fleet" 
            });
            
            finder.AddEditorTool(new Models.ExternalToolInfo2 
            { 
                Name = "Sublime Text", 
                LocationFinder = () => "/Applications/Sublime Text.app/Contents/SharedSupport/bin/subl" 
            });
            
            finder.AddEditorTool(new Models.ExternalToolInfo2 
            { 
                Name = "Zed", 
                LocationFinder = () => File.Exists("/usr/local/bin/zed") ? "/usr/local/bin/zed" : "/Applications/Zed.app/Contents/MacOS/cli" 
            });
            
            finder.FindJetBrainsFromToolbox(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/JetBrains/Toolbox");
            
            return finder;
        }

        public void OpenBrowser(string url)
        {
            Process.Start("open", url);
        }

        public void OpenInFileManager(string path, bool select)
        {
            if (Directory.Exists(path))
                Process.Start("open", $"\"{path}\"");
            else if (File.Exists(path))
                Process.Start("open", $"\"{path}\" -R");
        }

        public void OpenTerminal(string workdir)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dir = string.IsNullOrEmpty(workdir) ? home : workdir;
            Process.Start("open", $"-a {OS.ShellOrTerminal} \"{dir}\"");
        }

        public void OpenWithDefaultEditor(string file)
        {
            Process.Start("open", $"\"{file}\"");
        }
    }
}
